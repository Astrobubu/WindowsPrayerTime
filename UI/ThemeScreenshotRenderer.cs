using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.UI;

internal static class ThemeScreenshotRenderer
{
    private static readonly Dictionary<string, string> OutputNames = new(StringComparer.OrdinalIgnoreCase)
    {
        [WidgetThemeOptions.GoldDarkBlue] = "theme-gold-dark-blue.png",
        [WidgetThemeOptions.Glassy] = "theme-glassy-cyan.png",
        [WidgetThemeOptions.DarkPurple] = "theme-dark-purple.png"
    };

    private static readonly WidgetState SampleState = new(
        "Next Isha",
        "24:18",
        "8:31 PM",
        "Dubai, United Arab Emirates",
        false,
        DateTime.Now.AddMinutes(24));

    public static void Render(string? outputDirectory)
    {
        string root = Directory.GetCurrentDirectory();
        string assetRoot = Directory.Exists(Path.Combine(root, "Assets"))
            ? root
            : AppContext.BaseDirectory;
        string screenshotsDirectory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.Combine(root, "screenshots")
            : Path.GetFullPath(outputDirectory);

        Directory.CreateDirectory(screenshotsDirectory);

        foreach (WidgetThemeDefinition theme in WidgetThemeCatalog.All.Where(theme => !theme.IsSimple))
        {
            WidgetThemeCustomization customization =
                AppSettings.CreateDefaultWidgetThemeCustomization(theme.Key) ?? new WidgetThemeCustomization();
            RenderTheme(theme, customization, assetRoot, screenshotsDirectory);
        }
    }

    private static void RenderTheme(
        WidgetThemeDefinition theme,
        WidgetThemeCustomization customization,
        string assetRoot,
        string screenshotsDirectory)
    {
        using var logical = new Bitmap(theme.Size.Width, theme.Size.Height, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(logical))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            string imagePath = Path.Combine(assetRoot, theme.AssetPath);
            using Image background = Image.FromFile(imagePath);
            Rectangle source = new(
                theme.SourceInset,
                theme.SourceInset,
                background.Width - (theme.SourceInset * 2),
                background.Height - (theme.SourceInset * 2));
            graphics.DrawImage(background, new Rectangle(Point.Empty, theme.Size), source, GraphicsUnit.Pixel);

            DrawElement(graphics, theme, customization, WidgetTextElementKeys.Primary, SampleState.PrimaryLabel);
            DrawElement(graphics, theme, customization, WidgetTextElementKeys.Time, SampleState.TimeLabel);
            DrawElement(graphics, theme, customization, WidgetTextElementKeys.Countdown, SampleState.Countdown);
            DrawElement(graphics, theme, customization, WidgetTextElementKeys.Location, SampleState.SecondaryLabel);
            DrawElement(graphics, theme, customization, WidgetTextElementKeys.Detail, "Next prayer countdown");
        }

        int width = Math.Clamp((customization.Width ?? theme.DisplaySize.Width) * 2, 320, 1800);
        int height = Math.Clamp((customization.Height ?? theme.DisplaySize.Height) * 2, 160, 1040);
        using var output = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(output))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(logical, new Rectangle(0, 0, width, height), new Rectangle(Point.Empty, theme.Size), GraphicsUnit.Pixel);
        }

        output.Save(Path.Combine(screenshotsDirectory, OutputNames[theme.Key]), ImageFormat.Png);
    }

    private static void DrawElement(
        Graphics graphics,
        WidgetThemeDefinition theme,
        WidgetThemeCustomization customization,
        string elementKey,
        string text)
    {
        WidgetTextCustomization? element = customization.Elements.TryGetValue(elementKey, out WidgetTextCustomization? found)
            ? found
            : null;
        if (element?.Visible == false)
        {
            return;
        }

        ThemeTextDefaults defaults = ThemeEditorForm.GetTextDefaults(theme, elementKey);
        Rectangle bounds = ThemeEditorForm.GetBounds(element, defaults.Bounds);
        string fontFamily = element?.FontFamily ?? defaults.FontFamily;
        float fontSize = element?.FontSize ?? defaults.FontSize;
        FontStyle style = element?.Bold ?? defaults.Bold ? FontStyle.Bold : FontStyle.Regular;
        Color color = ThemeEditorForm.ParseColor(element?.Color, defaults.Color);
        StringAlignment alignment = ThemeEditorForm.ParseAlignment(element?.Alignment, defaults.Alignment);

        using var font = new Font(fontFamily, fontSize, style, GraphicsUnit.Point);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = alignment,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        int shadowAlpha = Math.Clamp(element?.ShadowAlpha ?? 0, 0, 255);
        TextEffectRenderer.DrawSoftText(
            graphics,
            text,
            font,
            format,
            bounds,
            Color.Black,
            shadowAlpha,
            Math.Clamp(element?.ShadowBlur ?? 2, 0, 12),
            element?.ShadowOffsetX ?? 1,
            element?.ShadowOffsetY ?? 1);

        int glowAlpha = Math.Clamp(element?.GlowAlpha ?? 0, 0, 255);
        int glowBlur = Math.Clamp(element?.GlowBlur ?? 0, 0, 16);
        if (glowAlpha > 0 && glowBlur > 0)
        {
            TextEffectRenderer.DrawSoftText(
                graphics,
                text,
                font,
                format,
                bounds,
                ThemeEditorForm.ParseColor(element?.GlowColor, color),
                glowAlpha,
                glowBlur,
                0,
                0);
        }

        graphics.DrawString(text, font, brush, bounds, format);
    }
}
