namespace HddChecker.Agent;

internal static class DataRootResolver
{
    public static string Resolve(IReadOnlyList<string> arguments)
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

        var developmentRoot = FindDevelopmentRoot();
        return developmentRoot ?? installedDataRoot;
    }

    public static string? FindDevelopmentRoot()
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
