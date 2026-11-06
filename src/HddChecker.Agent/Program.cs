using System.Threading;

namespace HddChecker.Agent;

internal static class Program
{
    private const string InstanceName = @"Local\HddChecker.NotificationAgent.Instance";

    [STAThread]
    private static void Main(string[] args)
    {
        using var instanceMutex = new Mutex(true, InstanceName, out var ownsInstance);
        if (!ownsInstance)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        using var context = new NotificationApplicationContext(DataRootResolver.Resolve(args));
        Application.Run(context);
        instanceMutex.ReleaseMutex();
    }
}
