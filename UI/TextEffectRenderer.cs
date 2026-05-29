using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

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
        if (alpha <= 0 || string.IsNullOrWhiteSpace(text) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        if (blur <= 0)
        {
            using var directBrush = new SolidBrush(Color.FromArgb(alpha, color));
            Rectangle directBounds = bounds;
            directBounds.Offset(offsetX, offsetY);
            graphics.DrawString(text, font, directBrush, directBounds, format);
            return;
        }

        int padding = Math.Clamp((blur * 3) + 4, 6, 104);
        int width = bounds.Width + (padding * 2);
        int height = bounds.Height + (padding * 2);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        using Bitmap textMask = CreateTextMask(text, font, format, bounds.Size, padding, width, height);
        byte[] blurredAlpha = BuildBlurredAlpha(textMask, blur);
        using Bitmap effect = BuildTintedBitmap(blurredAlpha, width, height, color, alpha);

        GraphicsState state = graphics.Save();
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        var destination = new Rectangle(
            bounds.X + offsetX - padding,
            bounds.Y + offsetY - padding,
            width,
            height);
        graphics.DrawImage(effect, destination);
        graphics.Restore(state);
    }

    private static Bitmap CreateTextMask(
        string text,
        Font font,
        StringFormat format,
        Size textArea,
        int padding,
        int width,
        int height)
    {
        var mask = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using Graphics maskGraphics = Graphics.FromImage(mask);
        maskGraphics.Clear(Color.Transparent);
        maskGraphics.SmoothingMode = SmoothingMode.AntiAlias;
        maskGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        using var maskBrush = new SolidBrush(Color.White);
        var textBounds = new Rectangle(padding, padding, textArea.Width, textArea.Height);
        maskGraphics.DrawString(text, font, maskBrush, textBounds, format);
        return mask;
    }

    private static byte[] BuildBlurredAlpha(Bitmap mask, int blur)
    {
        Rectangle rect = new(0, 0, mask.Width, mask.Height);
        BitmapData data = mask.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int length = Math.Abs(data.Stride) * data.Height;
            byte[] pixels = new byte[length];
            Marshal.Copy(data.Scan0, pixels, 0, length);

            byte[] alpha = new byte[mask.Width * mask.Height];
            for (int y = 0; y < mask.Height; y++)
            {
                int row = y * data.Stride;
                for (int x = 0; x < mask.Width; x++)
                {
                    alpha[(y * mask.Width) + x] = pixels[row + (x * 4) + 3];
                }
            }

            byte[] blurred = alpha;
            int boxRadius = Math.Max(1, blur);
            for (int pass = 0; pass < 3; pass++)
            {
                blurred = BoxBlur(blurred, mask.Width, mask.Height, boxRadius);
            }

            return blurred;
        }
        finally
        {
            mask.UnlockBits(data);
        }
    }

    private static Bitmap BuildTintedBitmap(byte[] alphaMask, int width, int height, Color color, int alpha)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        Rectangle rect = new(0, 0, width, height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        try
        {
            int length = Math.Abs(data.Stride) * data.Height;
            byte[] pixels = new byte[length];
            for (int y = 0; y < height; y++)
            {
                int row = y * data.Stride;
                for (int x = 0; x < width; x++)
                {
                    int effectAlpha = (alphaMask[(y * width) + x] * alpha) / 255;
                    int index = row + (x * 4);
                    pixels[index] = (byte)((color.B * effectAlpha) / 255);
                    pixels[index + 1] = (byte)((color.G * effectAlpha) / 255);
                    pixels[index + 2] = (byte)((color.R * effectAlpha) / 255);
                    pixels[index + 3] = (byte)effectAlpha;
                }
            }

            Marshal.Copy(pixels, 0, data.Scan0, length);
            return bitmap;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static byte[] BoxBlur(byte[] source, int width, int height, int radius)
    {
        byte[] horizontal = new byte[source.Length];
        byte[] output = new byte[source.Length];
        int window = (radius * 2) + 1;

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            int sum = 0;
            for (int x = -radius; x <= radius; x++)
            {
                sum += source[row + Math.Clamp(x, 0, width - 1)];
            }

            for (int x = 0; x < width; x++)
            {
                horizontal[row + x] = (byte)(sum / window);
                int removeX = Math.Clamp(x - radius, 0, width - 1);
                int addX = Math.Clamp(x + radius + 1, 0, width - 1);
                sum += source[row + addX] - source[row + removeX];
            }
        }

        for (int x = 0; x < width; x++)
        {
            int sum = 0;
            for (int y = -radius; y <= radius; y++)
            {
                sum += horizontal[(Math.Clamp(y, 0, height - 1) * width) + x];
            }

            for (int y = 0; y < height; y++)
            {
                output[(y * width) + x] = (byte)(sum / window);
                int removeY = Math.Clamp(y - radius, 0, height - 1);
                int addY = Math.Clamp(y + radius + 1, 0, height - 1);
                sum += horizontal[(addY * width) + x] - horizontal[(removeY * width) + x];
            }
        }

        return output;
    }
}
