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
    public string WidgetTheme { get; set; } = WidgetThemeOptions.GoldDarkBlue;
    public Dictionary<string, WidgetThemeCustomization> WidgetThemeCustomizations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
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
        WidgetThemeCustomizations ??= new Dictionary<string, WidgetThemeCustomization>(StringComparer.OrdinalIgnoreCase);

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

        if (!WidgetThemeOptions.All.Contains(WidgetTheme, StringComparer.OrdinalIgnoreCase))
        {
            WidgetTheme = WidgetThemeOptions.GoldDarkBlue;
        }

        if (!LocationModeOptions.All.Contains(LocationMode, StringComparer.OrdinalIgnoreCase))
        {
            LocationMode = LocationModeOptions.Auto;
        }

        foreach (WidgetThemeCustomization customization in WidgetThemeCustomizations.Values)
        {
            customization.Elements ??= new Dictionary<string, WidgetTextCustomization>(StringComparer.OrdinalIgnoreCase);
            customization.ShadowAlpha = customization.ShadowAlpha is null ? null : Math.Clamp(customization.ShadowAlpha.Value, 0, 255);
            customization.ShadowOffsetX = customization.ShadowOffsetX is null ? null : Math.Clamp(customization.ShadowOffsetX.Value, -12, 12);
            customization.ShadowOffsetY = customization.ShadowOffsetY is null ? null : Math.Clamp(customization.ShadowOffsetY.Value, -12, 12);
        }
    }
}

public sealed class WidgetThemeCustomization
{
    public int? ShadowAlpha { get; set; }
    public int? ShadowOffsetX { get; set; }
    public int? ShadowOffsetY { get; set; }
    public Dictionary<string, WidgetTextCustomization> Elements { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public WidgetThemeCustomization Clone()
    {
        return new WidgetThemeCustomization
        {
            ShadowAlpha = ShadowAlpha,
            ShadowOffsetX = ShadowOffsetX,
            ShadowOffsetY = ShadowOffsetY,
            Elements = Elements.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Clone(),
                StringComparer.OrdinalIgnoreCase)
        };
    }
}

public sealed class WidgetTextCustomization
{
    public int? X { get; set; }
    public int? Y { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public float? FontSize { get; set; }
    public string? FontFamily { get; set; }
    public string? Color { get; set; }
    public string? Alignment { get; set; }
    public bool? Visible { get; set; }

    public WidgetTextCustomization Clone()
    {
        return new WidgetTextCustomization
        {
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            FontSize = FontSize,
            FontFamily = FontFamily,
            Color = Color,
            Alignment = Alignment,
            Visible = Visible
        };
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

public static class WidgetThemeOptions
{
    public const string Simple = "Simple";
    public const string GoldDarkBlue = "GoldDarkBlue";
    public const string Glassy = "Glassy";
    public const string DarkPurple = "DarkPurple";

    public static readonly string[] All =
    [
        Simple,
        GoldDarkBlue,
        Glassy,
        DarkPurple
    ];
}
