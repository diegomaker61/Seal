using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Seal.Controls;

public sealed class RoundedProgressBar : ProgressBar
{
    private static readonly DependencyProperty DisplayValueProperty = DependencyProperty.Register(
        "DisplayValue", typeof(double), typeof(RoundedProgressBar),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsShimmerActiveProperty = DependencyProperty.Register(
        nameof(IsShimmerActive), typeof(bool), typeof(RoundedProgressBar),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender,
            (sender, _) => ((RoundedProgressBar)sender).UpdateShimmer()));

    private static readonly DependencyProperty ShimmerPhaseProperty = DependencyProperty.Register(
        "ShimmerPhase", typeof(double), typeof(RoundedProgressBar),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsShimmerActive
    {
        get => (bool)GetValue(IsShimmerActiveProperty);
        set => SetValue(IsShimmerActiveProperty, value);
    }

    public RoundedProgressBar()
    {
        Loaded += (_, _) => UpdateShimmer();
        IsVisibleChanged += (_, _) => UpdateShimmer();
        Unloaded += (_, _) => BeginAnimation(ShimmerPhaseProperty, null);
    }

    private void UpdateShimmer()
    {
        if (!IsShimmerActive || !IsLoaded || !IsVisible)
        {
            BeginAnimation(ShimmerPhaseProperty, null);
            return;
        }

        BeginAnimation(ShimmerPhaseProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(3))
        {
            RepeatBehavior = RepeatBehavior.Forever
        });
    }

    private void DrawShimmer(DrawingContext context, Rect fill, double radius)
    {
        var length = Math.Clamp(fill.Width * 0.65, 35, 100);
        var phase = (double)GetValue(ShimmerPhaseProperty);
        var tip = phase * (fill.Width + length);
        var left = tip - length;
        var inset = 1.0;
        var top = inset;
        var bottom = fill.Height - inset;
        var capRadius = Math.Max(0, (bottom - top) / 2);
        if (capRadius <= 0) return;

        var shape = new StreamGeometry();
        using (var path = shape.Open())
        {
            path.BeginFigure(new Point(left, top), true, true);
            path.LineTo(new Point(tip - capRadius, top), true, false);
            path.ArcTo(new Point(tip - capRadius, bottom), new Size(capRadius, capRadius),
                0, false, SweepDirection.Clockwise, true, false);
            path.LineTo(new Point(left, bottom), true, false);
        }
        shape.Freeze();
        var light = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint = new Point(left, 0),
            EndPoint = new Point(tip, 0),
            GradientStops = new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(0, 45, 255, 235), 0),
                new GradientStop(Color.FromArgb(100, 45, 245, 235), 0.3),
                new GradientStop(Color.FromArgb(235, 55, 255, 240), 0.65),
                new GradientStop(Color.FromArgb(245, 90, 255, 245), 0.88),
                new GradientStop(Color.FromArgb(140, 90, 255, 245), 1)
            }
        };
        light.Freeze();
        context.PushClip(new RectangleGeometry(fill, radius, radius));
        var edgeMask = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(125, 0, 0, 0), 0),
                new GradientStop(Colors.Black, 0.2),
                new GradientStop(Colors.Black, 0.8),
                new GradientStop(Color.FromArgb(125, 0, 0, 0), 1)
            }
        };
        edgeMask.Freeze();
        context.PushOpacityMask(edgeMask);
        context.DrawGeometry(light, null, shape);
        context.Pop();
        context.Pop();
    }

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        var displayed = (double)GetValue(DisplayValueProperty);
        BeginAnimation(DisplayValueProperty, null);
        SetValue(DisplayValueProperty, newValue);
        if (IsLoaded && newValue > oldValue && newValue - oldValue < 5)
        {
            BeginAnimation(DisplayValueProperty,
                new DoubleAnimation(displayed, newValue, TimeSpan.FromMilliseconds(200))
                { FillBehavior = FillBehavior.Stop });
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        var radius = height / 2;
        var track = new Rect(0, 0, width, height);
        drawingContext.DrawRoundedRectangle(Background, null, track, radius, radius);
        var fraction = Maximum > Minimum ? Math.Clamp(((double)GetValue(DisplayValueProperty) - Minimum) / (Maximum - Minimum), 0, 1) : 0;
        var fillWidth = width * fraction;
        if (fillWidth <= 0) return;

        drawingContext.PushClip(new RectangleGeometry(track, radius, radius));
        var fillRadius = Math.Min(radius, fillWidth / 2);
        var fill = new Rect(0, 0, fillWidth, height);
        drawingContext.DrawRoundedRectangle(Foreground, null, fill, fillRadius, fillRadius);
        if (IsShimmerActive) DrawShimmer(drawingContext, fill, fillRadius);
        drawingContext.Pop();
    }
}
