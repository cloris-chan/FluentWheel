// Polyfill for System.Diagnostics.CodeAnalysis.ExperimentalAttribute
// Needed when targeting .NET Framework and using code generators that emit this attribute.
// See: https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Diagnostics.CodeAnalysis
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class ExperimentalAttribute(string diagnosticId) : Attribute
    {
        public string DiagnosticId { get; } = diagnosticId;

        public string? UrlFormat { get; set; }
    }
}
