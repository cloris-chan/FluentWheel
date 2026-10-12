using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Shell;

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
        ShutdownToken.Register(static () => ThreadHelper.JoinableTaskFactory.Run(CleanupAsync));

        ExtensionDiagnostics.Initialize(ServiceProvider.GetRequiredService<TraceSource>());
        await SettingsCache.InitializeAsync(extensibility, cancellationToken);
        await WheelEngine.InitializeAsync();
        await base.OnInitializedAsync(extensibility, cancellationToken);
    }

    private static async Task CleanupAsync()
    {
        if (WheelEngine.IsInitialized)
        {
            await WheelEngine.CleanupAsync();
        }

        SettingsCache.Cleanup();
        ExtensionDiagnostics.Cleanup();
    }
}
