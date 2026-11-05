using System.IO;
using System.Threading;
using System.Windows;

namespace HddChecker.App;

public partial class App : Application
{
    private const string InstanceName = @"Local\HddChecker.Console.Instance";
    private const string ActivationName = @"Local\HddChecker.Console.Activate";
    private Mutex? instanceMutex;
    private bool ownsInstance;
    private EventWaitHandle? activationEvent;
    private EventWaitHandle? shutdownEvent;

    protected override void OnStartup(StartupEventArgs e)
    {
        instanceMutex = new Mutex(true, InstanceName, out ownsInstance);
        if (!ownsInstance)
        {
            SignalExistingInstance();
            Shutdown();
            return;
        }

        base.OnStartup(e);
        activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationName);
        shutdownEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        var window = new MainWindow(ResolveDataRoot(e.Args));
        MainWindow = window;
        window.Show();
        _ = ListenForActivationAsync(window, activationEvent, shutdownEvent);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        shutdownEvent?.Set();
        activationEvent?.Dispose();
        shutdownEvent?.Dispose();
        if (ownsInstance)
        {
            instanceMutex?.ReleaseMutex();
        }
        instanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var signal = EventWaitHandle.OpenExisting(ActivationName);
            signal.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // The first instance is still completing its startup.
        }
    }

    private static async Task ListenForActivationAsync(
        Window window,
        EventWaitHandle activation,
        EventWaitHandle shutdown)
    {
        await Task.Run(() =>
        {
            var handles = new WaitHandle[] { activation, shutdown };
            while (WaitHandle.WaitAny(handles) == 0)
            {
                window.Dispatcher.Invoke(() =>
                {
                    if (window.WindowState is WindowState.Minimized)
                    {
                        window.WindowState = WindowState.Normal;
                    }

                    window.Show();
                    window.Activate();
                    window.Topmost = true;
                    window.Topmost = false;
                    window.Focus();
                });
            }
        });
    }

    private static string ResolveDataRoot(IReadOnlyList<string> arguments)
    {
        for (var index = 0; index < arguments.Count - 1; index++)
        {
            if (arguments[index].Equals("--data-root", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(arguments[index + 1]);
            }
        }

        var installedDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "HDD_Checker");
        if (File.Exists(Path.Combine(installedDataRoot, "data", "hdd-checker.db")))
        {
            return installedDataRoot;
        }

        var developmentRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        if (Directory.Exists(Path.Combine(developmentRoot, ".git")) &&
            File.Exists(Path.Combine(developmentRoot, "data", "hdd-checker.db")))
        {
            return developmentRoot;
        }

        return installedDataRoot;
    }
}
