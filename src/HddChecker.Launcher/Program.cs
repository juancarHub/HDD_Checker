using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;

namespace HddChecker.Launcher;

internal static class Program
{
    private const string ServiceName = "STHolmes";

    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var command = args.FirstOrDefault();
        if (command?.Equals("/console", StringComparison.OrdinalIgnoreCase) is true)
        {
            return LaunchComponent("app", "STHolmes.App.exe", "HddChecker.App");
        }

        if (command?.Equals("/agent", StringComparison.OrdinalIgnoreCase) is true)
        {
            return LaunchComponent("agent", "STHolmes.Agent.exe", "HddChecker.Agent");
        }

        if (command is not null)
        {
            MessageBox.Show(
                "Uso: ST_Holmes.exe [/console | /agent]",
                "ST_Holmes",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return 2;
        }

        if (!EnsureServiceRunning())
        {
            return 1;
        }

        return LaunchComponent("agent", "STHolmes.Agent.exe", "HddChecker.Agent");
    }

    private static bool EnsureServiceRunning()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            if (service.Status is ServiceControllerStatus.Running)
            {
                return true;
            }

            if (service.Status is ServiceControllerStatus.Stopped)
            {
                service.Start();
            }

            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
            return true;
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show(
                "El servicio de ST_Holmes todavía no está instalado. " +
                "Ejecuta el instalador como administrador.",
                "ST_Holmes",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }
        catch (Exception exception) when (exception is Win32Exception or System.ServiceProcess.TimeoutException)
        {
            MessageBox.Show(
                $"No se pudo iniciar el servicio. {exception.Message}",
                "ST_Holmes",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
    }

    private static int LaunchComponent(
        string directoryName,
        string executableName,
        string projectDirectoryName)
    {
        var executable = ResolveComponent(directoryName, executableName, projectDirectoryName);
        if (executable is null)
        {
            MessageBox.Show(
                $"No se encontró {executableName}. Repara o reinstala ST_Holmes.",
                "ST_Holmes",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return 1;
        }

        Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
        return 0;
    }

    private static string? ResolveComponent(
        string directoryName,
        string executableName,
        string projectDirectoryName)
    {
        var installed = Path.Combine(AppContext.BaseDirectory, directoryName, executableName);
        if (File.Exists(installed))
        {
            return installed;
        }

        var root = FindDevelopmentRoot();
        if (root is null)
        {
            return null;
        }

        foreach (var configuration in new[] { "Debug", "Release" })
        {
            var candidate = Path.Combine(
                root,
                "src",
                projectDirectoryName,
                "bin",
                configuration,
                "net10.0-windows",
                executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? FindDevelopmentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
