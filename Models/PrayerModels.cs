namespace WindowsPrayerTime.Models;

public static class PrayerNames
{
    public static readonly string[] Alerted =
    [
        "Fajr",
        "Dhuhr",
        "Asr",
        "Maghrib",
        "Isha"
    ];

    public static readonly string[] Dashboard =
    [
        "Fajr",
        "Sunrise",
        "Dhuhr",
        "Asr",
        "Maghrib",
        "Isha"
    ];

    public static string ShortName(string prayerName) => prayerName.ToLowerInvariant() switch
    {
        "fajr" => "FJR",
        "dhuhr" => "DHR",
        "asr" => "ASR",
        "maghrib" => "MAG",
        "isha" => "ISH",
        "sunrise" => "SUN",
        _ => prayerName.Length <= 3 ? prayerName.ToUpperInvariant() : prayerName[..3].ToUpperInvariant()
    };
}

public sealed record PrayerTime(string Name, DateTime Time, bool IsAlertedPrayer);

public sealed record PrayerLocation(
    string Mode,
    string Label,
    string? City,
    string? Country,
    string? State,
    double? Latitude,
    double? Longitude,
    string? TimeZone)
{
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
}

public sealed class PrayerDay
{
    public DateOnly Date { get; init; }
    public string Location { get; init; } = "";
    public string TimeZone { get; init; } = "";
    public string MethodName { get; init; } = "";
    public DateTime FetchedAt { get; init; } = DateTime.Now;
    public List<PrayerTime> Times { get; init; } = [];

    public PrayerTime? Find(string name) =>
        Times.FirstOrDefault(time => string.Equals(time.Name, name, StringComparison.OrdinalIgnoreCase));
}

public sealed record PrayerOccurrence(
    string PrayerName,
    string Kind,
    DateTime Time,
    int IqamahOffsetMinutes)
{
    public string Id => $"{Time:yyyyMMddHHmm}-{PrayerName}-{Kind}";
    public bool IsIqamah => string.Equals(Kind, "Iqamah", StringComparison.OrdinalIgnoreCase);
    public bool IsAdhan => string.Equals(Kind, "Adhan", StringComparison.OrdinalIgnoreCase);
}

public sealed class PrayerSchedule
{
    public PrayerDay? Today { get; init; }
    public PrayerDay? Tomorrow { get; init; }
    public List<PrayerOccurrence> Occurrences { get; init; } = [];
    public DateTime BuiltAt { get; init; } = DateTime.Now;

    public PrayerOccurrence? NextAlert(DateTime now) =>
        Occurrences
            .Where(occurrence => occurrence.Time > now)
            .OrderBy(occurrence => occurrence.Time)
            .FirstOrDefault();

    public PrayerOccurrence? NextAdhan(DateTime now) =>
        Occurrences
            .Where(occurrence => occurrence.IsAdhan && occurrence.Time > now)
            .OrderBy(occurrence => occurrence.Time)
            .FirstOrDefault();

    public PrayerOccurrence? CurrentIqamahCountdown(DateTime now)
    {
        return Occurrences
            .Where(occurrence => occurrence.IsIqamah)
            .Where(occurrence =>
            {
                DateTime adhanTime = occurrence.Time.AddMinutes(-occurrence.IqamahOffsetMinutes);
                return now >= adhanTime && now < occurrence.Time;
            })
            .OrderBy(occurrence => occurrence.Time)
            .FirstOrDefault();
    }
}

public sealed record WidgetState(
    string PrimaryLabel,
    string Countdown,
    string TimeLabel,
    string SecondaryLabel,
    bool IsIqamahCountdown,
    DateTime? TargetTime);
