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
        graphics.DrawEllipse(outline, 6, 5, 16, 16);
        graphics.DrawLine(detail, 19, 19, 25, 25);
        graphics.DrawLines(detail,
        [
            new PointF(9, 15),
            new PointF(12, 15),
            new PointF(14, 11),
            new PointF(17, 17),
            new PointF(20, 13),
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
}
