using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.UI;

public sealed class TrayWidgetForm : Form
{
    private readonly Label _primaryLabel = new();
    private readonly Label _countdownLabel = new();
    private readonly Label _timeLabel = new();
    private readonly Label _secondaryLabel = new();
    private readonly TableLayoutPanel _layout = new();
    private bool _isDragging;
    private Point _dragStart;
    private string _placement = WidgetPlacementOptions.AboveTaskbar;

    public event EventHandler? OpenRequested;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExToolWindow = 0x00000080;
            const int wsExAppWindow = 0x00040000;
            const int wsExNoActivate = 0x08000000;

            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= wsExToolWindow | wsExNoActivate;
            parameters.ExStyle &= ~wsExAppWindow;
            return parameters;
        }
    }

    public TrayWidgetForm(ContextMenuStrip contextMenu)
    {
        ContextMenuStrip = contextMenu;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(21, 35, 38);
        ForeColor = Color.White;
        Opacity = 0.96;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        _layout.Dock = DockStyle.Fill;
        _layout.ColumnCount = 2;
        _layout.BackColor = Color.Transparent;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

        _primaryLabel.Dock = DockStyle.Fill;
        _primaryLabel.ForeColor = Color.FromArgb(238, 250, 248);
        _primaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        _primaryLabel.AutoEllipsis = true;

        _timeLabel.Dock = DockStyle.Fill;
        _timeLabel.ForeColor = Color.FromArgb(198, 221, 218);
        _timeLabel.TextAlign = ContentAlignment.MiddleRight;

        _countdownLabel.Dock = DockStyle.Fill;
        _countdownLabel.ForeColor = Color.White;
        _countdownLabel.TextAlign = ContentAlignment.MiddleLeft;

        _secondaryLabel.Dock = DockStyle.Fill;
        _secondaryLabel.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
        _secondaryLabel.ForeColor = Color.FromArgb(178, 202, 199);
        _secondaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        _secondaryLabel.AutoEllipsis = true;
        _secondaryLabel.Padding = new Padding(0, 2, 0, 0);

        Controls.Add(_layout);
        ApplyPlacement(WidgetPlacementOptions.AboveTaskbar);

        foreach (Control control in new Control[] { this, _layout, _primaryLabel, _timeLabel, _countdownLabel, _secondaryLabel })
        {
            control.MouseDoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
            control.MouseDown += BeginDrag;
            control.MouseMove += DragWidget;
            control.MouseUp += EndDrag;
        }
    }

    public void ApplyPlacement(string placement)
    {
        _placement = string.Equals(placement, WidgetPlacementOptions.TaskbarBand, StringComparison.OrdinalIgnoreCase)
            ? WidgetPlacementOptions.TaskbarBand
            : WidgetPlacementOptions.AboveTaskbar;

        _layout.SuspendLayout();
        _layout.Controls.Clear();
        _layout.RowStyles.Clear();
        MinimumSize = Size.Empty;
        MaximumSize = Size.Empty;

        if (_placement == WidgetPlacementOptions.TaskbarBand)
        {
            Size = new Size(174, 40);
            MinimumSize = Size;
            MaximumSize = Size;
            Padding = new Padding(8, 3, 8, 3);
            _layout.RowCount = 2;
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 15));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 19));
            _primaryLabel.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
            _timeLabel.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
            _countdownLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point);

            _layout.Controls.Add(_primaryLabel, 0, 0);
            _layout.Controls.Add(_timeLabel, 1, 0);
            _layout.Controls.Add(_countdownLabel, 0, 1);
            _layout.SetColumnSpan(_countdownLabel, 2);
        }
        else
        {
            Size = new Size(232, 96);
            MinimumSize = Size;
            MaximumSize = Size;
            Padding = new Padding(12, 8, 12, 8);
            _layout.RowCount = 3;
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            _primaryLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            _timeLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            _countdownLabel.Font = new Font("Segoe UI", 21F, FontStyle.Bold, GraphicsUnit.Point);

            _layout.Controls.Add(_primaryLabel, 0, 0);
            _layout.Controls.Add(_timeLabel, 1, 0);
            _layout.Controls.Add(_countdownLabel, 0, 1);
            _layout.SetColumnSpan(_countdownLabel, 2);
            _layout.Controls.Add(_secondaryLabel, 0, 2);
            _layout.SetColumnSpan(_secondaryLabel, 2);
        }

        _layout.ResumeLayout();
        PlaceNearTaskbar();
    }

    public void UpdateState(WidgetState state)
    {
        _primaryLabel.Text = _placement == WidgetPlacementOptions.TaskbarBand
            ? CompactPrimaryLabel(state.PrimaryLabel)
            : state.PrimaryLabel;
        _countdownLabel.Text = state.Countdown;
        _timeLabel.Text = _placement == WidgetPlacementOptions.TaskbarBand
            ? CompactTimeLabel(state.TimeLabel)
            : state.TimeLabel;
        _secondaryLabel.Text = state.SecondaryLabel;

        BackColor = state.IsIqamahCountdown ? Color.FromArgb(103, 42, 35) : Color.FromArgb(21, 35, 38);
        Invalidate();
    }

    public void PlaceNearTaskbar()
    {
        if (_placement == WidgetPlacementOptions.AboveTaskbar)
        {
            Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
            Location = new Point(workingArea.Right - Width - 12, workingArea.Bottom - Height - 12);
            PinTopmost();
            return;
        }

        Rectangle taskbar = TryGetTaskbarBounds(out Rectangle detected)
            ? detected
            : EstimateTaskbarBounds();

        Rectangle tray = TryGetTrayBounds(out Rectangle detectedTray)
            ? detectedTray
            : Rectangle.Empty;

        if (taskbar.Width >= taskbar.Height)
        {
            int rightEdge = tray.IsEmpty ? taskbar.Right - 210 : tray.Left - 8;
            int x = Math.Clamp(rightEdge - Width, taskbar.Left + 8, taskbar.Right - Width - 8);
            int y = taskbar.Top + Math.Max(0, (taskbar.Height - Height) / 2);
            Location = new Point(x, y);
            PinTopmost();
            return;
        }

        int verticalX = taskbar.Left + Math.Max(0, (taskbar.Width - Width) / 2);
        int verticalY = Math.Max(taskbar.Top + 8, taskbar.Bottom - Height - 96);
        Location = new Point(verticalX, verticalY);
        PinTopmost();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        PlaceNearTaskbar();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var borderPen = new Pen(Color.FromArgb(76, 102, 104), 1);
        using GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 8);
        e.Graphics.DrawPath(borderPen, path);
    }

    private void BeginDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || e.Clicks > 1)
        {
            return;
        }

        _isDragging = true;
        _dragStart = e.Location;
    }

    private void DragWidget(object? sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        Point screenPoint = PointToScreen(e.Location);
        Location = new Point(screenPoint.X - _dragStart.X, screenPoint.Y - _dragStart.Y);
    }

    private void EndDrag(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static string CompactPrimaryLabel(string label)
    {
        return label
            .Replace("Next ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Iqamah ", "Iqamah ", StringComparison.OrdinalIgnoreCase);
    }

    private static string CompactTimeLabel(string label)
    {
        return label
            .Replace(" AM", "a", StringComparison.OrdinalIgnoreCase)
            .Replace(" PM", "p", StringComparison.OrdinalIgnoreCase);
    }

    private void PinTopmost()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        const uint swpNoSize = 0x0001;
        const uint swpNoMove = 0x0002;
        const uint swpNoActivate = 0x0010;
        const uint swpShowWindow = 0x0040;
        SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, swpNoMove | swpNoSize | swpNoActivate | swpShowWindow);
    }

    private static Rectangle EstimateTaskbarBounds()
    {
        Screen screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        Rectangle bounds = screen.Bounds;
        Rectangle workingArea = screen.WorkingArea;

        if (workingArea.Bottom < bounds.Bottom)
        {
            return new Rectangle(bounds.Left, workingArea.Bottom, bounds.Width, bounds.Bottom - workingArea.Bottom);
        }

        if (workingArea.Top > bounds.Top)
        {
            return new Rectangle(bounds.Left, bounds.Top, bounds.Width, workingArea.Top - bounds.Top);
        }

        if (workingArea.Right < bounds.Right)
        {
            return new Rectangle(workingArea.Right, bounds.Top, bounds.Right - workingArea.Right, bounds.Height);
        }

        if (workingArea.Left > bounds.Left)
        {
            return new Rectangle(bounds.Left, bounds.Top, workingArea.Left - bounds.Left, bounds.Height);
        }

        return new Rectangle(bounds.Right - 420, bounds.Bottom - 48, 420, 48);
    }

    private static bool TryGetTaskbarBounds(out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
        return taskbarHandle != IntPtr.Zero && TryGetWindowRectangle(taskbarHandle, out bounds);
    }

    private static bool TryGetTrayBounds(out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
        if (taskbarHandle == IntPtr.Zero)
        {
            return false;
        }

        IntPtr trayHandle = FindChildWindow(taskbarHandle, "TrayNotifyWnd");
        return trayHandle != IntPtr.Zero && TryGetWindowRectangle(trayHandle, out bounds);
    }

    private static IntPtr FindChildWindow(IntPtr parent, string className)
    {
        IntPtr child = IntPtr.Zero;
        while (true)
        {
            child = FindWindowEx(parent, child, null, null);
            if (child == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            string childClassName = GetClassName(child);
            if (string.Equals(childClassName, className, StringComparison.Ordinal))
            {
                return child;
            }

            IntPtr nested = FindChildWindow(child, className);
            if (nested != IntPtr.Zero)
            {
                return nested;
            }
        }
    }

    private static bool TryGetWindowRectangle(IntPtr handle, out Rectangle rectangle)
    {
        rectangle = Rectangle.Empty;
        if (!GetWindowRect(handle, out Rect rect))
        {
            return false;
        }

        rectangle = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return true;
    }

    private static string GetClassName(IntPtr handle)
    {
        var buffer = new StringBuilder(256);
        int length = GetClassName(handle, buffer, buffer.Capacity);
        return length <= 0 ? "" : buffer.ToString();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string className, string? windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr windowHandle, out Rect rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr windowHandle, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetClassName(IntPtr windowHandle, StringBuilder className, int maxCount);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
