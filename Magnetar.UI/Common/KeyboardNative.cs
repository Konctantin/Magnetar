using System;
using System.Runtime.InteropServices;

namespace Magnetar.UI.Common;

public static class KeyboardNative
{
    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_KEYUP   = 0x0101;

    public const int VK_SHIFT    = 0x10; // SHIFT
    public const int VK_CONTROL  = 0x11; // CTRL
    public const int VK_ALT      = 0x12; // ALT

    public const int VK_LSHIFT   = 0xA0; // LEFT SHIFT
    public const int VK_RSHIFT   = 0xA1; // RIGHT SHIFT
    public const int VK_LCONTROL = 0xA2; // LEFT CONTROL
    public const int VK_RCONTROL = 0xA3; // RIGHT CONTROL
    public const int VK_LMENU    = 0xA4; // LEFT ALT
    public const int VK_RMENU    = 0xA5; // RIGHT ALT

    [DllImport("user32.dll", SetLastError = true)]
    public static extern ushort GetKeyState(int nVirtKey);

    [DllImport("user32.dll", EntryPoint = "PostMessageA", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

    public static bool IsAltKeyDown() => IsKeyDown(VK_ALT);

    public static bool IsLeftShiftDown() => IsKeyDown(VK_SHIFT);

    public static bool IsKeyDown(int keyCode)
    {
        var state = GetKeyState(keyCode);
        return (state & 0xFF00) == 0xFF00;
    }
}
