using System.Windows;
using System.Windows.Media;

namespace Seal.Controls;

public sealed class HoldProgressRing : FrameworkElement
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(HoldProgressRing),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }

    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        var progress = Math.Clamp(Progress / 100, 0, 1);
        var radius = Math.Min(ActualWidth, ActualHeight) / 2 - 1.5;
        if (progress <= 0 || radius <= 0) return;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var pen = new Pen((Brush)FindResource("AccentBrush"), 2.5)
        { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (progress >= 1)
        {
            context.DrawEllipse(null, pen, center, radius, radius);
            return;
        }
        var angle = progress * Math.PI * 2;
        var end = new Point(center.X + Math.Sin(angle) * radius, center.Y - Math.Cos(angle) * radius);
        var arc = new StreamGeometry();
        using (var path = arc.Open())
        {
            path.BeginFigure(new Point(center.X, center.Y - radius), false, false);
            path.ArcTo(end, new Size(radius, radius), 0, progress > 0.5, SweepDirection.Clockwise, true, false);
        }
        arc.Freeze();
        context.DrawGeometry(null, pen, arc);
    }
}
