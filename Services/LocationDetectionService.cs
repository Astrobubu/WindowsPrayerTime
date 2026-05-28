using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.Services;

public sealed class LocationDetectionService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<PrayerLocation> ResolveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        settings.EnsureDefaults();

        if (string.Equals(settings.LocationMode, LocationModeOptions.Coordinates, StringComparison.OrdinalIgnoreCase))
        {
            return FromCoordinates(settings, "Manual coordinates", settings.DetectedTimeZone);
        }

        if (string.Equals(settings.LocationMode, LocationModeOptions.Auto, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                PrayerLocation detected = await DetectFromNetworkAsync(settings, cancellationToken).ConfigureAwait(false);
                return detected;
            }
            catch
            {
                return FromCity(settings, "Fallback: " + settings.LocationLabel);
            }
        }

        return FromCity(settings, settings.LocationLabel);
    }

    private static async Task<PrayerLocation> DetectFromNetworkAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://ipapi.co/json/");
        request.Headers.UserAgent.ParseAdd("WindowsPrayerTime/1.0");

        using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = JsonSerializer.Deserialize<IpApiResponse>(body, JsonOptions);
        if (payload?.Latitude is null || payload.Longitude is null)
        {
            throw new InvalidOperationException("Network location response did not include coordinates.");
        }

        string label = BuildLabel(payload.City, payload.Region, payload.CountryName);
        settings.Latitude = payload.Latitude.Value;
        settings.Longitude = payload.Longitude.Value;
        settings.DetectedCity = payload.City ?? "";
        settings.DetectedRegion = payload.Region ?? "";
        settings.DetectedCountry = payload.CountryName ?? "";
        settings.DetectedTimeZone = payload.TimeZone ?? "";
        settings.LocationDetectedAt = DateTime.Now;

        return new PrayerLocation(
            LocationModeOptions.Auto,
            string.IsNullOrWhiteSpace(label) ? FormatCoordinates(payload.Latitude.Value, payload.Longitude.Value) : label,
            payload.City,
            payload.CountryName,
            payload.Region,
            payload.Latitude,
            payload.Longitude,
            payload.TimeZone);
    }

    private static PrayerLocation FromCoordinates(AppSettings settings, string labelPrefix, string? timeZone)
    {
        return new PrayerLocation(
            LocationModeOptions.Coordinates,
            $"{labelPrefix}: {FormatCoordinates(settings.Latitude, settings.Longitude)}",
            null,
            null,
            null,
            settings.Latitude,
            settings.Longitude,
            string.IsNullOrWhiteSpace(timeZone) ? null : timeZone);
    }

    private static PrayerLocation FromCity(AppSettings settings, string label)
    {
        return new PrayerLocation(
            LocationModeOptions.City,
            label,
            settings.City,
            settings.Country,
            settings.State,
            null,
            null,
            null);
    }

    private static string BuildLabel(params string?[] parts)
    {
        return string.Join(", ", parts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string FormatCoordinates(double latitude, double longitude)
    {
        return latitude.ToString("0.####", CultureInfo.InvariantCulture) + ", " +
            longitude.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private sealed class IpApiResponse
    {
        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("country_name")]
        public string? CountryName { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("timezone")]
        public string? TimeZone { get; set; }
    }
}
