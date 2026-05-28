using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.Services;

public sealed partial class AladhanPrayerTimesService
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://api.aladhan.com/v1/")
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _cacheDirectory;

    public AladhanPrayerTimesService(string appDirectory)
    {
        _cacheDirectory = Path.Combine(appDirectory, "cache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<PrayerDay> GetPrayerDayAsync(
        DateOnly date,
        AppSettings settings,
        PrayerLocation location,
        CancellationToken cancellationToken)
    {
        settings.EnsureDefaults();
        string cacheKey = BuildCacheKey(date, settings, location);
        string cachePath = Path.Combine(_cacheDirectory, cacheKey + ".json");

        if (TryLoadFreshCache(cachePath, out PrayerDay? cached))
        {
            return cached;
        }

        try
        {
            PrayerDay fetched = await FetchPrayerDayAsync(date, settings, location, cancellationToken).ConfigureAwait(false);
            SaveCache(cachePath, fetched);
            return fetched;
        }
        catch
        {
            if (TryLoadAnyCache(cachePath, out PrayerDay? stale))
            {
                return stale;
            }

            throw;
        }
    }

    private static async Task<PrayerDay> FetchPrayerDayAsync(
        DateOnly date,
        AppSettings settings,
        PrayerLocation location,
        CancellationToken cancellationToken)
    {
        string apiDate = date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        string path = BuildTimingsPath(apiDate, settings, location);
        using HttpResponseMessage response = await HttpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = JsonSerializer.Deserialize<AladhanResponse>(body, JsonOptions);
        if (payload?.Data?.Timings is null)
        {
            throw new InvalidOperationException("Prayer time response was missing timings.");
        }

        if (payload.Code is not null && payload.Code != 200)
        {
            throw new InvalidOperationException(payload.Status ?? "Prayer time API returned an error.");
        }

        DateOnly responseDate = ParseApiDate(payload.Data.Date?.Gregorian?.Date) ?? date;
        var times = new List<PrayerTime>();

        foreach (string prayerName in PrayerNames.Dashboard)
        {
            string? raw = payload.Data.Timings.GetValueOrDefault(prayerName);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (!TryParsePrayerTime(responseDate, raw, out DateTime time))
            {
                continue;
            }

            if (Array.Exists(PrayerNames.Alerted, name => name.Equals(prayerName, StringComparison.OrdinalIgnoreCase)))
            {
                time = time.AddMinutes(settings.GetPrayerAdjustment(prayerName));
            }

            bool isAlertedPrayer = Array.Exists(
                PrayerNames.Alerted,
                name => name.Equals(prayerName, StringComparison.OrdinalIgnoreCase));
            times.Add(new PrayerTime(prayerName, time, isAlertedPrayer));
        }

        return new PrayerDay
        {
            Date = responseDate,
            Location = location.Label,
            TimeZone = payload.Data.Meta?.Timezone ?? "",
            MethodName = payload.Data.Meta?.Method?.Name ?? "",
            FetchedAt = DateTime.Now,
            Times = times.OrderBy(time => time.Time).ToList()
        };
    }

    public static PrayerSchedule BuildSchedule(PrayerDay today, PrayerDay tomorrow, AppSettings settings)
    {
        var occurrences = new List<PrayerOccurrence>();

        foreach (PrayerDay day in new[] { today, tomorrow })
        {
            foreach (PrayerTime prayerTime in day.Times.Where(time => time.IsAlertedPrayer))
            {
                int iqamahOffset = settings.GetIqamahOffset(prayerTime.Name);
                occurrences.Add(new PrayerOccurrence(prayerTime.Name, "Adhan", prayerTime.Time, iqamahOffset));
                occurrences.Add(new PrayerOccurrence(prayerTime.Name, "Iqamah", prayerTime.Time.AddMinutes(iqamahOffset), iqamahOffset));
            }
        }

        return new PrayerSchedule
        {
            Today = today,
            Tomorrow = tomorrow,
            Occurrences = occurrences.OrderBy(occurrence => occurrence.Time).ToList()
        };
    }

    private static string BuildQuery(Dictionary<string, string?> values)
    {
        return string.Join("&", values
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => WebUtility.UrlEncode(pair.Key) + "=" + WebUtility.UrlEncode(pair.Value)));
    }

    private static string BuildTimingsPath(string apiDate, AppSettings settings, PrayerLocation location)
    {
        var query = new Dictionary<string, string?>
        {
            ["school"] = settings.School.ToString(CultureInfo.InvariantCulture)
        };

        if (!settings.UseAutomaticCalculationMethod && settings.CalculationMethod >= 0)
        {
            query["method"] = settings.CalculationMethod.ToString(CultureInfo.InvariantCulture);
        }

        if (location.HasCoordinates)
        {
            query["latitude"] = location.Latitude!.Value.ToString("0.######", CultureInfo.InvariantCulture);
            query["longitude"] = location.Longitude!.Value.ToString("0.######", CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(location.TimeZone))
            {
                query["timezonestring"] = location.TimeZone;
            }

            return "timings/" + WebUtility.UrlEncode(apiDate) + "?" + BuildQuery(query);
        }

        query["city"] = location.City ?? settings.City;
        query["country"] = location.Country ?? settings.Country;
        if (!string.IsNullOrWhiteSpace(location.State ?? settings.State))
        {
            query["state"] = location.State ?? settings.State;
        }

        return "timingsByCity/" + WebUtility.UrlEncode(apiDate) + "?" + BuildQuery(query);
    }

    private static string BuildCacheKey(DateOnly date, AppSettings settings, PrayerLocation location)
    {
        string raw = string.Join("|",
            date.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            location.Mode,
            location.Label,
            location.Latitude?.ToString("0.######", CultureInfo.InvariantCulture) ?? "",
            location.Longitude?.ToString("0.######", CultureInfo.InvariantCulture) ?? "",
            location.City ?? "",
            location.Country ?? "",
            location.State ?? "",
            settings.UseAutomaticCalculationMethod ? "auto-method" : settings.CalculationMethod,
            settings.School,
            string.Join(",", settings.PrayerAdjustmentsMinutes.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}")));

        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            raw = raw.Replace(invalid, '_');
        }

        return WebUtility.UrlEncode(raw).Replace("%", "", StringComparison.Ordinal);
    }

    private static bool TryLoadFreshCache(string cachePath, [NotNullWhen(true)] out PrayerDay? prayerDay)
    {
        prayerDay = null;
        if (!TryLoadAnyCache(cachePath, out PrayerDay? cached))
        {
            return false;
        }

        if ((DateTime.Now - cached.FetchedAt) > TimeSpan.FromHours(18))
        {
            return false;
        }

        prayerDay = cached;
        return true;
    }

    private static bool TryLoadAnyCache(string cachePath, [NotNullWhen(true)] out PrayerDay? prayerDay)
    {
        prayerDay = null;

        if (!File.Exists(cachePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(cachePath);
            prayerDay = JsonSerializer.Deserialize<PrayerDay>(json, JsonOptions);
            return prayerDay is not null;
        }
        catch
        {
            return false;
        }
    }

    private static void SaveCache(string cachePath, PrayerDay prayerDay)
    {
        string json = JsonSerializer.Serialize(prayerDay, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(cachePath, json);
    }

    private static DateOnly? ParseApiDate(string? value)
    {
        if (DateOnly.TryParseExact(value, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            return date;
        }

        return null;
    }

    private static bool TryParsePrayerTime(DateOnly date, string raw, out DateTime time)
    {
        time = default;
        Match match = TimeOnlyPattern().Match(raw);
        if (!match.Success)
        {
            return false;
        }

        if (!int.TryParse(match.Groups["hour"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hour) ||
            !int.TryParse(match.Groups["minute"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minute))
        {
            return false;
        }

        time = date.ToDateTime(new TimeOnly(hour, minute));
        return true;
    }

    [GeneratedRegex(@"(?<hour>\d{1,2}):(?<minute>\d{2})")]
    private static partial Regex TimeOnlyPattern();

    private sealed class AladhanResponse
    {
        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("data")]
        public AladhanData? Data { get; set; }
    }

    private sealed class AladhanData
    {
        [JsonPropertyName("timings")]
        public Dictionary<string, string> Timings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("date")]
        public AladhanDate? Date { get; set; }

        [JsonPropertyName("meta")]
        public AladhanMeta? Meta { get; set; }
    }

    private sealed class AladhanDate
    {
        [JsonPropertyName("gregorian")]
        public AladhanGregorianDate? Gregorian { get; set; }
    }

    private sealed class AladhanGregorianDate
    {
        [JsonPropertyName("date")]
        public string? Date { get; set; }
    }

    private sealed class AladhanMeta
    {
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("method")]
        public AladhanMethod? Method { get; set; }
    }

    private sealed class AladhanMethod
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
