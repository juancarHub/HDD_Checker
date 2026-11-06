using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HddChecker.App;

public partial class MetricChart : UserControl
{
    private IReadOnlyList<ChartSample> samples = [];
    private string unit = string.Empty;

    public MetricChart()
    {
        InitializeComponent();
        PlotCanvas.SizeChanged += (_, _) => RenderChart();
    }

    public void ShowSeries(string title, string valueUnit, IReadOnlyList<ChartSample> values)
    {
        TitleText.Text = title;
        unit = valueUnit;
        samples = values.OrderBy(sample => sample.Timestamp).ToArray();
        RenderChart();
    }

    private void RenderChart()
    {
        PlotCanvas.Children.Clear();
        FirstDateText.Text = string.Empty;
        LastDateText.Text = string.Empty;
        RangeText.Text = string.Empty;

        if (samples.Count < 2 || PlotCanvas.ActualWidth < 120 || PlotCanvas.ActualHeight < 80)
        {
            AddCenteredMessage(samples.Count == 0
                ? "Esta unidad no informa esta métrica."
                : "Se necesitan al menos dos mediciones comparables.");
            return;
        }

        const double left = 48;
        const double right = 12;
        const double top = 12;
        const double bottom = 18;
        var width = PlotCanvas.ActualWidth - left - right;
        var height = PlotCanvas.ActualHeight - top - bottom;
        var minimum = samples.Min(sample => sample.Value);
        var maximum = samples.Max(sample => sample.Value);
        var valueRange = maximum - minimum;
        if (valueRange == 0)
        {
            valueRange = Math.Max(Math.Abs(maximum) * 0.1, 1);
            minimum -= valueRange / 2;
            maximum += valueRange / 2;
        }
        else
        {
            var padding = valueRange * 0.12;
            minimum -= padding;
            maximum += padding;
        }

        var firstTime = samples[0].Timestamp;
        var lastTime = samples[^1].Timestamp;
        var elapsedTicks = Math.Max((lastTime - firstTime).Ticks, 1);

        for (var lineIndex = 0; lineIndex <= 4; lineIndex++)
        {
            var ratio = lineIndex / 4d;
            var y = top + (height * ratio);
            PlotCanvas.Children.Add(new Line
            {
                X1 = left,
                X2 = left + width,
                Y1 = y,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                StrokeThickness = 1,
            });

            var labelValue = maximum - ((maximum - minimum) * ratio);
            var label = new TextBlock
            {
                Text = FormatValue(labelValue),
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                FontSize = 10,
            };
            Canvas.SetLeft(label, 0);
            Canvas.SetTop(label, y - 8);
            PlotCanvas.Children.Add(label);
        }

        var points = new PointCollection();
        foreach (var sample in samples)
        {
            var xRatio = (sample.Timestamp - firstTime).Ticks / (double)elapsedTicks;
            var yRatio = (maximum - sample.Value) / (maximum - minimum);
            var x = left + (width * xRatio);
            var y = top + (height * yRatio);
            points.Add(new Point(x, y));
            var marker = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                ToolTip = $"{sample.Timestamp.ToLocalTime():g}: {FormatValue(sample.Value)} {unit}".Trim(),
            };
            Canvas.SetLeft(marker, x - 4);
            Canvas.SetTop(marker, y - 4);
            PlotCanvas.Children.Add(marker);
        }

        PlotCanvas.Children.Insert(5, new Polyline
        {
            Points = points,
            Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
            StrokeThickness = 2.5,
        });

        RangeText.Text = $"{FormatValue(samples.Min(sample => sample.Value))}–" +
                         $"{FormatValue(samples.Max(sample => sample.Value))} {unit}".TrimEnd();
        FirstDateText.Text = firstTime.ToLocalTime().ToString("g");
        LastDateText.Text = lastTime.ToLocalTime().ToString("g");
    }

    private void AddCenteredMessage(string message)
    {
        var text = new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            TextAlignment = TextAlignment.Center,
        };
        text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(text, Math.Max((PlotCanvas.ActualWidth - text.DesiredSize.Width) / 2, 0));
        Canvas.SetTop(text, Math.Max((PlotCanvas.ActualHeight - text.DesiredSize.Height) / 2, 0));
        PlotCanvas.Children.Add(text);
    }

    private string FormatValue(double value) =>
        value.ToString("0.##", CultureInfo.CurrentCulture);
}
