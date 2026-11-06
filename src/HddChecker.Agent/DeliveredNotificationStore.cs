namespace HddChecker.Agent;

internal sealed class DeliveredNotificationStore
{
    private readonly string path;
    private readonly HashSet<string> delivered;

    public DeliveredNotificationStore()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HDD_Checker");
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "delivered-notifications.txt");
        delivered = File.Exists(path)
            ? File.ReadLines(path)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);
    }

    public bool Contains(string eventKey) => delivered.Contains(eventKey);

    public async Task AddAsync(string eventKey)
    {
        if (!delivered.Add(eventKey))
        {
            return;
        }

        await File.AppendAllTextAsync(path, string.Concat(eventKey, Environment.NewLine));
    }
}
