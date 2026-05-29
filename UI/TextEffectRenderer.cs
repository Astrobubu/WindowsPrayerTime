using System.Drawing.Drawing2D;

namespace WindowsPrayerTime.UI;

internal static class TextEffectRenderer
{
    public static void DrawSoftText(
        Graphics graphics,
        string text,
        Font font,
        StringFormat format,
        Rectangle bounds,
        Color color,
        int alpha,
        int blur,
        int offsetX,
        int offsetY)
    {
        alpha = Math.Clamp(alpha, 0, 255);
        blur = Math.Clamp(blur, 0, 32);
        if (alpha <= 0 || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Rectangle centerBounds = bounds;
        centerBounds.Offset(offsetX, offsetY);
        if (blur <= 0)
        {
            using var directBrush = new SolidBrush(Color.FromArgb(alpha, color));
            graphics.DrawString(text, font, directBrush, centerBounds, format);
            return;
        }

        GraphicsState state = graphics.Save();
        graphics.CompositingQuality = CompositingQuality.HighQuality;

        var usedOffsets = new HashSet<Point>();
        for (int radius = blur; radius >= 1; radius--)
        {
            int points = Math.Clamp(radius * 10, 12, 72);
            int ringAlpha = Math.Clamp((int)(alpha * (0.06F + ((blur - radius + 1) / (float)(blur + 1) * 0.16F))), 1, 255);
            using var ringBrush = new SolidBrush(Color.FromArgb(ringAlpha, color));

            for (int index = 0; index < points; index++)
            {
                double angle = Math.Tau * index / points;
                var offset = new Point(
                    offsetX + (int)Math.Round(Math.Cos(angle) * radius),
                    offsetY + (int)Math.Round(Math.Sin(angle) * radius));

                if (!usedOffsets.Add(offset))
                {
                    continue;
                }

                Rectangle softBounds = bounds;
                softBounds.Offset(offset);
                graphics.DrawString(text, font, ringBrush, softBounds, format);
            }
        }

        using var coreBrush = new SolidBrush(Color.FromArgb(Math.Clamp(alpha / 3, 1, 255), color));
        graphics.DrawString(text, font, coreBrush, centerBounds, format);
        graphics.Restore(state);
    }
}
