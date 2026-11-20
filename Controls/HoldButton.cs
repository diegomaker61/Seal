using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Seal.Controls;

public sealed class HoldButton : Button
{
    public static readonly DependencyProperty HoldProgressProperty = DependencyProperty.Register(
        nameof(HoldProgress), typeof(double), typeof(HoldButton), new PropertyMetadata(0.0));
    public double HoldProgress { get => (double)GetValue(HoldProgressProperty); private set => SetValue(HoldProgressProperty, value); }
    private bool isHolding;
    private int holdVersion;
    private bool confirmed;

    public HoldButton()
    {
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
    private void BeginHold()
    {
        if (isHolding || !IsEnabled) return;
        isHolding = true;
        var version = ++holdVersion;
        var animation = new DoubleAnimation(0, 100, TimeSpan.FromSeconds(2));
        animation.Completed += (_, _) =>
        {
            if (!isHolding || version != holdVersion || !IsEnabled) return;
            isHolding = false;
            confirmed = true;
            try { OnClick(); } finally { confirmed = false; }
        };
        BeginAnimation(HoldProgressProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private void CancelHold()
    {
        isHolding = false;
        holdVersion++;
        BeginAnimation(HoldProgressProperty, null);
        HoldProgress = 0;
    }
}
