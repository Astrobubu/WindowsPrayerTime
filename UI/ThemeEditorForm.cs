using System.Drawing.Drawing2D;
using System.Drawing.Text;
using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.UI;

public sealed class ThemeEditorForm : Form
{
    private readonly ComboBox _themeBox = new();
    private readonly ComboBox _elementBox = new();
    private readonly NumericUpDown _widgetWidthBox = CreateNumber(160, 900, 0);
    private readonly NumericUpDown _widgetHeightBox = CreateNumber(80, 520, 0);
    private readonly NumericUpDown _xBox = CreateNumber(-200, 800, 0);
    private readonly NumericUpDown _yBox = CreateNumber(-200, 800, 0);
    private readonly NumericUpDown _widthBox = CreateNumber(0, 800, 0);
    private readonly NumericUpDown _heightBox = CreateNumber(0, 800, 0);
    private readonly NumericUpDown _fontSizeBox = CreateNumber(4, 96, 1);
    private readonly NumericUpDown _shadowAlphaBox = CreateNumber(0, 255, 0);
    private readonly NumericUpDown _shadowXBox = CreateNumber(-12, 12, 0);
    private readonly NumericUpDown _shadowYBox = CreateNumber(-12, 12, 0);
    private readonly ComboBox _fontBox = new();
    private readonly ComboBox _alignmentBox = new();
    private readonly TextBox _colorBox = new();
    private readonly CheckBox _visibleBox = new();
    private readonly ThemePreviewControl _preview = new();
    private bool _loading;

    public AppSettings Settings { get; private set; }

    public ThemeEditorForm(AppSettings settings)
    {
        Settings = Clone(settings);
        Settings.EnsureDefaults();
        if (string.Equals(Settings.WidgetTheme, WidgetThemeOptions.Simple, StringComparison.OrdinalIgnoreCase))
        {
            Settings.WidgetTheme = WidgetThemeOptions.GoldDarkBlue;
        }

        Text = "Widget Theme Editor";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(960, 610);
        Size = new Size(1040, 660);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = 2
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _preview.Dock = DockStyle.Fill;
        _preview.BackColor = Color.FromArgb(18, 18, 22);
        root.Controls.Add(_preview, 0, 0);

        root.Controls.Add(BuildControls(), 1, 0);
        root.Controls.Add(BuildButtons(), 0, 1);
        root.SetColumnSpan(root.GetControlFromPosition(0, 1)!, 2);

        Controls.Add(root);
        ConfigureDropdowns();
        LoadTheme();
    }

    private Control BuildControls()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 0,
            AutoScroll = true,
            Padding = new Padding(10, 0, 0, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));

        AddRow(layout, "Theme", _themeBox);
        AddRow(layout, "Widget width", _widgetWidthBox, "px");
        AddRow(layout, "Widget height", _widgetHeightBox, "px");
        AddRow(layout, "Text element", _elementBox);
        AddSeparator(layout, "Position");
        AddRow(layout, "X", _xBox, "px");
        AddRow(layout, "Y", _yBox, "px");
        AddRow(layout, "Width", _widthBox, "px");
        AddRow(layout, "Height", _heightBox, "px");
        AddSeparator(layout, "Type");
        AddRow(layout, "Font", _fontBox);
        AddRow(layout, "Font size", _fontSizeBox, "pt");
        AddRow(layout, "Alignment", _alignmentBox);

        var colorPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        colorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        colorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        _colorBox.Dock = DockStyle.Fill;
        var colorButton = new Button { Text = "...", Dock = DockStyle.Fill };
        colorButton.Click += (_, _) => PickColor();
        colorPanel.Controls.Add(_colorBox, 0, 0);
        colorPanel.Controls.Add(colorButton, 1, 0);
        AddRow(layout, "Color", colorPanel);

        _visibleBox.Text = "Show this text";
        AddFullRow(layout, _visibleBox);
        AddSeparator(layout, "Shadow");
        AddRow(layout, "Opacity", _shadowAlphaBox);
        AddRow(layout, "Offset X", _shadowXBox, "px");
        AddRow(layout, "Offset Y", _shadowYBox, "px");

        foreach (Control control in new Control[]
        {
            _widgetWidthBox, _widgetHeightBox, _xBox, _yBox, _widthBox, _heightBox, _fontSizeBox, _shadowAlphaBox, _shadowXBox, _shadowYBox
        })
        {
            if (control is NumericUpDown box)
            {
                box.ValueChanged += (_, _) => ApplyControlValues();
            }
        }

        _fontBox.SelectedIndexChanged += (_, _) => ApplyControlValues();
        _alignmentBox.SelectedIndexChanged += (_, _) => ApplyControlValues();
        _colorBox.TextChanged += (_, _) => ApplyControlValues();
        _visibleBox.CheckedChanged += (_, _) => ApplyControlValues();
        _themeBox.SelectedIndexChanged += (_, _) =>
        {
            if (_loading)
            {
                return;
            }

            Settings.WidgetTheme = _themeBox.SelectedValue as string ?? WidgetThemeOptions.GoldDarkBlue;
            LoadTheme();
        };
        _elementBox.SelectedIndexChanged += (_, _) =>
        {
            if (!_loading)
            {
                LoadElement();
            }
        };

        return layout;
    }

    private Control BuildButtons()
    {
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var saveButton = new Button { Text = "Save", Width = 90, Height = 30 };
        saveButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        buttons.Controls.Add(saveButton);

        var cancelButton = new Button { Text = "Cancel", Width = 90, Height = 30 };
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        buttons.Controls.Add(cancelButton);

        var resetThemeButton = new Button { Text = "Reset theme", Width = 104, Height = 30 };
        resetThemeButton.Click += (_, _) => ResetTheme();
        buttons.Controls.Add(resetThemeButton);

        var resetElementButton = new Button { Text = "Reset text", Width = 96, Height = 30 };
        resetElementButton.Click += (_, _) => ResetElement();
        buttons.Controls.Add(resetElementButton);

        return buttons;
    }

    private void ConfigureDropdowns()
    {
        _themeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeBox.DisplayMember = nameof(ThemeOption.Text);
        _themeBox.ValueMember = nameof(ThemeOption.Key);
        _themeBox.DataSource = WidgetThemeCatalog.All
            .Where(theme => !theme.IsSimple)
            .Select(theme => new ThemeOption(theme.DisplayName, theme.Key))
            .ToList();

        _elementBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _elementBox.DisplayMember = nameof(ThemeOption.Text);
        _elementBox.ValueMember = nameof(ThemeOption.Key);
        _elementBox.DataSource = new[]
        {
            new ThemeOption("Prayer label", WidgetTextElementKeys.Primary),
            new ThemeOption("Prayer time", WidgetTextElementKeys.Time),
            new ThemeOption("Countdown", WidgetTextElementKeys.Countdown),
            new ThemeOption("Location", WidgetTextElementKeys.Location),
            new ThemeOption("Detail line", WidgetTextElementKeys.Detail)
        }.ToList();

        _fontBox.DropDownStyle = ComboBoxStyle.DropDown;
        _fontBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _fontBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        foreach (string fontName in new InstalledFontCollection().Families.Select(family => family.Name).OrderBy(name => name))
        {
            _fontBox.Items.Add(fontName);
        }

        _alignmentBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _alignmentBox.Items.AddRange(["Left", "Center", "Right"]);

        _visibleBox.Checked = true;
    }

    private void LoadTheme()
    {
        _loading = true;
        WidgetThemeDefinition theme = CurrentTheme();
        _themeBox.SelectedValue = theme.Key;
        _widgetWidthBox.Value = CurrentCustomization().Width ?? theme.Size.Width;
        _widgetHeightBox.Value = CurrentCustomization().Height ?? theme.Size.Height;
        _shadowAlphaBox.Value = CurrentCustomization().ShadowAlpha ?? theme.ShadowAlpha;
        _shadowXBox.Value = CurrentCustomization().ShadowOffsetX ?? 1;
        _shadowYBox.Value = CurrentCustomization().ShadowOffsetY ?? 1;
        _loading = false;
        LoadElement();
        RefreshPreview();
    }

    private void LoadElement()
    {
        _loading = true;
        string elementKey = CurrentElementKey();
        ThemeTextDefaults defaults = GetTextDefaults(CurrentTheme(), elementKey);
        WidgetTextCustomization? overrideValue = GetElementOverride(elementKey);
        Rectangle bounds = GetBounds(overrideValue, defaults.Bounds);

        _xBox.Value = Math.Clamp(bounds.X, (int)_xBox.Minimum, (int)_xBox.Maximum);
        _yBox.Value = Math.Clamp(bounds.Y, (int)_yBox.Minimum, (int)_yBox.Maximum);
        _widthBox.Value = Math.Clamp(bounds.Width, (int)_widthBox.Minimum, (int)_widthBox.Maximum);
        _heightBox.Value = Math.Clamp(bounds.Height, (int)_heightBox.Minimum, (int)_heightBox.Maximum);
        _fontBox.Text = overrideValue?.FontFamily ?? defaults.FontFamily;
        _fontSizeBox.Value = (decimal)Math.Clamp(overrideValue?.FontSize ?? defaults.FontSize, (float)_fontSizeBox.Minimum, (float)_fontSizeBox.Maximum);
        _alignmentBox.SelectedItem = ToAlignmentText(ParseAlignment(overrideValue?.Alignment, defaults.Alignment));
        _colorBox.Text = overrideValue?.Color ?? ColorTranslator.ToHtml(defaults.Color);
        _visibleBox.Checked = overrideValue?.Visible ?? true;
        _loading = false;
        RefreshPreview();
    }

    private void ApplyControlValues()
    {
        if (_loading)
        {
            return;
        }

        WidgetThemeCustomization customization = CurrentCustomization();
        customization.Width = (int)_widgetWidthBox.Value;
        customization.Height = (int)_widgetHeightBox.Value;
        customization.ShadowAlpha = (int)_shadowAlphaBox.Value;
        customization.ShadowOffsetX = (int)_shadowXBox.Value;
        customization.ShadowOffsetY = (int)_shadowYBox.Value;

        string elementKey = CurrentElementKey();
        WidgetTextCustomization element = EnsureElementOverride(elementKey);
        element.X = (int)_xBox.Value;
        element.Y = (int)_yBox.Value;
        element.Width = (int)_widthBox.Value;
        element.Height = (int)_heightBox.Value;
        element.FontFamily = string.IsNullOrWhiteSpace(_fontBox.Text) ? null : _fontBox.Text.Trim();
        element.FontSize = (float)_fontSizeBox.Value;
        element.Alignment = _alignmentBox.SelectedItem?.ToString() switch
        {
            "Center" => "Center",
            "Right" => "Far",
            _ => "Near"
        };
        element.Color = string.IsNullOrWhiteSpace(_colorBox.Text) ? null : _colorBox.Text.Trim();
        element.Visible = _visibleBox.Checked;
        RefreshPreview();
    }

    private void PickColor()
    {
        using var dialog = new ColorDialog
        {
            FullOpen = true,
            Color = ParseColor(_colorBox.Text, Color.White)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _colorBox.Text = ColorTranslator.ToHtml(dialog.Color);
        }
    }

    private void ResetTheme()
    {
        Settings.WidgetThemeCustomizations.Remove(CurrentTheme().Key);
        LoadTheme();
    }

    private void ResetElement()
    {
        CurrentCustomization().Elements.Remove(CurrentElementKey());
        LoadElement();
    }

    private void RefreshPreview()
    {
        _preview.UpdatePreview(
            CurrentTheme(),
            Settings.WidgetThemeCustomizations.TryGetValue(CurrentTheme().Key, out WidgetThemeCustomization? customization) ? customization : null);
    }

    private WidgetThemeDefinition CurrentTheme() => WidgetThemeCatalog.Get(Settings.WidgetTheme);

    private string CurrentElementKey() => _elementBox.SelectedValue as string ?? WidgetTextElementKeys.Countdown;

    private WidgetThemeCustomization CurrentCustomization()
    {
        if (!Settings.WidgetThemeCustomizations.TryGetValue(CurrentTheme().Key, out WidgetThemeCustomization? customization))
        {
            customization = new WidgetThemeCustomization();
            Settings.WidgetThemeCustomizations[CurrentTheme().Key] = customization;
        }

        return customization;
    }

    private WidgetTextCustomization EnsureElementOverride(string elementKey)
    {
        WidgetThemeCustomization customization = CurrentCustomization();
        if (!customization.Elements.TryGetValue(elementKey, out WidgetTextCustomization? element))
        {
            element = new WidgetTextCustomization();
            customization.Elements[elementKey] = element;
        }

        return element;
    }

    private WidgetTextCustomization? GetElementOverride(string elementKey)
    {
        return Settings.WidgetThemeCustomizations.TryGetValue(CurrentTheme().Key, out WidgetThemeCustomization? customization) &&
            customization.Elements.TryGetValue(elementKey, out WidgetTextCustomization? element)
                ? element
                : null;
    }

    private static AppSettings Clone(AppSettings settings)
    {
        settings.EnsureDefaults();
        return new AppSettings
        {
            LocationMode = settings.LocationMode,
            City = settings.City,
            Country = settings.Country,
            State = settings.State,
            Latitude = settings.Latitude,
            Longitude = settings.Longitude,
            DetectedCity = settings.DetectedCity,
            DetectedRegion = settings.DetectedRegion,
            DetectedCountry = settings.DetectedCountry,
            DetectedTimeZone = settings.DetectedTimeZone,
            LocationDetectedAt = settings.LocationDetectedAt,
            UseAutomaticCalculationMethod = settings.UseAutomaticCalculationMethod,
            CalculationMethod = settings.CalculationMethod,
            School = settings.School,
            ShowDesktopWidget = settings.ShowDesktopWidget,
            StartWithWindows = settings.StartWithWindows,
            AdhanAlertsEnabled = settings.AdhanAlertsEnabled,
            IqamahAlertsEnabled = settings.IqamahAlertsEnabled,
            SoundEnabled = settings.SoundEnabled,
            IqamahRequiresPcActivity = settings.IqamahRequiresPcActivity,
            ActivityThresholdMinutes = settings.ActivityThresholdMinutes,
            RefreshEveryHours = settings.RefreshEveryHours,
            PopupAutoCloseSeconds = settings.PopupAutoCloseSeconds,
            AdhanLeadMinutes = settings.AdhanLeadMinutes,
            ShowIqamahCountdownAfterAdhan = settings.ShowIqamahCountdownAfterAdhan,
            WidgetPlacement = settings.WidgetPlacement,
            WidgetTheme = settings.WidgetTheme,
            WidgetThemeCustomizations = settings.WidgetThemeCustomizations.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Clone(),
                StringComparer.OrdinalIgnoreCase),
            IqamahOffsetsMinutes = new Dictionary<string, int>(settings.IqamahOffsetsMinutes, StringComparer.OrdinalIgnoreCase),
            PrayerAdjustmentsMinutes = new Dictionary<string, int>(settings.PrayerAdjustmentsMinutes, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static void AddRow(TableLayoutPanel layout, string labelText, Control input, string suffix = "")
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.Controls.Add(new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        input.Dock = DockStyle.Fill;
        layout.Controls.Add(input, 1, row);
        layout.Controls.Add(new Label
        {
            Text = suffix,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(90, 90, 90),
            TextAlign = ContentAlignment.MiddleLeft
        }, 2, row);
    }

    private static void AddFullRow(TableLayoutPanel layout, Control control)
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(control, 0, row);
        layout.SetColumnSpan(control, 3);
    }

    private static void AddSeparator(TableLayoutPanel layout, string text)
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(38, 83, 93),
            TextAlign = ContentAlignment.BottomLeft
        };
        layout.Controls.Add(label, 0, row);
        layout.SetColumnSpan(label, 3);
    }

    private static NumericUpDown CreateNumber(int min, int max, int decimals)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimals,
            Increment = decimals > 0 ? 0.5M : 1M
        };
    }

    internal static ThemeTextDefaults GetTextDefaults(WidgetThemeDefinition theme, string elementKey)
    {
        return elementKey switch
        {
            WidgetTextElementKeys.Primary => new ThemeTextDefaults(theme.PrimaryRect, theme.TitleFont, theme.PrimaryFontSize, theme.AccentColor, theme.PrimaryAlignment),
            WidgetTextElementKeys.Time => new ThemeTextDefaults(theme.TimeRect, theme.BodyFont, theme.TimeFontSize, theme.MutedColor, theme.TimeAlignment),
            WidgetTextElementKeys.Countdown => new ThemeTextDefaults(theme.CountdownRect, theme.DisplayFont, theme.CountdownFontSize, theme.CountdownColor, theme.CountdownAlignment),
            WidgetTextElementKeys.Location => new ThemeTextDefaults(theme.LocationRect, theme.BodyFont, theme.LocationFontSize, theme.LocationColor, theme.LocationAlignment),
            WidgetTextElementKeys.Detail => new ThemeTextDefaults(theme.DetailRect, theme.BodyFont, theme.DetailFontSize, theme.MutedColor, theme.DetailAlignment),
            _ => new ThemeTextDefaults(Rectangle.Empty, theme.BodyFont, 9, theme.MutedColor, StringAlignment.Near)
        };
    }

    internal static Rectangle GetBounds(WidgetTextCustomization? element, Rectangle defaultBounds)
    {
        return new Rectangle(
            element?.X ?? defaultBounds.X,
            element?.Y ?? defaultBounds.Y,
            element?.Width ?? defaultBounds.Width,
            element?.Height ?? defaultBounds.Height);
    }

    internal static Color ParseColor(string? value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch
        {
            return fallback;
        }
    }

    internal static StringAlignment ParseAlignment(string? value, StringAlignment fallback)
    {
        return value?.ToLowerInvariant() switch
        {
            "center" => StringAlignment.Center,
            "far" or "right" => StringAlignment.Far,
            "near" or "left" => StringAlignment.Near,
            _ => fallback
        };
    }

    private static string ToAlignmentText(StringAlignment alignment)
    {
        return alignment switch
        {
            StringAlignment.Center => "Center",
            StringAlignment.Far => "Right",
            _ => "Left"
        };
    }

    private sealed record ThemeOption(string Text, string Key);
}

internal sealed record ThemeTextDefaults(Rectangle Bounds, string FontFamily, float FontSize, Color Color, StringAlignment Alignment);

internal sealed class ThemePreviewControl : Control
{
    private readonly WidgetState _sampleState = new(
        "Next Isha",
        "24:18",
        "8:31 PM",
        "Dubai, United Arab Emirates",
        false,
        DateTime.Now.AddMinutes(24));

    private WidgetThemeDefinition _theme = WidgetThemeCatalog.Get(WidgetThemeOptions.GoldDarkBlue);
    private WidgetThemeCustomization? _customization;
    private Image? _themeImage;

    public ThemePreviewControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    public void UpdatePreview(WidgetThemeDefinition theme, WidgetThemeCustomization? customization)
    {
        bool themeChanged = !string.Equals(_theme.Key, theme.Key, StringComparison.OrdinalIgnoreCase);
        _theme = theme;
        _customization = customization;

        if (themeChanged)
        {
            _themeImage?.Dispose();
            _themeImage = null;
            string imagePath = Path.Combine(AppContext.BaseDirectory, _theme.AssetPath);
            if (File.Exists(imagePath))
            {
                _themeImage = Image.FromFile(imagePath);
            }
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        e.Graphics.Clear(BackColor);

        Size previewSize = GetThemeSize();
        Rectangle target = FitRectangle(previewSize, ClientRectangle);
        if (_themeImage is not null)
        {
            e.Graphics.DrawImage(_themeImage, target);
        }

        GraphicsState state = e.Graphics.Save();
        e.Graphics.TranslateTransform(target.X, target.Y);
        e.Graphics.ScaleTransform(target.Width / (float)_theme.Size.Width, target.Height / (float)_theme.Size.Height);

        DrawElement(e.Graphics, WidgetTextElementKeys.Primary, _sampleState.PrimaryLabel, _theme.AccentColor, FontStyle.Bold);
        DrawElement(e.Graphics, WidgetTextElementKeys.Time, _sampleState.TimeLabel, _theme.MutedColor, FontStyle.Regular);
        DrawElement(e.Graphics, WidgetTextElementKeys.Countdown, _sampleState.Countdown, _theme.CountdownColor, FontStyle.Bold);
        DrawElement(e.Graphics, WidgetTextElementKeys.Location, _sampleState.SecondaryLabel, _theme.LocationColor, FontStyle.Regular);
        DrawElement(e.Graphics, WidgetTextElementKeys.Detail, "Next prayer countdown", _theme.MutedColor, FontStyle.Regular);
        e.Graphics.Restore(state);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _themeImage?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void DrawElement(Graphics graphics, string key, string text, Color defaultColor, FontStyle style)
    {
        WidgetTextCustomization? element = _customization?.Elements.TryGetValue(key, out WidgetTextCustomization? found) == true ? found : null;
        if (element?.Visible == false)
        {
            return;
        }

        ThemeTextDefaults defaults = ThemeEditorForm.GetTextDefaults(_theme, key);
        Rectangle bounds = ThemeEditorForm.GetBounds(element, defaults.Bounds);
        string fontFamily = element?.FontFamily ?? defaults.FontFamily;
        float fontSize = element?.FontSize ?? defaults.FontSize;
        Color color = ThemeEditorForm.ParseColor(element?.Color, defaultColor);
        StringAlignment alignment = ThemeEditorForm.ParseAlignment(element?.Alignment, defaults.Alignment);
        int shadowAlpha = Math.Clamp(_customization?.ShadowAlpha ?? _theme.ShadowAlpha, 0, 255);

        using var font = new Font(fontFamily, fontSize, style, GraphicsUnit.Point);
        using var brush = new SolidBrush(color);
        using var shadowBrush = new SolidBrush(Color.FromArgb(shadowAlpha, Color.Black));
        using var format = new StringFormat
        {
            Alignment = alignment,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        Rectangle shadowBounds = bounds;
        shadowBounds.Offset(_customization?.ShadowOffsetX ?? 1, _customization?.ShadowOffsetY ?? 1);
        graphics.DrawString(text, font, shadowBrush, shadowBounds, format);
        graphics.DrawString(text, font, brush, bounds, format);
    }

    private static Rectangle FitRectangle(Size source, Rectangle target)
    {
        float scale = Math.Min(target.Width / (float)source.Width, target.Height / (float)source.Height);
        int width = (int)(source.Width * scale);
        int height = (int)(source.Height * scale);
        return new Rectangle(
            target.X + (target.Width - width) / 2,
            target.Y + (target.Height - height) / 2,
            width,
            height);
    }

    private Size GetThemeSize()
    {
        return new Size(
            Math.Clamp(_customization?.Width ?? _theme.Size.Width, 160, 900),
            Math.Clamp(_customization?.Height ?? _theme.Size.Height, 80, 520));
    }
}
