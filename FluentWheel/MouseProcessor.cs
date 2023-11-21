using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal class MouseProcessor(IWpfTextView wpfTextView) : MouseProcessorBase
{
    private readonly WheelController _wheelController = new(wpfTextView);
}