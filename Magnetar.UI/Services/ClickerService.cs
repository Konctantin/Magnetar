using Magnetar.UI.Common;

namespace Magnetar.UI.Services;

public class ClickerService
{
    readonly Random random = new();

    bool started = false;

    public event EventHandler<string>? Clicked;

    public void Click(IntPtr hwd, KeyRecord key)
    {
        if (started)
            return;

        if (KeyboardNative.IsAltKeyDown())
            return;

        started = true;
        Task.Factory.StartNew(() => {
            SendKey(hwd, key);
            started = false;
        });
    }

    private void RandomSleep(int min, int max)
    {
        Thread.Sleep(random.Next(min, max));
    }

    private void SendKey(IntPtr hwd, KeyRecord keyRec)
    {
        var modPressed = false;
        var isModDown = KeyboardNative.IsKeyDown(keyRec.Modifier);

        Clicked?.Invoke(this, $" >>>> Begin Click");

        if (keyRec.HasModif && !isModDown)
        {
            Clicked?.Invoke(this, $"Modifier Down: 0x{keyRec.Modifier:X}");
            KeyboardNative.PostMessage(hwd, KeyboardNative.WM_KEYDOWN, keyRec.Modifier, 0);
            RandomSleep(10, 30);
            modPressed = true;
        }

        Clicked?.Invoke(this, $"Key Down: 0x{keyRec.Key:X}");
        KeyboardNative.PostMessage(hwd, KeyboardNative.WM_KEYDOWN, keyRec.Key, 0);
        RandomSleep(30, 60);

        Clicked?.Invoke(this, $"Key Up: 0x{keyRec.Key:X}");
        KeyboardNative.PostMessage(hwd, KeyboardNative.WM_KEYUP, keyRec.Key, 0);

        if (keyRec.HasModif && modPressed)
        {
            Clicked?.Invoke(this, $"Modifier Up: 0x{keyRec.Modifier:X}");
            RandomSleep(10, 30);
            KeyboardNative.PostMessage(hwd, KeyboardNative.WM_KEYUP, keyRec.Modifier, 0);
        }
    }
}
