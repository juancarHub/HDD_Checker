using System.Drawing;
using HddChecker.Core.Analysis;

namespace HddChecker.Agent;

internal sealed class IncidentAlertForm : Form
{
    public IncidentAlertForm(AlertPresentation alert, Action openPanel)
    {
        var accent = alert.Severity switch
        {
            FindingSeverity.Critical => Color.FromArgb(239, 68, 68),
            FindingSeverity.Warning or FindingSeverity.Attention => Color.FromArgb(245, 158, 11),
            _ => Color.FromArgb(34, 197, 94),
        };

        Text = "HDD Checker";
        Size = new Size(470, 270);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(17, 24, 39);
        ForeColor = Color.FromArgb(249, 250, 251);
        TopMost = true;
        ShowInTaskbar = false;
        Padding = new Padding(0);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 8,
            BackColor = accent,
        };
        var title = new Label
        {
            Text = alert.Title,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(249, 250, 251),
            Location = new Point(22, 24),
            AutoSize = false,
            Size = new Size(420, 32),
        };
        var message = new Label
        {
            Text = alert.Message,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(209, 213, 219),
            Location = new Point(22, 66),
            AutoSize = false,
            Size = new Size(420, 64),
        };
        var recommendationTitle = new Label
        {
            Text = "Recomendación",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = accent,
            Location = new Point(22, 137),
            AutoSize = true,
        };
        var recommendation = new Label
        {
            Text = alert.Recommendation,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(156, 163, 175),
            Location = new Point(22, 159),
            AutoSize = false,
            Size = new Size(420, 43),
        };
        var dismissButton = CreateButton("Descartar", Color.FromArgb(55, 65, 81), Color.White);
        dismissButton.Location = new Point(244, 218);
        dismissButton.Click += (_, _) => Close();
        var openButton = CreateButton("Abrir panel", accent, Color.FromArgb(17, 24, 39));
        openButton.Location = new Point(352, 218);
        openButton.Click += (_, _) =>
        {
            openPanel();
            Close();
        };

        Controls.Add(header);
        Controls.Add(title);
        Controls.Add(message);
        Controls.Add(recommendationTitle);
        Controls.Add(recommendation);
        Controls.Add(dismissButton);
        Controls.Add(openButton);
        Shown += (_, _) => PositionAtBottomRight();
    }

    private static Button CreateButton(string text, Color background, Color foreground) => new()
    {
        Text = text,
        Size = new Size(96, 34),
        FlatStyle = FlatStyle.Flat,
        BackColor = background,
        ForeColor = foreground,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        Cursor = Cursors.Hand,
        FlatAppearance = { BorderSize = 0 },
    };

    private void PositionAtBottomRight()
    {
        var workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(
            workingArea.Right - Width - 18,
            workingArea.Bottom - Height - 18);
    }
}
