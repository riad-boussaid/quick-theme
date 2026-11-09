using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using QuickTheme.Models;
using QuickTheme.Services;
using QuickTheme.UI.Controls;

namespace QuickTheme.UI;

internal sealed class SettingsForm : Form
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    private readonly Action _scheduleChanged;
    private readonly Action _checkForUpdates;
    private readonly Func<string, string, string?> _applyHotkeys;
    private readonly Panel _content;

    private ThemeRow _scheduleEnabledRow = null!;
    private ThemeRow _schedulePausedRow = null!;
    private SegmentedControl _modeControl = null!;
    private NumericUpDown _lightHour = null!;
    private NumericUpDown _lightMinute = null!;
    private NumericUpDown _darkHour = null!;
    private NumericUpDown _darkMinute = null!;
    private NumericUpDown _latitude = null!;
    private NumericUpDown _longitude = null!;
    private SegmentedControl _scopeControl = null!;
    private SegmentedControl _daysControl = null!;
    private Label _scheduleStatus = null!;
    private ThemeRow _startWithWindowsRow = null!;
    private ThemeRow _startupUpdatesRow = null!;
    private TextBox _toggleHotkeyBox = null!;
    private TextBox _openHotkeyBox = null!;
    private Label _hotkeyStatus = null!;
    private TextBox _feedBox = null!;
    private Label _feedStatus = null!;
    private int _y = 12;
    private bool _loading = true;

    public SettingsForm(
        Action scheduleChanged,
        Action checkForUpdates,
        Func<string, string, string?> applyHotkeys)
    {
        _scheduleChanged = scheduleChanged;
        _checkForUpdates = checkForUpdates;
        _applyHotkeys = applyHotkeys;

        Palette.Apply(SettingsService.Current.FlyoutTheme, ThemeService.SystemUsesDarkTheme);

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Quick Theme Settings";
        Icon = Palette.IsLight
            ? Properties.Resources.themes_256_black
            : new Icon(Properties.Resources.themes_32, 32, 32);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 640);
        BackColor = Palette.SurfaceBottom;
        ForeColor = Palette.Text;
        Font = new Font("Segoe UI", 9F);

        _content = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Palette.SurfaceBottom,
        };
        Controls.Add(_content);

        BuildScheduleSection();
        BuildGeneralSection();
        BuildHotkeysSection();
        BuildUpdatesSection();
        BuildAboutSection();

        LoadSettings();
        _loading = false;

        _scheduleEnabledRow.CheckedChanged += OnScheduleEnabledChanged;
        _schedulePausedRow.CheckedChanged += OnSchedulePausedChanged;
        _modeControl.SelectedIndexChanged += OnScheduleModeChanged;
        _scopeControl.SelectedIndexChanged += OnScheduleOptionChanged;
        _daysControl.SelectedIndexChanged += OnScheduleOptionChanged;
        _lightHour.ValueChanged += OnScheduleTimeChanged;
        _lightMinute.ValueChanged += OnScheduleTimeChanged;
        _darkHour.ValueChanged += OnScheduleTimeChanged;
        _darkMinute.ValueChanged += OnScheduleTimeChanged;
        _latitude.ValueChanged += OnScheduleLocationChanged;
        _longitude.ValueChanged += OnScheduleLocationChanged;
        _startWithWindowsRow.CheckedChanged += OnStartWithWindowsChanged;
        _startupUpdatesRow.CheckedChanged += OnStartupUpdatesChanged;

        UpdateScheduleStatus();
    }

    public void RefreshFromSettings()
    {
        AppSettings settings = SettingsService.Current;
        _loading = true;
        try
        {
            _scheduleEnabledRow.SetCheckedSilently(settings.ScheduleEnabled);
            _schedulePausedRow.SetCheckedSilently(settings.SchedulePaused);
            _modeControl.SelectedIndex = (int)settings.ScheduleMode;
            _scopeControl.SelectedIndex = (int)settings.ScheduleScope;
            _daysControl.SelectedIndex = (int)settings.ScheduleDays;
            _lightHour.Value = settings.ScheduleLightTime.Hour;
            _lightMinute.Value = settings.ScheduleLightTime.Minute;
            _darkHour.Value = settings.ScheduleDarkTime.Hour;
            _darkMinute.Value = settings.ScheduleDarkTime.Minute;
            _latitude.Value = (decimal)Math.Clamp(settings.ScheduleLatitude, -90d, 90d);
            _longitude.Value = (decimal)Math.Clamp(settings.ScheduleLongitude, -180d, 180d);
            _toggleHotkeyBox.Text = settings.HotkeyToggleDark;
            _openHotkeyBox.Text = settings.HotkeyOpenPanel;
        }
        finally
        {
            _loading = false;
        }

        UpdateScheduleStatus();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (!Palette.IsLight)
        {
            int useDark = 1;
            _ = DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int));
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (!Palette.IsLight)
        {
            _ = SetWindowTheme(_content.Handle, "DarkMode_Explorer", null);
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        Rectangle area = Screen.FromControl(this).WorkingArea;
        int newWidth = Math.Min(ClientSize.Width, Math.Max(400, area.Width - 120));
        int newHeight = Math.Min(ClientSize.Height, Math.Max(400, area.Height - 120));

        if (newWidth != ClientSize.Width || newHeight != ClientSize.Height)
        {
            ClientSize = new Size(newWidth, newHeight);
            Left = area.Left + (area.Width - Width) / 2;
            Top = area.Top + (area.Height - Height) / 2;
        }
    }

    private void BuildScheduleSection()
    {
        AddSection("Schedule");

        _scheduleEnabledRow = AddToggleRow(
            "Enable schedule",
            "Automatically switch themes on a daily schedule");

        _modeControl = new SegmentedControl
        {
            Options = ["Fixed times", "Sunrise/sunset"],
        };
        AddChoiceRow("Mode", _modeControl);

        (_lightHour, _lightMinute) = AddTimeRow("Light time");
        (_darkHour, _darkMinute) = AddTimeRow("Dark time");

        AddLabel("Latitude", new Rectangle(20, _y + 3, 160, 26), Palette.Text, 9F);
        _latitude = CreateCoordinate(-90, 90, new Point(200, _y));
        _y += 38;

        AddLabel("Longitude", new Rectangle(20, _y + 3, 160, 26), Palette.Text, 9F);
        _longitude = CreateCoordinate(-180, 180, new Point(200, _y));
        _y += 38;

        AddLabel(
            "Sunrise/sunset uses your location and local time; fixed times are fallback.",
            new Rectangle(20, _y, 520, 22),
            Palette.TextMuted,
            8.5F);
        _y += 28;

        _scopeControl = new SegmentedControl
        {
            Options = ["Apps + system", "Apps", "System"],
        };
        AddChoiceRow("Apply to", _scopeControl);

        _daysControl = new SegmentedControl
        {
            Options = ["Every day", "Weekdays", "Weekends"],
        };
        AddChoiceRow("Days", _daysControl);

        _schedulePausedRow = AddToggleRow(
            "Pause schedule",
            "Suspend automatic switching until resumed");

        _scheduleStatus = AddLabel(string.Empty, new Rectangle(20, _y, 520, 24), Palette.TextMuted, 8.5F);
        _y += 30;
    }

    private void BuildGeneralSection()
    {
        AddSection("General");

        _startWithWindowsRow = AddToggleRow(
            "Start with Windows",
            "Launch Quick Theme when you sign in");

        _startupUpdatesRow = AddToggleRow(
            "Check for updates on startup",
            "At most once every 24 hours");

        AddLabel(
            "Flyout theme and quick presets are available from the tray menu.",
            new Rectangle(20, _y, 520, 24),
            Palette.TextMuted,
            8.5F);
        _y += 34;
    }

    private void BuildHotkeysSection()
    {
        AddSection("Hotkeys");

        AddLabel("Toggle dark mode", new Rectangle(20, _y + 3, 160, 26), Palette.Text, 9F);
        _toggleHotkeyBox = CreateHotkeyBox(new Rectangle(200, _y, 190, 28));
        _y += 38;

        AddLabel("Open flyout", new Rectangle(20, _y + 3, 160, 26), Palette.Text, 9F);
        _openHotkeyBox = CreateHotkeyBox(new Rectangle(200, _y, 190, 28));
        _y += 38;

        Button applyButton = CreateButton("Apply hotkeys", 110);
        applyButton.Location = new Point(200, _y);
        applyButton.Click += (_, _) => ApplyHotkeyChanges();
        _content.Controls.Add(applyButton);

        _hotkeyStatus = AddLabel(
            string.Empty,
            new Rectangle(320, _y, 220, 28),
            Palette.TextMuted,
            8.5F);
        _y += 38;

        AddLabel(
            "Click a box and press a combination including Ctrl, Alt, or Shift.",
            new Rectangle(20, _y, 520, 22),
            Palette.TextMuted,
            8.5F);
        _y += 34;
    }

    private void BuildUpdatesSection()
    {
        AddSection("Updates");

        AddLabel("Update feed URL", new Rectangle(20, _y, 520, 24), Palette.Text, 9F);
        _y += 24;

        _feedBox = new TextBox
        {
            Bounds = new Rectangle(20, _y, 340, 28),
            BackColor = Palette.MenuSurface,
            ForeColor = Palette.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F),
        };
        _content.Controls.Add(_feedBox);

        Button saveFeedButton = CreateButton("Save", 70);
        saveFeedButton.Location = new Point(368, _y);
        saveFeedButton.Click += (_, _) => SaveFeed();
        _content.Controls.Add(saveFeedButton);

        Button checkButton = CreateButton("Check now", 94);
        checkButton.Location = new Point(446, _y);
        checkButton.Click += (_, _) =>
        {
            SaveFeed();
            _checkForUpdates();
        };
        _content.Controls.Add(checkButton);

        _y += 36;

        _feedStatus = AddLabel(string.Empty, new Rectangle(20, _y, 520, 22), Palette.TextMuted, 8.5F);
        _y += 26;

        AddLabel(
            $"Settings file: {SettingsService.SettingsFilePath}",
            new Rectangle(20, _y, 520, 22),
            Palette.TextMuted,
            8F);
        _y += 34;
    }

    private void BuildAboutSection()
    {
        AddSection("About");

        AddLabel(
            $"Quick Theme {AppInfo.DisplayVersion}",
            new Rectangle(20, _y, 520, 24),
            Palette.Text,
            9.5F,
            FontStyle.Bold);
        _y += 24;

        AddLabel(
            "MIT License · open source · github.com/riad-boussaid/quick-theme",
            new Rectangle(20, _y, 520, 22),
            Palette.TextMuted,
            8.5F);
        _y += 30;

        Button githubButton = CreateButton("GitHub", 80);
        githubButton.Location = new Point(20, _y);
        githubButton.Click += (_, _) => OpenUrl("https://github.com/riad-boussaid/quick-theme");
        _content.Controls.Add(githubButton);

        Button releasesButton = CreateButton("Releases", 80);
        releasesButton.Location = new Point(108, _y);
        releasesButton.Click += (_, _) => OpenUrl("https://github.com/riad-boussaid/quick-theme/releases/latest");
        _content.Controls.Add(releasesButton);

        Button licenseButton = CreateButton("License", 80);
        licenseButton.Location = new Point(196, _y);
        licenseButton.Click += (_, _) => OpenUrl("https://github.com/riad-boussaid/quick-theme/blob/main/LICENSE");
        _content.Controls.Add(licenseButton);

        _y += 38;
    }

    private void LoadSettings()
    {
        AppSettings settings = SettingsService.Current;

        _scheduleEnabledRow.SetCheckedSilently(settings.ScheduleEnabled);
        _schedulePausedRow.SetCheckedSilently(settings.SchedulePaused);
        _modeControl.SelectedIndex = (int)settings.ScheduleMode;
        _lightHour.Value = settings.ScheduleLightTime.Hour;
        _lightMinute.Value = settings.ScheduleLightTime.Minute;
        _darkHour.Value = settings.ScheduleDarkTime.Hour;
        _darkMinute.Value = settings.ScheduleDarkTime.Minute;
        _latitude.Value = (decimal)Math.Clamp(settings.ScheduleLatitude, -90d, 90d);
        _longitude.Value = (decimal)Math.Clamp(settings.ScheduleLongitude, -180d, 180d);
        _scopeControl.SelectedIndex = (int)settings.ScheduleScope;
        _daysControl.SelectedIndex = (int)settings.ScheduleDays;
        _startWithWindowsRow.SetCheckedSilently(StartupService.IsEnabled);
        _startupUpdatesRow.SetCheckedSilently(settings.CheckForUpdatesOnStartup);
        _toggleHotkeyBox.Text = settings.HotkeyToggleDark;
        _openHotkeyBox.Text = settings.HotkeyOpenPanel;
        _feedBox.Text = settings.UpdateFeedUrl;
    }

    private void AddSection(string title)
    {
        _y += 6;

        var header = new Label
        {
            Text = title,
            AutoSize = false,
            Bounds = new Rectangle(20, _y, 520, 26),
            ForeColor = Palette.Text,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 12F),
        };
        _content.Controls.Add(header);
        _y += 30;

        var separator = new Panel
        {
            Bounds = new Rectangle(20, _y, 520, 1),
            BackColor = Palette.Separator,
        };
        _content.Controls.Add(separator);
        _y += 10;
    }

    private ThemeRow AddToggleRow(string title, string subtitle)
    {
        var row = new ThemeRow(title, subtitle, "On", "Off")
        {
            Bounds = new Rectangle(16, _y, 528, 56),
        };
        _content.Controls.Add(row);
        _y += 56;
        return row;
    }

    private (NumericUpDown Hour, NumericUpDown Minute) AddTimeRow(string label)
    {
        AddLabel(label, new Rectangle(20, _y + 3, 160, 26), Palette.Text, 9F);

        NumericUpDown hour = CreateNumeric(0, 23, new Point(200, _y));
        AddLabel(":", new Rectangle(258, _y, 10, 28), Palette.TextMuted, 10F);
        NumericUpDown minute = CreateNumeric(0, 59, new Point(274, _y));

        _y += 38;
        return (hour, minute);
    }

    private void AddChoiceRow(string label, SegmentedControl control)
    {
        AddLabel(label, new Rectangle(20, _y + 3, 160, 26), Palette.Text, 9F);
        control.Bounds = new Rectangle(200, _y, 330, 30);
        _content.Controls.Add(control);
        _y += 40;
    }

    private NumericUpDown CreateNumeric(int minimum, int maximum, Point location)
    {
        var control = new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            Bounds = new Rectangle(location.X, location.Y, 56, 28),
            BackColor = Palette.MenuSurface,
            ForeColor = Palette.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10F),
            TextAlign = HorizontalAlignment.Right,
        };
        _content.Controls.Add(control);
        return control;
    }

    private NumericUpDown CreateCoordinate(int minimum, int maximum, Point location)
    {
        var control = new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            DecimalPlaces = 4,
            Increment = 0.1m,
            Bounds = new Rectangle(location.X, location.Y, 120, 28),
            BackColor = Palette.MenuSurface,
            ForeColor = Palette.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F),
            TextAlign = HorizontalAlignment.Right,
        };
        _content.Controls.Add(control);
        return control;
    }

    private TextBox CreateHotkeyBox(Rectangle bounds)
    {
        var control = new TextBox
        {
            Bounds = bounds,
            ReadOnly = true,
            BackColor = Palette.MenuSurface,
            ForeColor = Palette.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand,
        };
        control.KeyDown += OnHotkeyBoxKeyDown;
        _content.Controls.Add(control);
        return control;
    }

    private Label AddLabel(
        string text,
        Rectangle bounds,
        Color color,
        float size = 9F,
        FontStyle style = FontStyle.Regular)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = false,
            Bounds = bounds,
            ForeColor = color,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", size, style),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _content.Controls.Add(label);
        return label;
    }

    private Button CreateButton(string text, int width)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Width = width,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Palette.MenuHover,
            ForeColor = Palette.Text,
            Font = new Font("Segoe UI", 8.75F),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };

        button.FlatAppearance.BorderColor = Palette.Border;
        button.FlatAppearance.MouseOverBackColor = Palette.RowHover;
        button.FlatAppearance.MouseDownBackColor = Palette.Border;
        return button;
    }

    private void OnScheduleEnabledChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        SettingsService.Current.ScheduleEnabled = _scheduleEnabledRow.Checked;
        SaveAndNotifySchedule();
    }

    private void OnSchedulePausedChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        SettingsService.Current.SchedulePaused = _schedulePausedRow.Checked;
        SaveAndNotifySchedule();
    }

    private void OnScheduleOptionChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        SettingsService.Current.ScheduleScope = (ScheduleScope)_scopeControl.SelectedIndex;
        SettingsService.Current.ScheduleDays = (ScheduleDays)_daysControl.SelectedIndex;
        SaveAndNotifySchedule();
    }

    private void OnScheduleModeChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        SettingsService.Current.ScheduleMode = (ScheduleMode)_modeControl.SelectedIndex;
        SaveAndNotifySchedule();
    }

    private void OnScheduleLocationChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        SettingsService.Current.ScheduleLatitude = (double)_latitude.Value;
        SettingsService.Current.ScheduleLongitude = (double)_longitude.Value;
        SaveAndNotifySchedule();
    }

    private void OnScheduleTimeChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        SettingsService.Current.ScheduleLightTime = new TimeOnly(
            (int)_lightHour.Value,
            (int)_lightMinute.Value);
        SettingsService.Current.ScheduleDarkTime = new TimeOnly(
            (int)_darkHour.Value,
            (int)_darkMinute.Value);
        SaveAndNotifySchedule();
    }

    private void OnStartWithWindowsChanged(object? sender, EventArgs e)
    {
        if (!_loading)
        {
            StartupService.SetEnabled(_startWithWindowsRow.Checked);
        }
    }

    private void OnStartupUpdatesChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        SettingsService.Current.CheckForUpdatesOnStartup = _startupUpdatesRow.Checked;
        SettingsService.Save();
    }

    private void SaveFeed()
    {
        SettingsService.Current.UpdateFeedUrl = _feedBox.Text.Trim();
        SettingsService.Save();
        _feedStatus.Text = "Saved.";
    }

    private void ApplyHotkeyChanges()
    {
        string? error = _applyHotkeys(_toggleHotkeyBox.Text, _openHotkeyBox.Text);

        if (error is not null)
        {
            _hotkeyStatus.ForeColor = Palette.Danger;
            _hotkeyStatus.Text = error;
            return;
        }

        _toggleHotkeyBox.Text = SettingsService.Current.HotkeyToggleDark;
        _openHotkeyBox.Text = SettingsService.Current.HotkeyOpenPanel;
        _hotkeyStatus.ForeColor = Palette.TextMuted;
        _hotkeyStatus.Text = "Hotkeys applied.";
    }

    private void OnHotkeyBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox box || e.KeyCode is Keys.Tab or Keys.Escape)
        {
            return;
        }

        e.SuppressKeyPress = true;
        e.Handled = true;

        if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin or Keys.None)
        {
            return;
        }

        HotkeyModifiers modifiers = HotkeyModifiers.None;
        if (e.Control)
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (e.Alt)
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (e.Shift)
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (modifiers == HotkeyModifiers.None)
        {
            _hotkeyStatus.ForeColor = Palette.Danger;
            _hotkeyStatus.Text = "Include Ctrl, Alt, or Shift.";
            return;
        }

        box.Text = new HotkeyGesture(modifiers, e.KeyCode).ToString();
        _hotkeyStatus.ForeColor = Palette.TextMuted;
        _hotkeyStatus.Text = "Press Apply hotkeys to confirm.";
    }

    private void SaveAndNotifySchedule()
    {
        SettingsService.Save();
        UpdateScheduleStatus();
        _scheduleChanged();
    }

    private void UpdateScheduleStatus()
    {
        AppSettings settings = SettingsService.Current;

        if (settings.ScheduleMode == ScheduleMode.FixedTimes &&
            settings.ScheduleLightTime == settings.ScheduleDarkTime)
        {
            _scheduleStatus.ForeColor = Palette.Danger;
            _scheduleStatus.Text = "Light and dark times must differ.";
            return;
        }

        _scheduleStatus.ForeColor = Palette.TextMuted;

        if (!settings.ScheduleEnabled)
        {
            _scheduleStatus.Text = "Schedule is off.";
            return;
        }

        if (settings.SchedulePaused)
        {
            _scheduleStatus.Text = "Schedule is paused.";
            return;
        }

        ScheduleTransition? next = ScheduleCalculator.GetNextTransition(settings, DateTime.Now);
        _scheduleStatus.Text = next is null
            ? "No upcoming switch."
            : $"Next switch: {next.Value.At:ddd HH:mm} → {(next.Value.Dark ? "Dark" : "Light")}";
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Best effort.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);
}
