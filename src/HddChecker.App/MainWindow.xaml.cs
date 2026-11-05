using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using HddChecker.Core.Analysis;
using HddChecker.Storage;

namespace HddChecker.App;

public partial class MainWindow : Window
{
    private readonly string databasePath;
    private readonly ObservableCollection<DeviceStatusView> devices = [];

    public MainWindow(string dataRoot)
    {
        InitializeComponent();
        databasePath = Path.Combine(dataRoot, "data", "hdd-checker.db");
        DevicesList.ItemsSource = devices;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        devices.Clear();
        if (!File.Exists(databasePath))
        {
            ShowEmpty("Todavía no existe un catálogo de supervisión.\nEl servicio creará uno en su primer ciclo.");
            return;
        }

        try
        {
            var statuses = await new CaptureIndex(databasePath).GetStatusesAsync();
            foreach (var status in statuses)
            {
                devices.Add(DeviceStatusView.FromStatus(status));
            }

            if (devices.Count == 0)
            {
                ShowEmpty("El catálogo no contiene unidades todavía.");
                return;
            }

            DevicesList.SelectedIndex = 0;
        }
        catch (Exception exception)
        {
            ShowEmpty($"No se pudo abrir el catálogo.\n{exception.Message}");
        }
    }

    private void DevicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DevicesList.SelectedItem is not DeviceStatusView device)
        {
            return;
        }

        EmptyMessage.Visibility = Visibility.Collapsed;
        DetailsPanel.Visibility = Visibility.Visible;
        ModelText.Text = device.ModelName;
        IdentityText.Text = $"{device.Protocol} · {device.IdentityKey}";
        HealthText.Text = device.Health;
        LatestText.Text = device.LatestCapture;
        TemperatureText.Text = device.Temperature;
        HoursText.Text = device.PowerOnHours;
        LifeText.Text = device.RemainingLife;
        CapturesText.Text = device.CaptureCount.ToString();
        ReallocatedText.Text = device.Reallocated;
        PendingText.Text = device.Pending;
        FindingText.Text = device.Finding;
    }

    private void ShowEmpty(string message)
    {
        DetailsPanel.Visibility = Visibility.Collapsed;
        EmptyMessage.Text = message;
        EmptyMessage.Visibility = Visibility.Visible;
    }

    private sealed record DeviceStatusView(
        string IdentityKey,
        string ModelName,
        string Protocol,
        long CaptureCount,
        string Summary,
        string Health,
        string LatestCapture,
        string Temperature,
        string PowerOnHours,
        string RemainingLife,
        string Reallocated,
        string Pending,
        string Finding)
    {
        public static DeviceStatusView FromStatus(DeviceStatus status)
        {
            var severity = status.HighestFindingSeverity switch
            {
                FindingSeverity.Critical => "Crítico",
                FindingSeverity.Warning => "Advertencia",
                FindingSeverity.Attention => "Atención",
                FindingSeverity.Information => "Informativo",
                _ => status.SmartPassed is false ? "SMART no aprobado" : "Sin incidencias"
            };

            return new DeviceStatusView(
                status.IdentityKey,
                status.ModelName ?? "Unidad sin nombre",
                status.Protocol ?? "Protocolo no disponible",
                status.CaptureCount,
                $"{status.Protocol ?? "Desconocido"} · {severity}",
                severity,
                status.LatestCaptureUtc.ToLocalTime().ToString("g"),
                status.TemperatureCelsius is int temperature ? $"{temperature} °C" : "No disponible",
                status.PowerOnHours?.ToString("N0") ?? "No disponible",
                status.RemainingLifePercent is int life ? $"{life} %" : "No disponible",
                status.ReallocatedSectors?.ToString("N0") ?? "No disponible",
                status.PendingSectors?.ToString("N0") ?? "No disponible",
                status.HighestFindingMessage ?? "No se han detectado cambios que requieran atención.");
        }
    }
}
