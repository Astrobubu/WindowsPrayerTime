using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.UI;

public sealed class SettingsForm : Form
{
    private readonly TextBox _cityBox = new();
    private readonly TextBox _countryBox = new();
    private readonly TextBox _stateBox = new();
    private readonly ComboBox _locationModeBox = new();
    private readonly NumericUpDown _latitudeBox = new();
    private readonly NumericUpDown _longitudeBox = new();
    private readonly ComboBox _methodBox = new();
    private readonly ComboBox _schoolBox = new();
    private readonly CheckBox _showWidgetBox = new();
    private readonly ComboBox _placementBox = new();
    private readonly ComboBox _themeBox = new();
    private readonly CheckBox _startWithWindowsBox = new();
    private readonly CheckBox _adhanAlertsBox = new();
    private readonly CheckBox _iqamahAlertsBox = new();
    private readonly CheckBox _soundBox = new();
    private readonly CheckBox _iqamahActivityBox = new();
    private readonly NumericUpDown _activityThreshold = new();
    private readonly NumericUpDown _refreshHours = new();
    private readonly NumericUpDown _popupSeconds = new();
    private readonly Dictionary<string, NumericUpDown> _iqamahOffsetBoxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, NumericUpDown> _adjustmentBoxes = new(StringComparer.OrdinalIgnoreCase);

    public AppSettings Settings { get; private set; }

    public SettingsForm(AppSettings settings)
    {
        Settings = Clone(settings);
        Text = "Prayer Time Settings";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(560, 620);
        Size = new Size(620, 700);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildLocationPage());
        tabs.TabPages.Add(BuildIqamahPage());
        tabs.TabPages.Add(BuildAlertsPage());
        root.Controls.Add(tabs, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var saveButton = new Button { Text = "Save", Width = 90, Height = 30 };
        saveButton.Click += (_, _) => SaveAndClose();
        buttons.Controls.Add(saveButton);

        var cancelButton = new Button { Text = "Cancel", Width = 90, Height = 30 };
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        buttons.Controls.Add(cancelButton);

        root.Controls.Add(buttons, 0, 1);
        Controls.Add(root);

        LoadSettings();
    }

    private TabPage BuildLocationPage()
    {
        var page = new TabPage("Location");
        var layout = CreateTwoColumnLayout();

        _locationModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _locationModeBox.Items.Add(new ComboItem("Auto-detect by network", 0));
        _locationModeBox.Items.Add(new ComboItem("Manual city/country", 1));
        _locationModeBox.Items.Add(new ComboItem("Manual coordinates", 2));
        _locationModeBox.DisplayMember = nameof(ComboItem.Text);
        _locationModeBox.ValueMember = nameof(ComboItem.Value);

        ConfigureCoordinateBox(_latitudeBox, -90, 90);
        ConfigureCoordinateBox(_longitudeBox, -180, 180);

        _methodBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _methodBox.DisplayMember = nameof(MethodOption.DisplayName);
        _methodBox.ValueMember = nameof(MethodOption.Id);
        _methodBox.DataSource = MethodOptions();

        _schoolBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _schoolBox.Items.Add(new ComboItem("Shafi / standard Asr", 0));
        _schoolBox.Items.Add(new ComboItem("Hanafi Asr", 1));
        _schoolBox.DisplayMember = nameof(ComboItem.Text);
        _schoolBox.ValueMember = nameof(ComboItem.Value);

        AddRow(layout, "Location source", _locationModeBox);
        AddRow(layout, "City fallback", _cityBox);
        AddRow(layout, "Country", _countryBox);
        AddRow(layout, "State", _stateBox);
        AddRow(layout, "Latitude", _latitudeBox);
        AddRow(layout, "Longitude", _longitudeBox);
        AddRow(layout, "Calculation method", _methodBox);
        AddRow(layout, "School", _schoolBox);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildIqamahPage()
    {
        var page = new TabPage("Iqamah");
        var layout = CreateTwoColumnLayout();

        foreach (string prayer in PrayerNames.Alerted)
        {
            var offsetBox = CreateNumber(0, 120);
            _iqamahOffsetBoxes[prayer] = offsetBox;
            AddRow(layout, prayer + " iqamah after Adhan", offsetBox, "minutes");
        }

        AddSeparator(layout, "Minute corrections");

        foreach (string prayer in PrayerNames.Alerted)
        {
            var adjustmentBox = CreateNumber(-60, 60);
            _adjustmentBoxes[prayer] = adjustmentBox;
            AddRow(layout, prayer + " Adhan adjustment", adjustmentBox, "minutes");
        }

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildAlertsPage()
    {
        var page = new TabPage("Alerts");
        var layout = CreateTwoColumnLayout();

        _showWidgetBox.Text = "Show the taskbar countdown";
        _startWithWindowsBox.Text = "Start automatically when Windows starts";
        _adhanAlertsBox.Text = "Show quiet Adhan reminder";
        _iqamahAlertsBox.Text = "Show Iqamah reminder";
        _soundBox.Text = "Play short Windows sounds";
        _iqamahActivityBox.Text = "Make Iqamah assertive only when this PC is active";

        AddFullRow(layout, _showWidgetBox);

        _placementBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _placementBox.Items.Add(new ComboItem("Above taskbar", 0));
        _placementBox.Items.Add(new ComboItem("Taskbar band (experimental)", 1));
        _placementBox.DisplayMember = nameof(ComboItem.Text);
        _placementBox.ValueMember = nameof(ComboItem.Value);
        AddRow(layout, "Countdown position", _placementBox);

        _themeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeBox.DisplayMember = nameof(ThemeOption.Text);
        _themeBox.ValueMember = nameof(ThemeOption.Key);
        _themeBox.DataSource = WidgetThemeCatalog.All
            .Select(theme => new ThemeOption(theme.DisplayName, theme.Key))
            .ToList();
        AddRow(layout, "Widget theme", _themeBox);

        var editThemeButton = new Button
        {
            Text = "Open theme editor",
            Height = 30,
            Dock = DockStyle.Left,
            Width = 150
        };
        editThemeButton.Click += (_, _) => OpenThemeEditor();
        AddFullRow(layout, editThemeButton);

        AddFullRow(layout, _startWithWindowsBox);
        AddFullRow(layout, _adhanAlertsBox);
        AddFullRow(layout, _iqamahAlertsBox);
        AddFullRow(layout, _soundBox);
        AddFullRow(layout, _iqamahActivityBox);

        _activityThreshold.Minimum = 1;
        _activityThreshold.Maximum = 120;
        _refreshHours.Minimum = 1;
        _refreshHours.Maximum = 24;
        _popupSeconds.Minimum = 5;
        _popupSeconds.Maximum = 120;

        AddRow(layout, "PC active threshold", _activityThreshold, "minutes");
        AddRow(layout, "Refresh from API every", _refreshHours, "hours");
        AddRow(layout, "Popup auto close after", _popupSeconds, "seconds");

        page.Controls.Add(layout);
        return page;
    }

    private void OpenThemeEditor()
    {
        SaveControlsToSettings();
        using var form = new ThemeEditorForm(Settings);
        form.Saved += (_, _) =>
        {
            Settings = form.Settings;
            LoadSettings();
        };

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            Settings = form.Settings;
            LoadSettings();
        }
    }

    private void LoadSettings()
    {
        _cityBox.Text = Settings.City;
        _countryBox.Text = Settings.Country;
        _stateBox.Text = Settings.State;
        _latitudeBox.Value = (decimal)Math.Clamp(Settings.Latitude, -90, 90);
        _longitudeBox.Value = (decimal)Math.Clamp(Settings.Longitude, -180, 180);
        _locationModeBox.SelectedIndex = Settings.LocationMode switch
        {
            LocationModeOptions.City => 1,
            LocationModeOptions.Coordinates => 2,
            _ => 0
        };
        _methodBox.SelectedValue = Settings.UseAutomaticCalculationMethod ? -1 : Settings.CalculationMethod;
        _schoolBox.SelectedIndex = Settings.School == 1 ? 1 : 0;
        _showWidgetBox.Checked = Settings.ShowDesktopWidget;
        _placementBox.SelectedIndex = string.Equals(
            Settings.WidgetPlacement,
            WidgetPlacementOptions.TaskbarBand,
            StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;
        _themeBox.SelectedValue = Settings.WidgetTheme;
        _startWithWindowsBox.Checked = Settings.StartWithWindows;
        _adhanAlertsBox.Checked = Settings.AdhanAlertsEnabled;
        _iqamahAlertsBox.Checked = Settings.IqamahAlertsEnabled;
        _soundBox.Checked = Settings.SoundEnabled;
        _iqamahActivityBox.Checked = Settings.IqamahRequiresPcActivity;
        _activityThreshold.Value = Settings.ActivityThresholdMinutes;
        _refreshHours.Value = Settings.RefreshEveryHours;
        _popupSeconds.Value = Settings.PopupAutoCloseSeconds;

        foreach (string prayer in PrayerNames.Alerted)
        {
            _iqamahOffsetBoxes[prayer].Value = Settings.GetIqamahOffset(prayer);
            _adjustmentBoxes[prayer].Value = Settings.GetPrayerAdjustment(prayer);
        }
    }

    private void SaveAndClose()
    {
        if (string.IsNullOrWhiteSpace(_cityBox.Text) || string.IsNullOrWhiteSpace(_countryBox.Text))
        {
            MessageBox.Show(this, "City and country are required.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SaveControlsToSettings();

        DialogResult = DialogResult.OK;
        Close();
    }

    private void SaveControlsToSettings()
    {
        Settings.City = _cityBox.Text.Trim();
        Settings.Country = _countryBox.Text.Trim();
        Settings.State = _stateBox.Text.Trim();
        Settings.Latitude = (double)_latitudeBox.Value;
        Settings.Longitude = (double)_longitudeBox.Value;
        Settings.LocationMode = _locationModeBox.SelectedIndex switch
        {
            1 => LocationModeOptions.City,
            2 => LocationModeOptions.Coordinates,
            _ => LocationModeOptions.Auto
        };
        int method = _methodBox.SelectedValue is int selectedMethod ? selectedMethod : -1;
        Settings.UseAutomaticCalculationMethod = method < 0;
        if (method >= 0)
        {
            Settings.CalculationMethod = method;
        }
        Settings.School = _schoolBox.SelectedItem is ComboItem item ? item.Value : 0;
        Settings.ShowDesktopWidget = _showWidgetBox.Checked;
        Settings.WidgetPlacement = _placementBox.SelectedIndex == 1
            ? WidgetPlacementOptions.TaskbarBand
            : WidgetPlacementOptions.AboveTaskbar;
        Settings.WidgetTheme = _themeBox.SelectedValue as string ?? WidgetThemeOptions.GoldDarkBlue;
        Settings.StartWithWindows = _startWithWindowsBox.Checked;
        Settings.AdhanAlertsEnabled = _adhanAlertsBox.Checked;
        Settings.IqamahAlertsEnabled = _iqamahAlertsBox.Checked;
        Settings.SoundEnabled = _soundBox.Checked;
        Settings.IqamahRequiresPcActivity = _iqamahActivityBox.Checked;
        Settings.ActivityThresholdMinutes = (int)_activityThreshold.Value;
        Settings.RefreshEveryHours = (int)_refreshHours.Value;
        Settings.PopupAutoCloseSeconds = (int)_popupSeconds.Value;

        foreach (string prayer in PrayerNames.Alerted)
        {
            Settings.IqamahOffsetsMinutes[prayer] = (int)_iqamahOffsetBoxes[prayer].Value;
            Settings.PrayerAdjustmentsMinutes[prayer] = (int)_adjustmentBoxes[prayer].Value;
        }
    }

    private static TableLayoutPanel CreateTwoColumnLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 3,
            AutoScroll = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74));
        return layout;
    }

    private static void AddRow(TableLayoutPanel layout, string labelText, Control input, string suffix = "")
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(label, 0, row);

        input.Dock = DockStyle.Fill;
        layout.Controls.Add(input, 1, row);

        var suffixLabel = new Label
        {
            Text = suffix,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(90, 90, 90),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(suffixLabel, 2, row);
    }

    private static void AddFullRow(TableLayoutPanel layout, Control box)
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        box.Dock = DockStyle.Fill;
        layout.Controls.Add(box, 0, row);
        layout.SetColumnSpan(box, 3);
    }

    private static void AddSeparator(TableLayoutPanel layout, string text)
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 60, 68),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(label, 0, row);
        layout.SetColumnSpan(label, 3);
    }

    private static NumericUpDown CreateNumber(int min, int max)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Increment = 1,
            Dock = DockStyle.Left,
            Width = 90
        };
    }

    private static void ConfigureCoordinateBox(NumericUpDown box, int min, int max)
    {
        box.Minimum = min;
        box.Maximum = max;
        box.DecimalPlaces = 6;
        box.Increment = 0.0001M;
        box.Dock = DockStyle.Left;
        box.Width = 130;
    }

    private static List<MethodOption> MethodOptions() =>
    [
        new(-1, "Automatic by location"),
        new(16, "Dubai (unofficial)"),
        new(8, "Gulf Region"),
        new(4, "Umm Al-Qura, Makkah"),
        new(3, "Muslim World League"),
        new(2, "ISNA"),
        new(1, "Karachi"),
        new(5, "Egyptian General Authority"),
        new(9, "Kuwait"),
        new(10, "Qatar"),
        new(11, "Singapore"),
        new(12, "France"),
        new(13, "Diyanet, Turkey"),
        new(14, "Russia"),
        new(15, "Moonsighting Committee"),
        new(0, "Shia Ithna-Ashari")
    ];

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
            WidgetLeft = settings.WidgetLeft,
            WidgetTop = settings.WidgetTop,
            WidgetScreenDeviceName = settings.WidgetScreenDeviceName,
            WidgetTheme = settings.WidgetTheme,
            WidgetThemeCustomizations = settings.WidgetThemeCustomizations.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Clone(),
                StringComparer.OrdinalIgnoreCase),
            IqamahOffsetsMinutes = new Dictionary<string, int>(settings.IqamahOffsetsMinutes, StringComparer.OrdinalIgnoreCase),
            PrayerAdjustmentsMinutes = new Dictionary<string, int>(settings.PrayerAdjustmentsMinutes, StringComparer.OrdinalIgnoreCase)
        };
    }

    private sealed record ComboItem(string Text, int Value);

    private sealed record ThemeOption(string Text, string Key);

    private sealed record MethodOption(int Id, string Name)
    {
        public string DisplayName => $"{Id} - {Name}";
    }
}
