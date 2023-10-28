using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal class MouseProcessor(IWpfTextView wpfTextView) : MouseProcessorBase
{
    private readonly WheelController _wheelController = new(wpfTextView);

    public override void PreprocessMouseWheel(MouseWheelEventArgs e)
    {
        e.Handled = _wheelController.HandleWheel(e.Delta);
    }
}