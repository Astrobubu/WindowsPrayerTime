using System.Diagnostics;
using WindowsPrayerTime.Models;
using WindowsPrayerTime.Services;
using WindowsPrayerTime.UI;

namespace WindowsPrayerTime;

public sealed class PrayerTimeApplicationContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore = new();
    private readonly LocationDetectionService _locationDetectionService = new();
    private readonly SoundService _soundService = new();
    private readonly NotifyIcon _notifyIcon = new();
    private readonly ContextMenuStrip _contextMenu = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly HashSet<string> _triggeredAlerts = [];
    private readonly List<PrayerOccurrence> _snoozedOccurrences = [];
    private readonly ToolStripMenuItem _showWidgetMenuItem;
    private readonly ToolStripMenuItem _aboveTaskbarMenuItem;
    private readonly ToolStripMenuItem _taskbarBandMenuItem;
    private readonly Dictionary<string, ToolStripMenuItem> _themeMenuItems = new(StringComparer.OrdinalIgnoreCase);
    private readonly ToolStripMenuItem _refreshMenuItem;
    private readonly AladhanPrayerTimesService _prayerTimesService;
    private AppSettings _settings;
    private PrayerSchedule? _schedule;
    private TrayWidgetForm? _widget;
    private DashboardForm? _dashboard;
    private DateOnly _lastTriggerDate = DateOnly.FromDateTime(DateTime.Now);
    private DateTime _lastRefresh = DateTime.MinValue;
    private DateTime _lastIconUpdate = DateTime.MinValue;
    private bool _isRefreshing;
    private string _status = "Starting.";

    public PrayerTimeApplicationContext()
    {
        _settings = _settingsStore.Load();
        _settings.StartWithWindows = StartupService.IsEnabled();
        _prayerTimesService = new AladhanPrayerTimesService(_settingsStore.AppDirectory);

        _showWidgetMenuItem = new ToolStripMenuItem("Show countdown widget")
        {
            Checked = _settings.ShowDesktopWidget,
            CheckOnClick = true
        };
        _showWidgetMenuItem.Click += (_, _) => ToggleWidget(_showWidgetMenuItem.Checked);

        _aboveTaskbarMenuItem = new ToolStripMenuItem("Above taskbar")
        {
            CheckOnClick = true
        };
        _aboveTaskbarMenuItem.Click += (_, _) => SetWidgetPlacement(WidgetPlacementOptions.AboveTaskbar);

        _taskbarBandMenuItem = new ToolStripMenuItem("Taskbar band (experimental)")
        {
            CheckOnClick = true
        };
        _taskbarBandMenuItem.Click += (_, _) => SetWidgetPlacement(WidgetPlacementOptions.TaskbarBand);
        UpdatePlacementMenu();

        foreach (WidgetThemeDefinition theme in WidgetThemeCatalog.All)
        {
            var themeItem = new ToolStripMenuItem(theme.DisplayName)
            {
                CheckOnClick = true
            };
            string themeKey = theme.Key;
            themeItem.Click += (_, _) => SetWidgetTheme(themeKey);
            _themeMenuItems[theme.Key] = themeItem;
        }
        UpdateThemeMenu();

        _refreshMenuItem = new ToolStripMenuItem("Refresh prayer times");
        _refreshMenuItem.Click += async (_, _) => await RefreshScheduleAsync(showErrors: true);

        ConfigureContextMenu();
        ConfigureNotifyIcon();
        ConfigureDashboard();

        if (_settings.ShowDesktopWidget)
        {
            ShowWidget();
        }

        _timer.Interval = 1000;
        _timer.Tick += async (_, _) => await TickAsync();
        _timer.Start();

        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        _ = RefreshScheduleAsync(showErrors: true);
    }

    private void ConfigureContextMenu()
    {
        _contextMenu.Items.Add("Open schedule", null, (_, _) => OpenDashboard());
        _contextMenu.Items.Add(_showWidgetMenuItem);
        var placementMenu = new ToolStripMenuItem("Countdown position");
        placementMenu.DropDownItems.Add(_aboveTaskbarMenuItem);
        placementMenu.DropDownItems.Add(_taskbarBandMenuItem);
        _contextMenu.Items.Add(placementMenu);
        var themeMenu = new ToolStripMenuItem("Widget theme");
        foreach (ToolStripMenuItem themeItem in _themeMenuItems.Values)
        {
            themeMenu.DropDownItems.Add(themeItem);
        }
        _contextMenu.Items.Add(themeMenu);
        _contextMenu.Items.Add("Edit widget theme", null, (_, _) => OpenThemeEditor());
        _contextMenu.Items.Add(_refreshMenuItem);
        _contextMenu.Items.Add("Settings", null, (_, _) => OpenSettings());
        _contextMenu.Items.Add("Open settings folder", null, (_, _) => OpenSettingsFolder());
        _contextMenu.Items.Add("About", null, (_, _) => OpenAbout());
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("Exit", null, (_, _) => ExitThread());
    }

    private void ConfigureNotifyIcon()
    {
        var initialState = new WidgetState("Loading", "--:--", "Starting", _settings.LocationLabel, false, null);
        _notifyIcon.Icon = IconFactory.CreateTrayIcon(initialState);
        _notifyIcon.Text = LimitTooltip("Windows Prayer Time - loading");
        _notifyIcon.Visible = true;
        _notifyIcon.ContextMenuStrip = _contextMenu;
        _notifyIcon.DoubleClick += (_, _) => OpenDashboard();
        _notifyIcon.BalloonTipClicked += (_, _) => OpenDashboard();
    }

    private void ConfigureDashboard()
    {
        _dashboard = new DashboardForm();
        _dashboard.RefreshRequested += async (_, _) => await RefreshScheduleAsync(showErrors: true, force: true);
        _dashboard.SettingsRequested += (_, _) => OpenSettings();
    }

    private async Task TickAsync()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        if (today != _lastTriggerDate)
        {
            _triggeredAlerts.Clear();
            _lastTriggerDate = today;
        }

        if (_schedule is null || DateTime.Now - _lastRefresh > TimeSpan.FromHours(_settings.RefreshEveryHours))
        {
            await RefreshScheduleAsync(showErrors: false);
        }

        TriggerDueAlerts();
        UpdateUi();
    }

    private async Task RefreshScheduleAsync(bool showErrors, bool force = false)
    {
        if (_isRefreshing)
        {
            return;
        }

        if (!force && _schedule is not null && DateTime.Now - _lastRefresh < TimeSpan.FromMinutes(5))
        {
            return;
        }

        _isRefreshing = true;
        _refreshMenuItem.Enabled = false;
        _status = "Refreshing prayer times.";
        UpdateUi();

        try
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);
            DateOnly tomorrow = today.AddDays(1);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(18));
            PrayerLocation location = await _locationDetectionService.ResolveAsync(_settings, cts.Token);
            if (string.Equals(_settings.LocationMode, LocationModeOptions.Auto, StringComparison.OrdinalIgnoreCase))
            {
                _settingsStore.Save(_settings);
            }

            Task<PrayerDay> todayTask = _prayerTimesService.GetPrayerDayAsync(today, _settings, location, cts.Token);
            Task<PrayerDay> tomorrowTask = _prayerTimesService.GetPrayerDayAsync(tomorrow, _settings, location, cts.Token);
            await Task.WhenAll(todayTask, tomorrowTask);

            _schedule = AladhanPrayerTimesService.BuildSchedule(todayTask.Result, tomorrowTask.Result, _settings);
            _lastRefresh = DateTime.Now;
            _status = "Updated " + _lastRefresh.ToString("g") + ".";
        }
        catch (Exception ex)
        {
            _status = "Refresh failed: " + ex.Message;
            if (showErrors)
            {
                _notifyIcon.ShowBalloonTip(
                    5000,
                    "Prayer time refresh failed",
                    ex.Message,
                    ToolTipIcon.Warning);
            }
        }
        finally
        {
            _isRefreshing = false;
            _refreshMenuItem.Enabled = true;
            UpdateUi();
        }
    }

    private void TriggerDueAlerts()
    {
        if (_schedule is null)
        {
            return;
        }

        DateTime now = DateTime.Now;
        var due = _schedule.Occurrences
            .Concat(_snoozedOccurrences)
            .Where(occurrence => now >= occurrence.Time && now - occurrence.Time <= TimeSpan.FromSeconds(70))
            .OrderBy(occurrence => occurrence.Time)
            .ToList();

        foreach (PrayerOccurrence occurrence in due)
        {
            if (!_triggeredAlerts.Add(occurrence.Id))
            {
                continue;
            }

            if (occurrence.IsAdhan)
            {
                ShowAdhanReminder(occurrence);
                continue;
            }

            ShowIqamahReminder(occurrence);
        }

        _snoozedOccurrences.RemoveAll(occurrence => now - occurrence.Time > TimeSpan.FromMinutes(2));
    }

    private void ShowAdhanReminder(PrayerOccurrence occurrence)
    {
        if (!_settings.AdhanAlertsEnabled)
        {
            return;
        }

        _soundService.PlayAdhanCue(_settings);
        string iqamahLine = $"Iqamah in {_settings.GetIqamahOffset(occurrence.PrayerName)} minutes.";
        _notifyIcon.ShowBalloonTip(
            7000,
            "Adhan is now",
            $"{occurrence.PrayerName} at {occurrence.Time:h:mm tt}. {iqamahLine}",
            ToolTipIcon.Info);
    }

    private void ShowIqamahReminder(PrayerOccurrence occurrence)
    {
        if (!_settings.IqamahAlertsEnabled)
        {
            return;
        }

        TimeSpan threshold = TimeSpan.FromMinutes(_settings.ActivityThresholdMinutes);
        bool userActive = UserActivityService.IsUserActive(threshold);
        _soundService.PlayIqamahCue(_settings, userActive);

        if (_settings.IqamahRequiresPcActivity && !userActive)
        {
            _notifyIcon.ShowBalloonTip(
                7000,
                "Iqamah is now",
                $"{occurrence.PrayerName} iqamah at {occurrence.Time:h:mm tt}.",
                ToolTipIcon.Info);
            return;
        }

        var alert = new AlertForm(occurrence, userActive, _settings);
        alert.SnoozeRequested += (_, _) => SnoozeIqamah(occurrence);
        alert.SettingsRequested += (_, _) => OpenSettings();
        alert.FormClosed += (_, _) => alert.Dispose();
        alert.Show();
    }

    private void SnoozeIqamah(PrayerOccurrence occurrence)
    {
        var snoozed = new PrayerOccurrence(
            occurrence.PrayerName,
            "Iqamah",
            DateTime.Now.AddMinutes(3),
            occurrence.IqamahOffsetMinutes);
        _snoozedOccurrences.Add(snoozed);
        _notifyIcon.ShowBalloonTip(
            3000,
            "Snoozed",
            $"{occurrence.PrayerName} iqamah reminder will return in 3 minutes.",
            ToolTipIcon.Info);
    }

    private void UpdateUi()
    {
        DateTime now = DateTime.Now;
        WidgetState state = WidgetStateFactory.FromSchedule(_schedule, _settings, now);

        if (_widget is not null && !_widget.IsDisposed)
        {
            _widget.UpdateState(state);
        }

        _dashboard?.UpdateSchedule(_schedule, _settings, now, _status);

        string tooltip = $"Windows Prayer Time - {state.PrimaryLabel} {state.Countdown} ({state.TimeLabel})";
        _notifyIcon.Text = LimitTooltip(tooltip);

        if (now - _lastIconUpdate >= TimeSpan.FromSeconds(30))
        {
            _lastIconUpdate = now;
            Icon? oldIcon = _notifyIcon.Icon;
            _notifyIcon.Icon = IconFactory.CreateTrayIcon(state);
            oldIcon?.Dispose();
        }
    }

    private void OpenDashboard()
    {
        if (_dashboard is null || _dashboard.IsDisposed)
        {
            ConfigureDashboard();
        }

        _dashboard!.UpdateSchedule(_schedule, _settings, DateTime.Now, _status);
        _dashboard.Show();
        _dashboard.WindowState = FormWindowState.Normal;
        _dashboard.Activate();
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog(_dashboard) != DialogResult.OK)
        {
            return;
        }

        _settings = form.Settings;
        _settingsStore.Save(_settings);
        StartupService.SetEnabled(_settings.StartWithWindows);
        _showWidgetMenuItem.Checked = _settings.ShowDesktopWidget;
        UpdatePlacementMenu();
        UpdateThemeMenu();
        ToggleWidget(_settings.ShowDesktopWidget, save: false);
        _ = RefreshScheduleAsync(showErrors: true, force: true);
    }

    private void OpenAbout()
    {
        using var form = new AboutForm();
        form.ShowDialog(_dashboard);
    }

    private void OpenThemeEditor()
    {
        using var form = new ThemeEditorForm(_settings);
        form.Saved += (_, _) => ApplyThemeEditorSettings(form.Settings);

        if (form.ShowDialog(_dashboard) == DialogResult.OK)
        {
            ApplyThemeEditorSettings(form.Settings);
        }
    }

    private void ApplyThemeEditorSettings(AppSettings settings)
    {
        _settings = settings;
        _settings.EnsureDefaults();
        _settings.ShowDesktopWidget = true;
        _settingsStore.Save(_settings);
        _showWidgetMenuItem.Checked = true;
        UpdatePlacementMenu();
        UpdateThemeMenu();
        ShowWidget();
    }

    private void SetWidgetPlacement(string placement)
    {
        _settings.WidgetPlacement = string.Equals(placement, WidgetPlacementOptions.TaskbarBand, StringComparison.OrdinalIgnoreCase)
            ? WidgetPlacementOptions.TaskbarBand
            : WidgetPlacementOptions.AboveTaskbar;
        _settings.ShowDesktopWidget = true;
        _settingsStore.Save(_settings);
        _showWidgetMenuItem.Checked = true;
        UpdatePlacementMenu();
        ShowWidget();
    }

    private void SetWidgetTheme(string theme)
    {
        _settings.WidgetTheme = WidgetThemeOptions.All.Contains(theme, StringComparer.OrdinalIgnoreCase)
            ? theme
            : WidgetThemeOptions.GoldDarkBlue;
        _settings.ShowDesktopWidget = true;
        _settingsStore.Save(_settings);
        _showWidgetMenuItem.Checked = true;
        UpdateThemeMenu();
        ShowWidget();
    }

    private void UpdatePlacementMenu()
    {
        bool taskbarBand = string.Equals(_settings.WidgetPlacement, WidgetPlacementOptions.TaskbarBand, StringComparison.OrdinalIgnoreCase);
        _aboveTaskbarMenuItem.Checked = !taskbarBand;
        _taskbarBandMenuItem.Checked = taskbarBand;
    }

    private void UpdateThemeMenu()
    {
        foreach ((string themeKey, ToolStripMenuItem item) in _themeMenuItems)
        {
            item.Checked = string.Equals(_settings.WidgetTheme, themeKey, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void ToggleWidget(bool show, bool save = true)
    {
        _settings.ShowDesktopWidget = show;
        if (show)
        {
            ShowWidget();
        }
        else
        {
            HideWidget();
        }

        if (save)
        {
            _settingsStore.Save(_settings);
        }
    }

    private void ShowWidget()
    {
        if (_widget is null || _widget.IsDisposed)
        {
            _widget = new TrayWidgetForm(_contextMenu);
            _widget.OpenRequested += (_, _) => OpenDashboard();
        }

        _widget.ApplyTheme(
            _settings.WidgetTheme,
            _settings.WidgetThemeCustomizations.TryGetValue(_settings.WidgetTheme, out WidgetThemeCustomization? customization)
                ? customization
                : null);
        _widget.ApplyPlacement(_settings.WidgetPlacement);
        _widget.UpdateState(WidgetStateFactory.FromSchedule(_schedule, _settings, DateTime.Now));
        _widget.Show();
        _widget.PlaceNearTaskbar();
    }

    private void HideWidget()
    {
        if (_widget is null || _widget.IsDisposed)
        {
            return;
        }

        _widget.Hide();
    }

    private void OpenSettingsFolder()
    {
        Directory.CreateDirectory(_settingsStore.AppDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = _settingsStore.AppDirectory,
            UseShellExecute = true
        });
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        _widget?.PlaceNearTaskbar();
    }

    protected override void ExitThreadCore()
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _timer.Stop();
        _timer.Dispose();
        _widget?.Dispose();
        _dashboard?.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        base.ExitThreadCore();
    }

    private static string LimitTooltip(string text)
    {
        const int maxLength = 120;
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..(maxLength - 3)] + "...";
    }
}
