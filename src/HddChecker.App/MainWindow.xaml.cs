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
    private readonly ObservableCollection<MeasurementView> measurements = [];
    private readonly ObservableCollection<IncidentView> incidents = [];
    private IReadOnlyList<DeviceMeasurement> timelineEntries = [];
    private static readonly IReadOnlyList<MetricOption> MetricOptions =
    [
        new("Temperatura", "°C", measurement => measurement.TemperatureCelsius),
        new("Horas de funcionamiento", "h", measurement => measurement.PowerOnHours),
        new("Vida restante", "%", measurement => measurement.RemainingLifePercent),
        new("Sectores reasignados", string.Empty, measurement => measurement.ReallocatedSectors),
        new("Sectores pendientes", string.Empty, measurement => measurement.PendingSectors),
        new("Errores de medios NVMe", string.Empty, measurement => measurement.NvmeMediaErrors),
    ];

    public MainWindow(string dataRoot)
    {
        InitializeComponent();
        databasePath = Path.Combine(dataRoot, "data", "hdd-checker.db");
        DevicesList.ItemsSource = devices;
        TimelineGrid.ItemsSource = measurements;
        IncidentGrid.ItemsSource = incidents;
        MetricSelector.ItemsSource = MetricOptions;
        MetricSelector.SelectedIndex = 0;
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

    private async void DevicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
        await LoadTimelineAsync(device);
    }

    private async Task LoadTimelineAsync(DeviceStatusView device)
    {
        measurements.Clear();
        try
        {
            var entries = await new CaptureIndex(databasePath)
                .GetMeasurementsAsync(device.IdentityKey);
            if (DevicesList.SelectedItem is not DeviceStatusView selected ||
                selected.IdentityKey != device.IdentityKey)
            {
                return;
            }

            foreach (var entry in entries)
            {
                measurements.Add(MeasurementView.FromMeasurement(entry));
            }

            timelineEntries = entries;
            UpdateChart();
            await LoadIncidentsAsync(device);

            TimelineSummaryText.Text = entries.Count switch
            {
                0 => "Todavía no hay mediciones para esta unidad.",
                1 => "Primera medición registrada; la tendencia comenzará con la siguiente.",
                _ => $"{entries.Count} mediciones disponibles, de la más reciente a la más antigua."
            };
        }
        catch (Exception exception)
        {
            timelineEntries = [];
            UpdateChart();
            TimelineSummaryText.Text = $"No se pudo abrir la cronología: {exception.Message}";
        }
    }

    private async Task LoadIncidentsAsync(DeviceStatusView device)
    {
        incidents.Clear();
        try
        {
            var entries = await new CaptureIndex(databasePath)
                .GetNotificationHistoryAsync(device.IdentityKey);
            if (DevicesList.SelectedItem is not DeviceStatusView selected ||
                selected.IdentityKey != device.IdentityKey)
            {
                return;
            }

            foreach (var entry in entries)
            {
                incidents.Add(IncidentView.FromEntry(entry));
            }

            IncidentSummaryText.Text = entries.Count == 0
                ? "No se ha registrado ninguna incidencia para esta unidad."
                : $"{entries.Count} incidencias registradas, de la más reciente a la más antigua.";
        }
        catch (Exception exception)
        {
            IncidentSummaryText.Text = $"No se pudo abrir el historial: {exception.Message}";
        }
    }

    private void MetricSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateChart();

    private void UpdateChart()
    {
        if (MetricSelector.SelectedItem is not MetricOption option)
        {
            return;
        }

        var samples = timelineEntries
            .Select(measurement => new
            {
                measurement.CapturedAtUtc,
                Value = option.SelectValue(measurement),
            })
            .Where(item => item.Value is not null)
            .Select(item => new ChartSample(item.CapturedAtUtc, item.Value!.Value))
            .ToArray();
        TimelineChart.ShowSeries(option.Name, option.Unit, samples);
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

    private sealed record MeasurementView(
        string CapturedAt,
        string Temperature,
        string Hours,
        string Life,
        string State)
    {
        public static MeasurementView FromMeasurement(DeviceMeasurement measurement) => new(
            measurement.CapturedAtUtc.ToLocalTime().ToString("g"),
            measurement.TemperatureCelsius is int temperature ? $"{temperature} °C" : "—",
            measurement.PowerOnHours?.ToString("N0") ?? "—",
            measurement.RemainingLifePercent is int life ? $"{life} %" : "—",
            measurement.SmartPassed switch
            {
                false => "SMART fallido",
                true when measurement.AcquisitionOutcome.Equals("Complete", StringComparison.OrdinalIgnoreCase) =>
                    "Completa",
                true => "Parcial",
                _ => "No informado"
            });
    }

    private sealed record MetricOption(
        string Name,
        string Unit,
        Func<DeviceMeasurement, double?> SelectValue);

    private sealed record IncidentView(
        string CreatedAt,
        string Severity,
        string Message,
        string Recommendation)
    {
        public static IncidentView FromEntry(NotificationHistoryEntry entry) => new(
            entry.CreatedAtUtc.ToLocalTime().ToString("g"),
            entry.Severity switch
            {
                FindingSeverity.Critical => "Crítico",
                FindingSeverity.Warning => "Advertencia",
                FindingSeverity.Attention => "Atención",
                _ => "Información"
            },
            entry.Message,
            entry.Recommendation);
    }
}
