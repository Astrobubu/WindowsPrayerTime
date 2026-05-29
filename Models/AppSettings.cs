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
    public Dictionary<string, WidgetThemeCustomization> WidgetThemeCustomizations { get; set; } = CreateDefaultWidgetThemeCustomizations();
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

    public static Dictionary<string, WidgetThemeCustomization> CreateDefaultWidgetThemeCustomizations() => new(StringComparer.OrdinalIgnoreCase)
    {
        [WidgetThemeOptions.GoldDarkBlue] = new WidgetThemeCustomization
        {
            Width = 260,
            Height = 145,
            ShadowAlpha = 82,
            ShadowOffsetX = 1,
            ShadowOffsetY = 1,
            ShadowBlur = 0,
            GlowAlpha = 135,
            GlowBlur = 0,
            GlowColor = "#EDBC5C",
            Elements = new Dictionary<string, WidgetTextCustomization>(StringComparer.OrdinalIgnoreCase)
            {
                ["Primary"] = new()
                {
                    X = 76, Y = 28, Width = 262, Height = 26,
                    FontSize = 17, FontFamily = "Raleway", Color = "#EDBC5C",
                    Alignment = "Center", Bold = false, Visible = true,
                    ShadowAlpha = 0, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 0, GlowBlur = 0, GlowColor = "#EDBC5C"
                },
                ["Time"] = new()
                {
                    X = 268, Y = 36, Width = 104, Height = 24,
                    FontSize = 14.5F, FontFamily = "Raleway", Color = "#C4A05C",
                    Alignment = "Far", Bold = false, Visible = false,
                    ShadowAlpha = 0, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 0, GlowBlur = 0, GlowColor = "#C4A05C"
                },
                ["Countdown"] = new()
                {
                    X = 19, Y = 76, Width = 385, Height = 98,
                    FontSize = 51, FontFamily = "Century Gothic", Color = "#ECB755",
                    Alignment = "Center", Bold = false, Visible = true,
                    ShadowAlpha = 0, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 48, GlowBlur = 3, GlowColor = "#FFEDC6"
                },
                ["Location"] = new()
                {
                    X = 36, Y = 201, Width = 343, Height = 22,
                    FontSize = 13, FontFamily = "Raleway", Color = "#ECCD8F",
                    Alignment = "Center", Bold = false, Visible = true,
                    ShadowAlpha = 0, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 0, GlowBlur = 0, GlowColor = "#ECCD8F"
                },
                ["Detail"] = new()
                {
                    X = 48, Y = 160, Width = 220, Height = 21,
                    FontSize = 7.5F, FontFamily = "Segoe UI", Color = "#C4A05C",
                    Alignment = "Near", Visible = false
                }
            }
        },
        [WidgetThemeOptions.Glassy] = new WidgetThemeCustomization
        {
            Width = 260,
            Height = 145,
            ShadowAlpha = 76,
            ShadowOffsetX = 1,
            ShadowOffsetY = 1,
            ShadowBlur = 2,
            GlowAlpha = 0,
            GlowBlur = 0,
            GlowColor = "#71F8FF",
            Elements = new Dictionary<string, WidgetTextCustomization>(StringComparer.OrdinalIgnoreCase)
            {
                ["Primary"] = new()
                {
                    X = 21, Y = 33, Width = 180, Height = 33,
                    FontSize = 22.5F, FontFamily = "Bahnschrift SemiCondensed", Color = "#71F8FF",
                    Alignment = "Near", Bold = false, Visible = true,
                    ShadowAlpha = 138, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 0, GlowBlur = 0, GlowColor = "#71F8FF"
                },
                ["Time"] = new()
                {
                    X = 245, Y = 34, Width = 146, Height = 33,
                    FontSize = 22.5F, FontFamily = "Bahnschrift SemiCondensed", Color = "#8FC5CF",
                    Alignment = "Far", Bold = false, Visible = true,
                    ShadowAlpha = 147, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 0, GlowBlur = 0, GlowColor = "#8FC5CF"
                },
                ["Countdown"] = new()
                {
                    X = 33, Y = 86, Width = 354, Height = 81,
                    FontSize = 57.5F, FontFamily = "Bahnschrift SemiCondensed", Color = "#EBFFFF",
                    Alignment = "Center", Bold = false, Visible = true,
                    ShadowAlpha = 0, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 0,
                    GlowAlpha = 69, GlowBlur = 6, GlowColor = "#71F8FF"
                },
                ["Location"] = new()
                {
                    X = 38, Y = 188, Width = 348, Height = 22,
                    FontSize = 16, FontFamily = "Bahnschrift SemiCondensed", Color = "#AEEAF0",
                    Alignment = "Center", Bold = false, Visible = true,
                    ShadowAlpha = 0, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 0, GlowBlur = 0, GlowColor = "#AEEAF0"
                },
                ["Detail"] = new()
                {
                    X = 42, Y = 158, Width = 224, Height = 18,
                    FontSize = 7.5F, FontFamily = "Segoe UI", Color = "#8FC5CF",
                    Alignment = "Near", Bold = false, Visible = false
                }
            }
        },
        [WidgetThemeOptions.DarkPurple] = new WidgetThemeCustomization
        {
            Width = 260,
            Height = 145,
            Elements = new Dictionary<string, WidgetTextCustomization>(StringComparer.OrdinalIgnoreCase)
            {
                ["Primary"] = new()
                {
                    X = 21, Y = 29, Width = 190, Height = 35,
                    FontSize = 21, FontFamily = "Bahnschrift SemiCondensed", Color = "#C284FF",
                    Alignment = "Near", Bold = false, Visible = true,
                    ShadowAlpha = 0, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 0, GlowBlur = 0, GlowColor = "#C284FF"
                },
                ["Time"] = new()
                {
                    X = 291, Y = 30, Width = 112, Height = 37,
                    FontSize = 21, FontFamily = "Bahnschrift SemiCondensed", Color = "#948BDC",
                    Alignment = "Far", Bold = false, Visible = true,
                    ShadowAlpha = 0, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 0, GlowBlur = 0, GlowColor = "#948BDC"
                },
                ["Countdown"] = new()
                {
                    X = 11, Y = 85, Width = 354, Height = 98,
                    FontSize = 60.5F, FontFamily = "Bahnschrift SemiCondensed", Color = "#F6F1FF",
                    Alignment = "Near", Bold = true, Visible = true,
                    ShadowAlpha = 0, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 60, GlowBlur = 3, GlowColor = "#C284FF"
                },
                ["Location"] = new()
                {
                    X = 22, Y = 187, Width = 339, Height = 28,
                    FontSize = 14.5F, FontFamily = "Bahnschrift SemiCondensed", Color = "#CDB9FF",
                    Alignment = "Near", Bold = false, Visible = true,
                    ShadowAlpha = 0, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 0, GlowBlur = 0, GlowColor = "#CDB9FF"
                },
                ["Detail"] = new()
                {
                    X = 26, Y = 225, Width = 210, Height = 18,
                    FontSize = 7.5F, FontFamily = "Segoe UI", Color = "#948BDC",
                    Alignment = "Near", Bold = false, Visible = false,
                    ShadowAlpha = 0, ShadowOffsetX = 1, ShadowOffsetY = 1, ShadowBlur = 2,
                    GlowAlpha = 0, GlowBlur = 0, GlowColor = "#948BDC"
                }
            }
        }
    };

    public static WidgetThemeCustomization? CreateDefaultWidgetThemeCustomization(string themeKey)
    {
        Dictionary<string, WidgetThemeCustomization> defaults = CreateDefaultWidgetThemeCustomizations();
        return defaults.TryGetValue(themeKey, out WidgetThemeCustomization? customization)
            ? customization.Clone()
            : null;
    }

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

        foreach (var pair in CreateDefaultWidgetThemeCustomizations())
        {
            if (!WidgetThemeCustomizations.TryGetValue(pair.Key, out WidgetThemeCustomization? existing))
            {
                WidgetThemeCustomizations[pair.Key] = pair.Value.Clone();
                continue;
            }

            existing.Width ??= pair.Value.Width;
            existing.Height ??= pair.Value.Height;
            existing.Elements ??= new Dictionary<string, WidgetTextCustomization>(StringComparer.OrdinalIgnoreCase);
            foreach (var elementPair in pair.Value.Elements)
            {
                existing.Elements.TryAdd(elementPair.Key, elementPair.Value.Clone());
            }
        }

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
            customization.Width = customization.Width is null ? null : Math.Clamp(customization.Width.Value, 160, 900);
            customization.Height = customization.Height is null ? null : Math.Clamp(customization.Height.Value, 80, 520);
            customization.ShadowAlpha = customization.ShadowAlpha is null ? null : Math.Clamp(customization.ShadowAlpha.Value, 0, 255);
            customization.ShadowOffsetX = customization.ShadowOffsetX is null ? null : Math.Clamp(customization.ShadowOffsetX.Value, -12, 12);
            customization.ShadowOffsetY = customization.ShadowOffsetY is null ? null : Math.Clamp(customization.ShadowOffsetY.Value, -12, 12);
            customization.ShadowBlur = customization.ShadowBlur is null ? null : Math.Clamp(customization.ShadowBlur.Value, 0, 12);
            customization.GlowAlpha = customization.GlowAlpha is null ? null : Math.Clamp(customization.GlowAlpha.Value, 0, 255);
            customization.GlowBlur = customization.GlowBlur is null ? null : Math.Clamp(customization.GlowBlur.Value, 0, 16);

            foreach (WidgetTextCustomization element in customization.Elements.Values)
            {
                element.ShadowAlpha = element.ShadowAlpha is null ? null : Math.Clamp(element.ShadowAlpha.Value, 0, 255);
                element.ShadowOffsetX = element.ShadowOffsetX is null ? null : Math.Clamp(element.ShadowOffsetX.Value, -12, 12);
                element.ShadowOffsetY = element.ShadowOffsetY is null ? null : Math.Clamp(element.ShadowOffsetY.Value, -12, 12);
                element.ShadowBlur = element.ShadowBlur is null ? null : Math.Clamp(element.ShadowBlur.Value, 0, 12);
                element.GlowAlpha = element.GlowAlpha is null ? null : Math.Clamp(element.GlowAlpha.Value, 0, 255);
                element.GlowBlur = element.GlowBlur is null ? null : Math.Clamp(element.GlowBlur.Value, 0, 16);
            }
        }
    }
}

public sealed class WidgetThemeCustomization
{
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? ShadowAlpha { get; set; }
    public int? ShadowOffsetX { get; set; }
    public int? ShadowOffsetY { get; set; }
    public int? ShadowBlur { get; set; }
    public int? GlowAlpha { get; set; }
    public int? GlowBlur { get; set; }
    public string? GlowColor { get; set; }
    public Dictionary<string, WidgetTextCustomization> Elements { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public WidgetThemeCustomization Clone()
    {
        return new WidgetThemeCustomization
        {
            Width = Width,
            Height = Height,
            ShadowAlpha = ShadowAlpha,
            ShadowOffsetX = ShadowOffsetX,
            ShadowOffsetY = ShadowOffsetY,
            ShadowBlur = ShadowBlur,
            GlowAlpha = GlowAlpha,
            GlowBlur = GlowBlur,
            GlowColor = GlowColor,
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
    public bool? Bold { get; set; }
    public bool? Visible { get; set; }
    public int? ShadowAlpha { get; set; }
    public int? ShadowOffsetX { get; set; }
    public int? ShadowOffsetY { get; set; }
    public int? ShadowBlur { get; set; }
    public int? GlowAlpha { get; set; }
    public int? GlowBlur { get; set; }
    public string? GlowColor { get; set; }

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
            Bold = Bold,
            Visible = Visible,
            ShadowAlpha = ShadowAlpha,
            ShadowOffsetX = ShadowOffsetX,
            ShadowOffsetY = ShadowOffsetY,
            ShadowBlur = ShadowBlur,
            GlowAlpha = GlowAlpha,
            GlowBlur = GlowBlur,
            GlowColor = GlowColor
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
