using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Cloris.FluentWheel;

internal static class LowLevelMouseHook
{
    private static readonly HOOKPROC _hookProc = HookCallback;
    private static HookState? _state;

    public static event Action<MouseWheelInput>? MouseWheelInputReceived;

    public static bool IsEnabled => _state is not null;

    public static async Task EnableAsync()
    {
        await Task.Yield();

        using var started = new ManualResetEventSlim();
        Exception? startupException = null;
        HookState state;

        lock (HookState.LockObject)
        {
            if (_state is not null)
            {
                return;
            }

            state = new();
            var thread = new Thread(ThreadMain)
            {
                IsBackground = true,
                Name = nameof(LowLevelMouseHook),
            };
            state.Thread = thread;
            thread.Start();
            started.Wait();

            if (startupException is not null)
            {
                throw startupException;
            }

            _state = state;
        }

        void ThreadMain()
        {
            state.ThreadId = PInvoke.GetCurrentThreadId();

            try
            {
                state.HookHandle = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_MOUSE_LL, _hookProc, PInvoke.GetModuleHandle(default(PCWSTR)), 0);
                if (state.HookHandle.IsNull)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch (Exception ex)
            {
                startupException = ex;
                return;
            }
            finally
            {
                started.Set();
            }

            try
            {
                while (PInvoke.GetMessage(out _, HWND.Null, 0, 0).Value > 0)
                {
                }
            }
            finally
            {
                _ = Task.Run(DisableAsync);
            }
        }
    }

    public static async Task DisableAsync()
    {
        await Task.Yield();


        lock (HookState.LockObject)
        {
            if (_state is null)
            {
                return;
            }

            if (_state.ThreadId != 0)
            {
                PInvoke.PostThreadMessage(_state.ThreadId, PInvoke.WM_QUIT, 0, 0);
            }

            _state.Thread?.Join();
            _state = null;
        }
    }

    private static LRESULT HookCallback(int code, WPARAM wParam, LPARAM lParam)
    {
        if (code == (int)PInvoke.HC_ACTION)
        {
            var message = unchecked((uint)wParam.Value);
            if (message is PInvoke.WM_MOUSEWHEEL or PInvoke.WM_MOUSEHWHEEL)
            {
                var mouseData = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam.Value).mouseData;
                var delta = (short)(mouseData >> 16);

                if (delta != 0)
                {
                    MouseWheelInputReceived?.Invoke(new()
                    {
                        Delta = delta,
                        IsHorizontal = message == PInvoke.WM_MOUSEHWHEEL,
                        IsShiftPressed = IsKeyPressed(VIRTUAL_KEY.VK_SHIFT),
                        IsControlPressed = IsKeyPressed(VIRTUAL_KEY.VK_CONTROL),
                        IsAltPressed = IsKeyPressed(VIRTUAL_KEY.VK_MENU)
                    });
                }
            }
        }

        return PInvoke.CallNextHookEx(_state?.HookHandle ?? default, code, wParam, lParam);
    }

    private static bool IsKeyPressed(VIRTUAL_KEY virtualKey)
    {
        return (PInvoke.GetAsyncKeyState((int)virtualKey) & 0x8000) != 0;
    }

    public struct MouseWheelInput
    {
        public short Delta;
        public bool IsHorizontal;
        public bool IsShiftPressed;
        public bool IsControlPressed;
        public bool IsAltPressed;
    }

    private sealed class HookState
    {
        public static readonly object LockObject = new();

        public Thread? Thread;

        public HHOOK HookHandle;

        public uint ThreadId;
    }
}
