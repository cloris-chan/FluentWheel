using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Cloris.FluentWheel;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideOptionPage(typeof(OptionPage), "Fluent Wheel", "General", 0, 0, true)]
[Guid(PackageGuidString)]
public sealed class FluentWheelPackage : AsyncPackage
{
    private const string PackageGuidString = "F0A2F81C-BBEC-466B-A7FC-99A68A969B02";

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await Settings.InitializeAsync(this);
    }
}