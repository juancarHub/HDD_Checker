using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace HddChecker.Agent;

internal static class StatusIconFactory
{
    public static Icon Create(Color statusColor)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var background = new SolidBrush(statusColor);
        using var outline = new Pen(Color.White, 1.8f);
        using var detail = new Pen(Color.White, 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };

        graphics.FillEllipse(background, 1, 1, 30, 30);
        graphics.DrawEllipse(outline, 2, 2, 28, 28);
        graphics.DrawRoundedRectangle(outline, new RectangleF(7, 8, 18, 16), 3);
        graphics.DrawLines(detail,
        [
            new PointF(9, 17),
            new PointF(12, 17),
            new PointF(14, 13),
            new PointF(17, 20),
            new PointF(19, 16),
            new PointF(23, 16),
        ]);

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            _ = DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    private static void DrawRoundedRectangle(
        this Graphics graphics,
        Pen pen,
        RectangleF rectangle,
        float radius)
    {
        using var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.DrawPath(pen, path);
    }
}
