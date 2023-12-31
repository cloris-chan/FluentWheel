﻿using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Cloris.FluentWheel;

[Export(typeof(IMouseProcessorProvider))]
[Name(nameof(MouseProcessorProvider))]
[ContentType("Text")]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal class MouseProcessorProvider : IMouseProcessorProvider
{
    public IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView) => new MouseProcessor(wpfTextView);
}