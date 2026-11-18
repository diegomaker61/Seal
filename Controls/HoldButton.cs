using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Seal.Controls;

public sealed class HoldButton : Button
{
    public static readonly DependencyProperty HoldProgressProperty = DependencyProperty.Register(
        nameof(HoldProgress), typeof(double), typeof(HoldButton), new PropertyMetadata(0.0));
    public double HoldProgress { get => (double)GetValue(HoldProgressProperty); private set => SetValue(HoldProgressProperty, value); }
    private readonly Stopwatch watch = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(25) };
    private bool confirmed;

    public HoldButton()
    {
        timer.Tick += (_, _) =>
        {
            HoldProgress = Math.Min(100, watch.Elapsed.TotalSeconds / 2 * 100);
            if (HoldProgress < 100) return;
            CancelHold();
            confirmed = true;
            try { OnClick(); } finally { confirmed = false; }
        };
        LostMouseCapture += (_, _) => CancelHold();
        LostKeyboardFocus += (_, _) => CancelHold();
        MouseLeave += (_, _) => CancelHold();
        Unloaded += (_, _) => CancelHold();
        IsEnabledChanged += (_, _) => { if (!IsEnabled) CancelHold(); };
    }

    protected override void OnClick() { if (confirmed) base.OnClick(); }
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        Focus();
        CaptureMouse();
        BeginHold();
        e.Handled = true;
    }
    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        CancelHold();
        ReleaseMouseCapture();
        e.Handled = true;
        base.OnPreviewMouseLeftButtonUp(e);
    }
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter) { if (!e.IsRepeat) BeginHold(); e.Handled = true; }
        else if (e.Key == Key.Escape) CancelHold();
        base.OnPreviewKeyDown(e);
    }
    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter) { CancelHold(); e.Handled = true; }
        base.OnPreviewKeyUp(e);
    }
    private void BeginHold() { watch.Restart(); timer.Start(); }
    private void CancelHold() { timer.Stop(); watch.Reset(); HoldProgress = 0; }
}
