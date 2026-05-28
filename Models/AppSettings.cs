namespace WindowsPrayerTime.Models;

public sealed class AppSettings
{
    public string LocationMode { get; set; } = LocationModeOptions.Auto;
    public string City { get; set; } = "Dubai";
    public string Country { get; set; } = "United Arab Emirates";
    public string State { get; set; } = "";
    public double Latitude { get; set; } = 25.2048;
    public double Longitude { get; set; } = 55.2708;
    public string DetectedCity { get; set; } = "";
    public string DetectedRegion { get; set; } = "";
    public string DetectedCountry { get; set; } = "";
    public string DetectedTimeZone { get; set; } = "";
    public DateTime? LocationDetectedAt { get; set; }
    public bool UseAutomaticCalculationMethod { get; set; } = true;
    public int CalculationMethod { get; set; } = 16;
    public int School { get; set; } = 0;
    public bool ShowDesktopWidget { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool AdhanAlertsEnabled { get; set; } = true;
    public bool IqamahAlertsEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool IqamahRequiresPcActivity { get; set; } = true;
    public int ActivityThresholdMinutes { get; set; } = 5;
    public int RefreshEveryHours { get; set; } = 6;
    public int PopupAutoCloseSeconds { get; set; } = 12;
    public int AdhanLeadMinutes { get; set; } = 0;
    public bool ShowIqamahCountdownAfterAdhan { get; set; } = true;
    public string WidgetPlacement { get; set; } = WidgetPlacementOptions.AboveTaskbar;
    public Dictionary<string, int> IqamahOffsetsMinutes { get; set; } = CreateDefaultIqamahOffsets();
    public Dictionary<string, int> PrayerAdjustmentsMinutes { get; set; } = CreateDefaultPrayerAdjustments();

    public string LocationLabel
    {
        get
        {
            if (string.Equals(LocationMode, LocationModeOptions.Auto, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(DetectedCity) &&
                !string.IsNullOrWhiteSpace(DetectedCountry))
            {
                var detectedParts = new[] { DetectedCity, DetectedRegion, DetectedCountry }
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                return string.Join(", ", detectedParts);
            }

            if (string.Equals(LocationMode, LocationModeOptions.Coordinates, StringComparison.OrdinalIgnoreCase))
            {
                return $"{Latitude:0.####}, {Longitude:0.####}";
            }

            var parts = new[] { City, State, Country }.Where(part => !string.IsNullOrWhiteSpace(part));
            return string.Join(", ", parts);
        }
    }

    public static Dictionary<string, int> CreateDefaultIqamahOffsets() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Fajr"] = 25,
        ["Dhuhr"] = 20,
        ["Asr"] = 20,
        ["Maghrib"] = 5,
        ["Isha"] = 20
    };

    public static Dictionary<string, int> CreateDefaultPrayerAdjustments() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Fajr"] = 0,
        ["Dhuhr"] = 0,
        ["Asr"] = 0,
        ["Maghrib"] = 0,
        ["Isha"] = 0
    };

    public int GetIqamahOffset(string prayerName)
    {
        if (IqamahOffsetsMinutes.TryGetValue(prayerName, out int minutes))
        {
            return Math.Clamp(minutes, 0, 120);
        }

        return CreateDefaultIqamahOffsets().GetValueOrDefault(prayerName, 20);
    }

    public int GetPrayerAdjustment(string prayerName)
    {
        if (PrayerAdjustmentsMinutes.TryGetValue(prayerName, out int minutes))
        {
            return Math.Clamp(minutes, -60, 60);
        }

        return 0;
    }

    public void EnsureDefaults()
    {
        IqamahOffsetsMinutes ??= CreateDefaultIqamahOffsets();
        PrayerAdjustmentsMinutes ??= CreateDefaultPrayerAdjustments();

        foreach (var pair in CreateDefaultIqamahOffsets())
        {
            IqamahOffsetsMinutes.TryAdd(pair.Key, pair.Value);
        }

        foreach (var pair in CreateDefaultPrayerAdjustments())
        {
            PrayerAdjustmentsMinutes.TryAdd(pair.Key, pair.Value);
        }

        if (string.IsNullOrWhiteSpace(City))
        {
            City = "Dubai";
        }

        if (string.IsNullOrWhiteSpace(Country))
        {
            Country = "United Arab Emirates";
        }

        RefreshEveryHours = Math.Clamp(RefreshEveryHours, 1, 24);
        ActivityThresholdMinutes = Math.Clamp(ActivityThresholdMinutes, 1, 120);
        PopupAutoCloseSeconds = Math.Clamp(PopupAutoCloseSeconds, 5, 120);
        Latitude = Math.Clamp(Latitude, -90, 90);
        Longitude = Math.Clamp(Longitude, -180, 180);

        if (!WidgetPlacementOptions.All.Contains(WidgetPlacement, StringComparer.OrdinalIgnoreCase))
        {
            WidgetPlacement = WidgetPlacementOptions.AboveTaskbar;
        }

        if (!LocationModeOptions.All.Contains(LocationMode, StringComparer.OrdinalIgnoreCase))
        {
            LocationMode = LocationModeOptions.Auto;
        }
    }
}

public static class LocationModeOptions
{
    public const string Auto = "Auto";
    public const string City = "City";
    public const string Coordinates = "Coordinates";

    public static readonly string[] All =
    [
        Auto,
        City,
        Coordinates
    ];
}

public static class WidgetPlacementOptions
{
    public const string AboveTaskbar = "AboveTaskbar";
    public const string TaskbarBand = "TaskbarBand";

    public static readonly string[] All =
    [
        AboveTaskbar,
        TaskbarBand
    ];
}
