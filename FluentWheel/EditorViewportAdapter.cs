using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal enum ViewportApplyResult
{
    NotReady,
    Applied,
    Failed,
}

internal sealed class EditorViewportAdapter(IWpfTextView view)
{
    public IViewScroller NativeScroller { get; } = view.ViewScroller;

    public bool CanApply
    {
        get
        {
            try
            {
                return view is { IsClosed: false, InLayout: false } && !view.GetInOuterLayout();
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    public ViewportApplyResult TryScrollVertically(double distance)
    {
        if (!CanApply)
        {
            return ViewportApplyResult.NotReady;
        }

        try
        {
            if (distance != 0)
            {
                NativeScroller.ScrollViewportVerticallyByPixels(distance);
            }

            return ViewportApplyResult.Applied;
        }
        catch (Exception ex)
        {
            ExtensionDiagnostics.TraceException(nameof(TryScrollVertically), ex);
            return ViewportApplyResult.Failed;
        }
    }

    public ViewportApplyResult TryScrollHorizontally(double distance)
    {
        if (!CanApply)
        {
            return ViewportApplyResult.NotReady;
        }

        try
        {
            if (distance != 0)
            {
                NativeScroller.ScrollViewportHorizontallyByPixels(distance);
            }

            return ViewportApplyResult.Applied;
        }
        catch (Exception ex)
        {
            ExtensionDiagnostics.TraceException(nameof(TryScrollHorizontally), ex);
            return ViewportApplyResult.Failed;
        }
    }

    public ViewportApplyResult TrySetZoomLevel(double zoomLevel)
    {
        if (!CanApply)
        {
            return ViewportApplyResult.NotReady;
        }

        try
        {
            view.Options.GlobalOptions.SetOptionValue(DefaultWpfViewOptions.ZoomLevelId, zoomLevel);
            return ViewportApplyResult.Applied;
        }
        catch (Exception ex)
        {
            ExtensionDiagnostics.TraceException(nameof(TrySetZoomLevel), ex);
            return ViewportApplyResult.Failed;
        }
    }
}
