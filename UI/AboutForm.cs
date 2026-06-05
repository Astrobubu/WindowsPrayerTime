using System.Diagnostics;

namespace WindowsPrayerTime.UI;

public sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About Windows Prayer Time";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(520, 520);
        Size = new Size(600, 620);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Windows Prayer Time",
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(20, 60, 70),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(title, 0, 0);

        var about = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = BackColor,
            Text = "A quiet Windows tray app for prayer times, Adhan reminders, and Iqamah nudges while you are working at your PC.\n\n" +
                "Created by Ahmad / Astrobubu for a practical daily workflow: keep the next prayer visible without making the desktop noisy.\n\n" +
                "The app includes themed widgets, a manual layout editor, per-text font controls, and subtle reminder behavior for Adhan and Iqamah.\n\n" +
                "Constant Labs is a Dubai technology studio building practical AI agents, automation systems, custom software, websites, mobile apps, dashboards, integrations, and connected-product prototypes.\n\n" +
                "Credits:\n" +
                "- Prayer time data: AlAdhan Prayer Times API\n" +
                "- Network location fallback: ipapi\n" +
                "- Short Adhan cue: Beautiful adhan.ogg by Adam-synagda, CC0 1.0\n" +
                "- Built with Windows Forms and .NET"
        };
        root.Controls.Add(about, 0, 1);

        var links = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4
        };
        links.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        links.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddLink(links, "Project repo", "https://github.com/Astrobubu/WindowsPrayerTime");
        AddLink(links, "GitHub profile", "https://github.com/Astrobubu");
        AddLink(links, "Constant Labs", "https://constantlabs.ai/");
        AddLink(links, "Releases", "https://github.com/Astrobubu/WindowsPrayerTime/releases");
        root.Controls.Add(links, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var closeButton = new Button { Text = "Close", Width = 90, Height = 30 };
        closeButton.Click += (_, _) => Close();
        buttons.Controls.Add(closeButton);
        root.Controls.Add(buttons, 0, 3);

        Controls.Add(root);
    }

    private static void AddLink(TableLayoutPanel layout, string label, string url)
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        layout.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        var link = new LinkLabel
        {
            Text = url,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            LinkColor = Color.FromArgb(20, 102, 130),
            ActiveLinkColor = Color.FromArgb(120, 80, 160),
            VisitedLinkColor = Color.FromArgb(20, 102, 130)
        };
        link.Links.Add(0, url.Length, url);
        link.LinkClicked += (_, args) =>
        {
            if (args.Link?.LinkData is string target)
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
        };
        layout.Controls.Add(link, 1, row);
    }
}
