using System.Runtime.InteropServices;

namespace Cloris.FluentWheel;

internal static class NativeMethods
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "ScreenToClient", ExactSpelling = true, SetLastError = true)]
    public static extern int ScreenToClient(HandleRef hWnd, [In][Out] ref POINT pt);

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;

        public int Y;
    }
}