// Polyfill for C# 9 init-only setters when targeting .NET Framework 4.x
// Enables generated code that uses 'init' accessors to compile.
// Reference: https://github.com/dotnet/roslyn/issues/45510
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    internal static class IsExternalInit { }
}
