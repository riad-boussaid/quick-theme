using QuickTheme.Models;

namespace QuickTheme.App;

internal sealed class Hotkeys : IDisposable
{
    private const int HotkeyToggleDark = 1;
    private const int HotkeyOpenPanel = 2;

    private readonly HotkeyWindow _window;
    private HotkeyGesture _toggleGesture;
    private HotkeyGesture _openGesture;

    public Hotkeys(string toggleText, string openText)
    {
        _window = new HotkeyWindow();
        _window.HotkeyPressed += OnHotkeyPressed;

        if (!TryUpdate(toggleText, openText, out _))
        {
            TryUpdate("Ctrl+Alt+D", "Ctrl+Alt+Q", out _);
        }
    }

    public event Action? ToggleDarkRequested;
    public event Action? OpenPanelRequested;

    public string ToggleGestureText => _toggleGesture.ToString();

    public string OpenGestureText => _openGesture.ToString();

    public bool TryUpdate(string toggleText, string openText, out string? error)
    {
        if (!HotkeyGesture.TryParse(toggleText, out HotkeyGesture toggleGesture))
        {
            error = "Enter a valid shortcut with Ctrl, Alt, Shift, or Win.";
            return false;
        }

        if (!HotkeyGesture.TryParse(openText, out HotkeyGesture openGesture))
        {
            error = "Enter a valid shortcut with Ctrl, Alt, Shift, or Win.";
            return false;
        }

        if (toggleGesture == openGesture)
        {
            error = "The two shortcuts must be different.";
            return false;
        }

        UnregisterCurrent();

        if (!_window.Register(HotkeyToggleDark, (uint)toggleGesture.Modifiers, toggleGesture.Key))
        {
            error = $"Could not register {toggleGesture}. It may already be in use.";
            RestorePrevious();
            return false;
        }

        if (!_window.Register(HotkeyOpenPanel, (uint)openGesture.Modifiers, openGesture.Key))
        {
            _window.Unregister(HotkeyToggleDark);
            error = $"Could not register {openGesture}. It may already be in use.";
            RestorePrevious();
            return false;
        }

        _toggleGesture = toggleGesture;
        _openGesture = openGesture;
        error = null;
        return true;
    }

    public void Dispose()
    {
        _window.HotkeyPressed -= OnHotkeyPressed;
        _window.Dispose();
    }

    private void UnregisterCurrent()
    {
        _window.Unregister(HotkeyToggleDark);
        _window.Unregister(HotkeyOpenPanel);
    }

    private void RestorePrevious()
    {
        if (_toggleGesture.Key != Keys.None)
        {
            _window.Register(HotkeyToggleDark, (uint)_toggleGesture.Modifiers, _toggleGesture.Key);
        }

        if (_openGesture.Key != Keys.None)
        {
            _window.Register(HotkeyOpenPanel, (uint)_openGesture.Modifiers, _openGesture.Key);
        }
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
