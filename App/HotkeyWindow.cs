using System.Runtime.InteropServices;

namespace QuickTheme.App;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;
    private static readonly IntPtr HwndMessage = new(-3);

    private readonly List<int> _registered = new();

    public event Action<int>? HotkeyPressed;

    public HotkeyWindow()
    {
        var parameters = new CreateParams
        {
            Caption = "QuickTheme.Hotkeys",
            Parent = HwndMessage,
        };

        CreateHandle(parameters);
    }

    public bool Register(int id, uint modifiers, Keys key)
    {
        if (!RegisterHotKey(Handle, id, modifiers | ModNoRepeat, (uint)key))
        {
            return false;
        }

        _registered.Add(id);
        return true;
    }

    public void Unregister(int id)
    {
        UnregisterHotKey(Handle, id);
        _registered.Remove(id);
    }

    public void Dispose()
    {
        foreach (int id in _registered)
        {
            UnregisterHotKey(Handle, id);
        }

        _registered.Clear();

        if (Handle != IntPtr.Zero)
        {
            DestroyHandle();
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey)
        {
            HotkeyPressed?.Invoke(m.WParam.ToInt32());
            return;
        }

        base.WndProc(ref m);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
