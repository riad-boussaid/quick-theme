namespace QuickTheme.App;

internal sealed class Hotkeys : IDisposable
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const int HotkeyToggleDark = 1;
    private const int HotkeyOpenPanel = 2;

    private readonly HotkeyWindow _window;

    public Hotkeys()
    {
        _window = new HotkeyWindow();
        _window.HotkeyPressed += OnHotkeyPressed;

        _window.Register(HotkeyToggleDark, ModControl | ModAlt, Keys.D);
        _window.Register(HotkeyOpenPanel, ModControl | ModAlt, Keys.Q);
    }

    public event Action? ToggleDarkRequested;
    public event Action? OpenPanelRequested;

    public void Dispose()
    {
        _window.HotkeyPressed -= OnHotkeyPressed;
        _window.Dispose();
    }

    private void OnHotkeyPressed(int id)
    {
        switch (id)
        {
            case HotkeyToggleDark:
                ToggleDarkRequested?.Invoke();
                break;
            case HotkeyOpenPanel:
                OpenPanelRequested?.Invoke();
                break;
        }
    }
}
