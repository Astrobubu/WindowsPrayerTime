using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.UI;

public sealed class AlertForm : Form
{
    private readonly System.Windows.Forms.Timer _autoCloseTimer = new();

    public event EventHandler? SnoozeRequested;
    public event EventHandler? SettingsRequested;

    public AlertForm(PrayerOccurrence occurrence, bool userActive, AppSettings settings)
    {
        bool isIqamah = occurrence.IsIqamah;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = isIqamah ? new Size(360, 172) : new Size(320, 132);
        Text = isIqamah ? "Iqamah reminder" : "Prayer time";
        BackColor = isIqamah ? Color.FromArgb(255, 249, 244) : Color.FromArgb(246, 251, 251);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, isIqamah ? 44 : 32));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", isIqamah ? 14F : 12F, FontStyle.Bold),
            ForeColor = isIqamah ? Color.FromArgb(148, 50, 34) : Color.FromArgb(30, 92, 103),
            Text = isIqamah ? "Iqamah is now" : "Adhan is now",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var message = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", isIqamah ? 11F : 9.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(45, 55, 55),
            Text = BuildMessage(occurrence, userActive),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var time = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(84, 98, 98),
            Text = $"{occurrence.PrayerName} at {occurrence.Time:h:mm tt}",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var dismissButton = new Button
        {
            Text = "Dismiss",
            AutoSize = true,
            Height = 30
        };
        dismissButton.Click += (_, _) => Close();
        buttons.Controls.Add(dismissButton);

        if (isIqamah)
        {
            var snoozeButton = new Button
            {
                Text = "Snooze 3 min",
                AutoSize = true,
                Height = 30
            };
            snoozeButton.Click += (_, _) =>
            {
                SnoozeRequested?.Invoke(this, EventArgs.Empty);
                Close();
            };
            buttons.Controls.Add(snoozeButton);
        }

        var settingsButton = new Button
        {
            Text = "Settings",
            AutoSize = true,
            Height = 30
        };
        settingsButton.Click += (_, _) =>
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
            Close();
        };
        buttons.Controls.Add(settingsButton);

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(message, 0, 1);
        root.Controls.Add(time, 0, 2);
        root.Controls.Add(buttons, 0, 3);
        Controls.Add(root);

        _autoCloseTimer.Interval = Math.Max(5, settings.PopupAutoCloseSeconds) * 1000;
        _autoCloseTimer.Tick += (_, _) =>
        {
            _autoCloseTimer.Stop();
            Close();
        };
    }

    public new void Show()
    {
        PlaceNearTaskbar();
        base.Show();
        Activate();
        _autoCloseTimer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _autoCloseTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private static string BuildMessage(PrayerOccurrence occurrence, bool userActive)
    {
        if (occurrence.IsAdhan)
        {
            return "A quiet reminder for " + occurrence.PrayerName + ".";
        }

        if (userActive)
        {
            return "You are active on this PC. Time to step away for " + occurrence.PrayerName + ".";
        }

        return "Iqamah time for " + occurrence.PrayerName + ".";
    }

    private void PlaceNearTaskbar()
    {
        Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
        Location = new Point(workingArea.Right - Width - 18, workingArea.Bottom - Height - 18);
    }
}
