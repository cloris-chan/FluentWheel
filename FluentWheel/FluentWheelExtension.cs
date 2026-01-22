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

    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSettingsObservers();
        base.InitializeServices(serviceCollection);
    }

    protected override async Task OnInitializedAsync(VisualStudioExtensibility extensibility, CancellationToken cancellationToken)
    {
        await SettingsCache.InitializeAsync(extensibility, ServiceProvider, cancellationToken);
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

            if (SettingsCache.IsInitialized)
            {
                SettingsCache.Cleanup();
            }
        }

        base.Dispose(disposing);
    }
}
