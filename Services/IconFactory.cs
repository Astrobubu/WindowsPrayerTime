using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.Services;

public static class IconFactory
{
    public static Icon CreateTrayIcon(WidgetState state)
    {
        using var bitmap = new Bitmap(64, 64);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        Color background = state.IsIqamahCountdown ? Color.FromArgb(196, 65, 45) : Color.FromArgb(34, 91, 105);
        using var backgroundBrush = new SolidBrush(background);
        using var textBrush = new SolidBrush(Color.White);
        using var mutedBrush = new SolidBrush(Color.FromArgb(226, 245, 242));
        using var borderPen = new Pen(Color.FromArgb(248, 251, 250), 3);
        graphics.FillRoundedRectangle(backgroundBrush, new Rectangle(2, 2, 60, 60), 12);
        graphics.DrawRoundedRectangle(borderPen, new Rectangle(3, 3, 58, 58), 11);

        string shortName = state.PrimaryLabel.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "PT";
        shortName = PrayerNames.ShortName(shortName);
        using var nameFont = new Font("Segoe UI", 14, FontStyle.Bold, GraphicsUnit.Pixel);
        using var timeFont = new Font("Segoe UI", 11, FontStyle.Regular, GraphicsUnit.Pixel);
        var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        graphics.DrawString(shortName, nameFont, textBrush, new RectangleF(0, 9, 64, 20), center);
        graphics.DrawString(CompactCountdown(state.Countdown), timeFont, mutedBrush, new RectangleF(0, 34, 64, 18), center);

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static string CompactCountdown(string countdown)
    {
        string[] parts = countdown.Split(':');
        if (parts.Length == 3)
        {
            return parts[0] + "h";
        }

        if (parts.Length == 2 && int.TryParse(parts[0], out int minutes))
        {
            return minutes + "m";
        }

        return countdown;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using GraphicsPath path = CreateRoundedRectangle(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using GraphicsPath path = CreateRoundedRectangle(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
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
}
