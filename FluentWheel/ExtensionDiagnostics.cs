using System.Diagnostics;

namespace Cloris.FluentWheel;

internal static class ExtensionDiagnostics
{
    private static TraceSource? _traceSource;

    public static void Initialize(TraceSource traceSource)
    {
        _traceSource = traceSource;
        _traceSource.TraceEvent(TraceEventType.Information, 0, "Extension diagnostics initialized.");
    }

    public static void Cleanup()
    {
        _traceSource = null;
    }

    public static void TraceException(string context, Exception ex)
    {
        _traceSource?.TraceEvent(TraceEventType.Error, 0, "[{0}] {1}", context, ex);
    }
}
