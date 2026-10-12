using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal sealed class TextViewAnimationState : IDisposable
{
    private bool _disposed;

    public IWpfTextView View { get; }

    public ViewScroller ViewScroller { get; }

    public EditorViewportAdapter ViewportAdapter { get; }

    public IWpfTextViewHost? Host { get; private set; }

    public ScrollAnimation HorizontalScrollAnimation { get; } = new();

    public ScrollAnimation VerticalScrollAnimation { get; } = new();

    public ZoomAnimation ZoomAnimation { get; } = new();

    public bool HasPendingUpdates => HorizontalScrollAnimation.HasPendingUpdate || VerticalScrollAnimation.HasPendingUpdate || ZoomAnimation.HasPendingUpdate;

    public bool CanQueueAnimation => !_disposed && !View.IsClosed;

    public bool CanZoom { get; private set; }

    public TextViewAnimationState(IWpfTextView wpfTextView)
    {
        const string ZoomControlMarginFullName = "Microsoft.VisualStudio.Text.Editor.Implementation.ZoomControlMargin";

        View = wpfTextView;
        ViewportAdapter = new EditorViewportAdapter(View);
        ViewScroller = new ViewScroller(this);
        Host = FindHost(View.VisualElement);
        CanZoom = Host?.HostControl.FindDescendants<ComboBox>().OfType<IWpfTextViewMargin>().Any(item => item.GetType().FullName is ZoomControlMarginFullName) is true;
    }

    public void ApplyPendingAnimations()
    {
        if (!CanQueueAnimation)
        {
            return;
        }

        ApplyScroll(VerticalScrollAnimation, isHorizontal: false);
        ApplyScroll(HorizontalScrollAnimation, isHorizontal: true);

        if (ZoomAnimation.HasPendingUpdate && ViewportAdapter.CanApply)
        {
            try
            {
                var value = ZoomAnimation.SampleValue();
                switch (ViewportAdapter.TrySetZoomLevel(value))
                {
                    case ViewportApplyResult.Applied:
                        ZoomAnimation.MarkApplied(value);
                        break;
                    case ViewportApplyResult.Failed:
                        ZoomAnimation.CancelPendingUpdate();
                        break;
                }
            }
            catch (Exception ex)
            {
                ZoomAnimation.CancelPendingUpdate();
                ExtensionDiagnostics.TraceException("TextViewAnimationState.ApplyZoom", ex);
            }
        }
    }

    private void ApplyScroll(ScrollAnimation animation, bool isHorizontal)
    {
        if (!animation.HasPendingUpdate)
        {
            return;
        }

        try
        {
            if (!ViewportAdapter.CanApply)
            {
                return;
            }

            var value = animation.SampleValue();
            var distance = value - animation.AppliedValue;
            var result = isHorizontal ? ViewportAdapter.TryScrollHorizontally(distance) : ViewportAdapter.TryScrollVertically(distance);
            switch (result)
            {
                case ViewportApplyResult.Applied:
                    animation.MarkApplied(value);
                    break;
                case ViewportApplyResult.Failed:
                    animation.CancelPendingUpdate();
                    break;
            }
        }
        catch (Exception ex)
        {
            animation.CancelPendingUpdate();
            ExtensionDiagnostics.TraceException("TextViewAnimationState.ApplyScroll", ex);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            HorizontalScrollAnimation.Dispose();
            VerticalScrollAnimation.Dispose();
            ZoomAnimation.Dispose();
        }
    }

    private static IWpfTextViewHost? FindHost(DependencyObject element) => element switch
    {
        IWpfTextViewHost host => host,
        FrameworkElement fe => FindHost(fe.Parent),
        _ => null
    };
}
