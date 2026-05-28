using WindowsPrayerTime.Models;
using WindowsPrayerTime.Services;

namespace WindowsPrayerTime.UI;

public sealed class DashboardForm : Form
{
    private readonly Label _summaryLabel = new();
    private readonly Label _statusLabel = new();
    private readonly DataGridView _grid = new();

    public event EventHandler? RefreshRequested;
    public event EventHandler? SettingsRequested;

    public DashboardForm()
    {
        Text = "Windows Prayer Time";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(620, 460);
        Size = new Size(720, 520);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        _summaryLabel.Dock = DockStyle.Fill;
        _summaryLabel.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
        _summaryLabel.ForeColor = Color.FromArgb(25, 60, 68);
        _summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_summaryLabel, 0, 0);

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _grid.EditMode = DataGridViewEditMode.EditProgrammatically;
        _grid.ReadOnly = true;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.Columns.Add("Day", "Day");
        _grid.Columns.Add("Prayer", "Prayer");
        _grid.Columns.Add("Adhan", "Adhan");
        _grid.Columns.Add("Iqamah", "Iqamah");
        _grid.Columns.Add("Countdown", "Countdown");
        root.Controls.Add(_grid, 0, 1);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = Color.FromArgb(84, 98, 98);
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 2);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var closeButton = new Button { Text = "Close", Width = 90, Height = 30 };
        closeButton.Click += (_, _) => Hide();
        actions.Controls.Add(closeButton);

        var settingsButton = new Button { Text = "Settings", Width = 90, Height = 30 };
        settingsButton.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        actions.Controls.Add(settingsButton);

        var refreshButton = new Button { Text = "Refresh", Width = 90, Height = 30 };
        refreshButton.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        actions.Controls.Add(refreshButton);

        root.Controls.Add(actions, 0, 3);
        Controls.Add(root);
    }

    public void UpdateSchedule(PrayerSchedule? schedule, AppSettings settings, DateTime now, string? status = null)
    {
        var widgetState = WidgetStateFactory.FromSchedule(schedule, settings, now);
        _summaryLabel.Text = widgetState.PrimaryLabel + " in " + widgetState.Countdown;
        _statusLabel.Text = status ?? BuildStatus(schedule, settings);

        _grid.Rows.Clear();
        if (schedule?.Today is null)
        {
            return;
        }

        AddDayRows(schedule.Today, settings, now, "Today");

        if (schedule.Tomorrow is not null)
        {
            AddDayRows(schedule.Tomorrow, settings, now, "Tomorrow");
        }
    }

    private void AddDayRows(PrayerDay day, AppSettings settings, DateTime now, string dayLabel)
    {
        foreach (PrayerTime prayerTime in day.Times.Where(time => PrayerNames.Dashboard.Contains(time.Name)))
        {
            string iqamahText = "";
            if (prayerTime.IsAlertedPrayer)
            {
                iqamahText = prayerTime.Time.AddMinutes(settings.GetIqamahOffset(prayerTime.Name)).ToString("h:mm tt");
            }

            string countdown = prayerTime.Time > now
                ? WidgetStateFactory.FormatCountdown(prayerTime.Time - now)
                : "";

            int rowIndex = _grid.Rows.Add(
                dayLabel,
                prayerTime.Name,
                prayerTime.Time.ToString("h:mm tt"),
                iqamahText,
                countdown);

            if (prayerTime.Time <= now && prayerTime.Time.Date == now.Date)
            {
                _grid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(120, 120, 120);
            }
        }
    }

    private static string BuildStatus(PrayerSchedule? schedule, AppSettings settings)
    {
        if (schedule?.Today is null)
        {
            return "Prayer times are loading.";
        }

        string source = "AlAdhan";
        string method = string.IsNullOrWhiteSpace(schedule.Today.MethodName) ? "method " + settings.CalculationMethod : schedule.Today.MethodName;
        string fetched = schedule.Today.FetchedAt.ToString("g");
        string location = string.IsNullOrWhiteSpace(schedule.Today.Location)
            ? settings.LocationLabel
            : schedule.Today.Location;
        return $"{location} - {source}, {method}. Updated {fetched}.";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }
}
