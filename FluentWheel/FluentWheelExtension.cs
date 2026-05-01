using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;

namespace Cloris.FluentWheel;

[VisualStudioContribution]
internal sealed class FluentWheelExtension : Extension
{
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        RequiresInProcessHosting = true,
    };

    protected override async Task OnInitializedAsync(VisualStudioExtensibility extensibility, CancellationToken cancellationToken)
    {
        ExtensionDiagnostics.Initialize(ServiceProvider.GetRequiredService<TraceSource>());
        await SettingsCache.InitializeAsync(extensibility, cancellationToken);
        await WheelEngine.InitializeAsync();
        await base.OnInitializedAsync(extensibility, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (WheelEngine.IsInitialized)
            {
                WheelEngine.Cleanup();
            }

            SettingsCache.Cleanup();
            ExtensionDiagnostics.Cleanup();
        }

        base.Dispose(disposing);
    }
}
