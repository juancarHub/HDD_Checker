using System.Diagnostics;
using System.Drawing;
using HddChecker.Core.Analysis;
using HddChecker.Storage;

namespace HddChecker.Agent;

internal sealed class NotificationApplicationContext : ApplicationContext
{
    private readonly string databasePath;
    private readonly NotifyIcon notifyIcon;
    private readonly System.Windows.Forms.Timer pollTimer;
    private readonly DeliveredNotificationStore deliveredNotifications = new();
    private bool polling;

    public NotificationApplicationContext(string dataRoot)
    {
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
            Icon = SystemIcons.Information,
            Text = "HDD Checker",
            Visible = true,
            ContextMenuStrip = menu,
        };
        notifyIcon.DoubleClick += (_, _) => OpenConsole();
        notifyIcon.BalloonTipClicked += (_, _) => OpenConsole();

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
        notifyIcon.ShowBalloonTip(
            10_000,
            preview.Title,
            preview.Message,
            preview.Icon);
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength
            ? value
            : string.Concat(value.AsSpan(0, maximumLength - 1), "…");

    private static void OpenConsole()
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

        Process.Start(new ProcessStartInfo(executable)
        {
            UseShellExecute = true,
        });
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
