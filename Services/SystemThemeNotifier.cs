using System.Runtime.InteropServices;

namespace QuickTheme.Services;

internal static class SystemThemeNotifier
{
    private const IntPtr HwndBroadcast = (IntPtr)0xFFFF;
    private const uint WmSettingChange = 0x001A;
    private const uint SmtoAbortIfHung = 0x0002;

    public static void NotifyThemeChanged()
    {
        // Best effort, off the UI thread: let listening apps refresh without restarting
        // Explorer. This must never block the flyout.
        ThreadPool.QueueUserWorkItem(static _ => NotifyThemeChangedNow());
    }

    public static void NotifyThemeChangedNow()
    {
        NotifySettingChanged("ImmersiveColorSet");
    }

    private static void NotifySettingChanged(string setting)
    {
        _ = SendMessageTimeout(
            HwndBroadcast,
            WmSettingChange,
            IntPtr.Zero,
            setting,
            SmtoAbortIfHung,
            timeout: 250,
            out _);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        string lParam,
        uint fuFlags,
        uint timeout,
        out IntPtr result);
}
