using System.Diagnostics;
using System.Drawing;
using HddChecker.Core.Analysis;
using HddChecker.Storage;

namespace HddChecker.Agent;

internal sealed class NotificationApplicationContext : ApplicationContext
{
    private readonly string databasePath;
    private readonly string dataRoot;
    private readonly NotifyIcon notifyIcon;
    private readonly System.Windows.Forms.Timer pollTimer;
    private readonly DeliveredNotificationStore deliveredNotifications = new();
    private bool polling;
    private FindingSeverity? currentSeverity;
    private Icon? currentIcon;
    private IncidentAlertForm? activeAlert;

    public NotificationApplicationContext(string dataRoot)
    {
        this.dataRoot = dataRoot;
        databasePath = Path.Combine(dataRoot, "data", "hdd-checker.db");
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir panel", null, (_, _) => OpenConsole());
        var previewMenu = new ToolStripMenuItem("Probar avisos");
        foreach (var preview in NotificationPreview.All)
        {
            var item = new ToolStripMenuItem(preview.Title.Replace("[SIMULACIÓN] ", string.Empty));
            item.Click += (_, _) => ShowPreview(preview);
            previewMenu.DropDownItems.Add(item);
        }

        menu.Items.Add(previewMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Cerrar agente de notificaciones", null, (_, _) => ExitThread());

        notifyIcon = new NotifyIcon
        {
            Text = "HDD Checker",
            Visible = true,
            ContextMenuStrip = menu,
        };
        notifyIcon.DoubleClick += (_, _) => OpenConsole();
        notifyIcon.BalloonTipClicked += (_, _) => OpenConsole();
        UpdateStatusIcon(null);

        pollTimer = new System.Windows.Forms.Timer { Interval = 15_000 };
        pollTimer.Tick += async (_, _) => await PollAsync();
        pollTimer.Start();

        notifyIcon.ShowBalloonTip(
            5_000,
            "Supervisión activa",
            "HDD Checker vigila las unidades y mostrará aquí cualquier incidencia nueva.",
            ToolTipIcon.Info);
        _ = PollAsync();
    }

    protected override void ExitThreadCore()
    {
        pollTimer.Stop();
        pollTimer.Dispose();
        notifyIcon.Visible = false;
        activeAlert?.Close();
        currentIcon?.Dispose();
        notifyIcon.Dispose();
        base.ExitThreadCore();
    }

    private async Task PollAsync()
    {
        if (polling || !File.Exists(databasePath))
        {
            return;
        }

        polling = true;
        try
        {
            var index = new CaptureIndex(databasePath);
            var statuses = await index.GetStatusesAsync();
            currentSeverity = statuses
                .Select(status => status.SmartPassed is false
                    ? FindingSeverity.Critical
                    : status.HighestFindingSeverity)
                .Where(severity => severity is not null)
                .DefaultIfEmpty(null)
                .Max();
            UpdateStatusIcon(currentSeverity);
            var pending = await index.GetPendingNotificationsAsync(100);
            var notification = pending.FirstOrDefault(candidate =>
                !deliveredNotifications.Contains(candidate.EventKey));
            if (notification is null)
            {
                return;
            }

            notifyIcon.ShowBalloonTip(
                10_000,
                Truncate(notification.Title, 63),
                Truncate(
                    $"{notification.ModelName}: {notification.Message} " +
                    notification.Recommendation,
                    255),
                notification.Severity is FindingSeverity.Critical
                    ? ToolTipIcon.Error
                    : ToolTipIcon.Warning);
            ShowCustomAlert(new AlertPresentation(
                notification.Title,
                $"{notification.ModelName}: {notification.Message}",
                notification.Recommendation,
                notification.Severity,
                false));
            await deliveredNotifications.AddAsync(notification.EventKey);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"No se pudo consultar la cola de alertas: {exception}");
        }
        finally
        {
            polling = false;
        }
    }

    private void ShowPreview(NotificationPreview preview)
    {
        UpdateStatusIcon(preview.Severity);
        notifyIcon.ShowBalloonTip(
            10_000,
            preview.Title,
            preview.Message,
            preview.Icon);
        ShowCustomAlert(new AlertPresentation(
            preview.Title,
            preview.Message,
            preview.Recommendation,
            preview.Severity,
            true));
    }

    private void ShowCustomAlert(AlertPresentation alert)
    {
        activeAlert?.Close();
        activeAlert = new IncidentAlertForm(alert, OpenConsole);
        activeAlert.FormClosed += (_, _) =>
        {
            activeAlert = null;
            UpdateStatusIcon(currentSeverity);
        };
        activeAlert.Show();
    }

    private void UpdateStatusIcon(FindingSeverity? severity)
    {
        var color = severity switch
        {
            FindingSeverity.Critical => Color.FromArgb(239, 68, 68),
            FindingSeverity.Warning or FindingSeverity.Attention => Color.FromArgb(245, 158, 11),
            _ => Color.FromArgb(34, 197, 94),
        };
        var nextIcon = StatusIconFactory.Create(color);
        var previousIcon = currentIcon;
        currentIcon = nextIcon;
        notifyIcon.Icon = nextIcon;
        previousIcon?.Dispose();
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength
            ? value
            : string.Concat(value.AsSpan(0, maximumLength - 1), "…");

    private void OpenConsole()
    {
        var executable = ResolveConsoleExecutable();
        if (executable is null)
        {
            MessageBox.Show(
                "No se encontró la interfaz de HDD Checker.",
                "HDD Checker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = true,
        };
        startInfo.ArgumentList.Add("--data-root");
        startInfo.ArgumentList.Add(dataRoot);
        Process.Start(startInfo);
    }

    private static string? ResolveConsoleExecutable()
    {
        var packaged = Path.Combine(AppContext.BaseDirectory, "HddChecker.App.exe");
        if (File.Exists(packaged))
        {
            return packaged;
        }

        var packagedSibling = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "app",
            "HddChecker.App.exe"));
        if (File.Exists(packagedSibling))
        {
            return packagedSibling;
        }

        var developmentRoot = DataRootResolver.FindDevelopmentRoot();
        if (developmentRoot is null)
        {
            return null;
        }

        foreach (var configuration in new[] { "Debug", "Release" })
        {
            var candidate = Path.Combine(
                developmentRoot,
                "src",
                "HddChecker.App",
                "bin",
                configuration,
                "net10.0-windows",
                "HddChecker.App.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
