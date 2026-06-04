using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Cloris.FluentWheel;

[Export(typeof(IMouseProcessorProvider))]
[Name(nameof(MouseProcessorProvider))]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal sealed class MouseProcessorProvider : MouseProcessorBase, IMouseProcessorProvider
{
    IMouseProcessor IMouseProcessorProvider.GetAssociatedProcessor(IWpfTextView wpfTextView)
    {
        WheelEngine.RegisterView(wpfTextView);
        return this;
    }
}
