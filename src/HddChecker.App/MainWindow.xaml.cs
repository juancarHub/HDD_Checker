using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using HddChecker.Core.Analysis;
using HddChecker.Core.Smartctl;
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
        DiagnosisText.Text = device.Diagnosis;
        ConfidenceText.Text = $"Confianza: {device.Confidence}";
        ReasonsText.Text = device.Reasons;
        LimitationsText.Text = device.Limitations;
        TrajectoryText.Text = device.Trajectory;
        RecommendationText.Text = device.Recommendation;
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
        string Diagnosis,
        string Confidence,
        string Reasons,
        string Limitations,
        string Trajectory,
        string Recommendation)
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

            _ = Enum.TryParse<AcquisitionOutcome>(
                status.AcquisitionOutcome,
                true,
                out var acquisitionOutcome);
            var assessment = DeviceHealthAssessmentEvaluator.Evaluate(new DeviceHealthSignals(
                status.SmartPassed,
                acquisitionOutcome,
                status.Protocol,
                status.CaptureCount,
                status.TemperatureCelsius,
                status.RemainingLifePercent,
                status.NvmeAvailableSparePercent,
                status.ReallocatedSectors,
                status.PendingSectors,
                status.NvmeMediaErrors,
                status.HighestFindingSeverity,
                status.HighestFindingMessage));

            return new DeviceStatusView(
                status.IdentityKey,
                status.ModelName ?? "Unidad sin nombre",
                status.Protocol ?? "Protocolo no disponible",
                status.CaptureCount,
                $"{status.Protocol ?? "Desconocido"} · {severity}",
                severity,
                status.LatestCaptureUtc.ToLocalTime().ToString("g"),
                status.TemperatureCelsius is int temperature ? $"{temperature} °C" : "No informado",
                status.PowerOnHours?.ToString("N0") ?? "No informado",
                status.RemainingLifePercent is int life ? $"{life} %" : "No informado",
                status.ReallocatedSectors?.ToString("N0") ?? "No informado por la unidad",
                status.PendingSectors?.ToString("N0") ?? "No informado por la unidad",
                assessment.Diagnosis,
                assessment.Confidence switch
                {
                    AssessmentConfidence.High => "Alta",
                    AssessmentConfidence.Partial => "Parcial",
                    _ => "Limitada"
                },
                FormatItems(assessment.Reasons, "No hay motivos suficientes disponibles."),
                FormatItems(assessment.Limitations, "Ninguna limitación relevante en esta medición."),
                assessment.Trajectory,
                assessment.Recommendation);
        }

        private static string FormatItems(IReadOnlyList<string> items, string emptyText) =>
            items.Count == 0
                ? emptyText
                : string.Join(Environment.NewLine, items.Select(item => $"• {item}"));
    }
}
