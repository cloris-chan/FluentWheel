using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;

namespace Cloris.FluentWheel;

[VisualStudioContribution]
public sealed class ExtensionLoadTrigger : ExtensionPart, ITextViewOpenClosedListener
{
    public TextViewExtensionConfiguration TextViewExtensionConfiguration { get; } = new() { AppliesTo = [DocumentFilter.FromDocumentType("text")] };

    public Task TextViewClosedAsync(ITextViewSnapshot textView, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task TextViewOpenedAsync(ITextViewSnapshot textView, CancellationToken cancellationToken) => Task.CompletedTask;
}
