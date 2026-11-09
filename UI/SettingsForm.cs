using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using QuickTheme.Models;
using QuickTheme.Services;
using QuickTheme.UI.Controls;

namespace QuickTheme.UI;

internal sealed class SettingsForm : Form
{
    private const int CsDropShadow = 0x00020000;
    private const int WmNcLButtonDown = 0xA1;
    private const int HtCaption = 0x2;

    private readonly Action _scheduleChanged;
    private readonly Action _checkForUpdates;
    private readonly Func<string, string, string?> _applyHotkeys;

    private readonly Panel _header;
    private readonly PictureBox _logo;
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly CloseButton _closeButton;
    private readonly SettingsNavButton[] _navButtons;
    private readonly Panel[] _sections;
    private readonly Label _footer;

    private ThemeRow _scheduleEnabledRow = null!;
    private ThemeRow _schedulePausedRow = null!;
    private SegmentedControl _modeControl = null!;
    private RoundedTextBox _lightTimeBox = null!;
    private RoundedTextBox _darkTimeBox = null!;
    private RoundedTextBox _latitudeBox = null!;
    private RoundedTextBox _longitudeBox = null!;
    private SegmentedControl _scopeControl = null!;
    private SegmentedControl _daysControl = null!;
    private Label _scheduleStatus = null!;
    private ThemeRow _startWithWindowsRow = null!;
    private ThemeRow _startupUpdatesRow = null!;
    private RoundedTextBox _toggleHotkeyBox = null!;
    private RoundedTextBox _openHotkeyBox = null!;
    private Label _hotkeyStatus = null!;
    private RoundedTextBox _feedBox = null!;
    private Label _feedStatus = null!;
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
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;
        DoubleBuffered = true;
        ClientSize = new Size(660, 600);
        BackColor = Palette.SurfaceBottom;
        ForeColor = Palette.Text;
        Font = new Font("Segoe UI", 9F);

        _header = new Panel
        {
            Bounds = new Rectangle(0, 0, ClientSize.Width, 64),
            BackColor = Color.Transparent,
        };

        _logo = new PictureBox
        {
            Image = Palette.IsLight ? Properties.Resources.themes_black : Properties.Resources.themes_white,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Bounds = new Rectangle(18, 17, 28, 28),
        };

        _title = new Label
        {
            Text = "Settings",
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Palette.Text,
            Font = new Font("Segoe UI Semibold", 13F),
            Location = new Point(56, 10),
        };

        _subtitle = new Label
        {
            Text = $"Quick Theme {AppInfo.DisplayVersion}",
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Palette.TextMuted,
            Font = new Font("Segoe UI", 8.5F),
            Location = new Point(57, 34),
        };

        _closeButton = new CloseButton
        {
            Bounds = new Rectangle(ClientSize.Width - 44, 18, 28, 28),
        };
        _closeButton.Click += (_, _) => Close();

        _header.Controls.AddRange([_logo, _title, _subtitle, _closeButton]);
        Controls.Add(_header);

        AttachDrag(_header);
        AttachDrag(_logo);
        AttachDrag(_title);
        AttachDrag(_subtitle);

        string[] navLabels = ["General", "Schedule", "Hotkeys", "Updates", "About"];
        _navButtons = new SettingsNavButton[navLabels.Length];

        for (int index = 0; index < navLabels.Length; index++)
        {
            int sectionIndex = index;
            var button = new SettingsNavButton(navLabels[index])
            {
                Bounds = new Rectangle(16, 84 + index * 44, 160, 36),
            };
            button.Click += (_, _) => ShowSection(sectionIndex);
            _navButtons[index] = button;
            Controls.Add(button);
        }

        _footer = new Label
        {
            Text = "Changes are saved automatically",
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Palette.TextMuted,
            Font = new Font("Segoe UI", 8.25F),
            Bounds = new Rectangle(18, ClientSize.Height - 32, 150, 20),
        };
        Controls.Add(_footer);

        _sections = new Panel[navLabels.Length];
        _sections[0] = CreateSectionPanel();
        _sections[1] = CreateSectionPanel();
        _sections[2] = CreateSectionPanel();
        _sections[3] = CreateSectionPanel();
        _sections[4] = CreateSectionPanel();

        BuildGeneralSection(_sections[0]);
        BuildScheduleSection(_sections[1]);
        BuildHotkeysSection(_sections[2]);
        BuildUpdatesSection(_sections[3]);
        BuildAboutSection(_sections[4]);

        _loading = true;
        LoadSettings();
        HookEvents();
        _loading = false;
        UpdateScheduleStatus();

        ShowSection(0);
        LayoutUi();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ClassStyle |= CsDropShadow;
            return parameters;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateWindowRegion();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (IsHandleCreated)
        {
            LayoutUi();
            UpdateWindowRegion();
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        Rectangle area = Screen.FromControl(this).WorkingArea;
        int width = Math.Min(Width, Math.Max(520, area.Width - 80));
        int height = Math.Min(Height, Math.Max(420, area.Height - 80));

        if (width != Width || height != Height)
        {
            Size = new Size(width, height);
            Left = area.Left + (area.Width - Width) / 2;
            Top = area.Top + (area.Height - Height) / 2;
        }

        LayoutUi();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (!Palette.IsLight)
        {
            foreach (Panel panel in _sections)
            {
                if (panel.IsHandleCreated)
                {
                    _ = SetWindowTheme(panel.Handle, "DarkMode_Explorer", null);
                }
            }
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using (var baseBrush = new SolidBrush(Palette.SurfaceBottom))
        {
            e.Graphics.FillRectangle(baseBrush, ClientRectangle);
        }

        if (_header is not null)
        {
            using var headerBrush = new SolidBrush(Palette.SurfaceTop);
            e.Graphics.FillRectangle(headerBrush, new Rectangle(0, 0, Width, _header.Height));

            int dividerX = Scale(184);
            using var sidebarBrush = new SolidBrush(
                Palette.IsLight
                    ? Color.FromArgb(246, 246, 250)
                    : Color.FromArgb(24, 24, 29));
            e.Graphics.FillRectangle(
                sidebarBrush,
                new Rectangle(0, _header.Height, dividerX, Height - _header.Height));
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using (var borderPath = Ui.RoundedRect(
                   new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f),
                   Scale(14)))
        using (var borderPen = new Pen(Palette.Border))
        {
            e.Graphics.DrawPath(borderPen, borderPath);
        }

        int dividerX = Scale(184);
        using (var separator = new Pen(Palette.Separator))
        {
            e.Graphics.DrawLine(separator, 18, _header.Bottom, Width - 18, _header.Bottom);
            e.Graphics.DrawLine(separator, dividerX, _header.Bottom + 8, dividerX, Height - 16);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    private Panel CreateSectionPanel()
    {
        var panel = new Panel
        {
            BackColor = Palette.SurfaceBottom,
            AutoScroll = true,
            Visible = false,
        };
        Controls.Add(panel);
        return panel;
    }

    private void BuildGeneralSection(Panel panel)
    {
        int y = AddPageHeader(
            panel,
            "General",
            "Startup behavior for Quick Theme.",
            0);

        _startWithWindowsRow = AddToggleRow(
            panel,
            ref y,
            "Start with Windows",
            "Launch Quick Theme when you sign in");

        _startupUpdatesRow = AddToggleRow(
            panel,
            ref y,
            "Check for updates on startup",
            "At most once every 24 hours");

        AddNote(
            panel,
            ref y,
            "Flyout theme and quick presets are available from the tray menu.");
    }

    private void BuildScheduleSection(Panel panel)
    {
        int y = AddPageHeader(
            panel,
            "Schedule",
            "Switch themes automatically on a daily rhythm.",
            0);

        _scheduleEnabledRow = AddToggleRow(
            panel,
            ref y,
            "Enable schedule",
            "Automatically switch themes");

        _modeControl = new SegmentedControl
        {
            Options = ["Fixed times", "Sunrise/sunset"],
        };
        AddSegmentedRow(panel, ref y, "Mode", _modeControl);

        _lightTimeBox = CreateBox(center: true, placeholder: "HH:mm");
        AddFieldRow(panel, ref y, "Light time", _lightTimeBox, 120);

        _darkTimeBox = CreateBox(center: true, placeholder: "HH:mm");
        AddFieldRow(panel, ref y, "Dark time", _darkTimeBox, 120);

        _latitudeBox = CreateBox(placeholder: "51.5074");
        AddFieldRow(panel, ref y, "Latitude", _latitudeBox, 160);

        _longitudeBox = CreateBox(placeholder: "-0.1278");
        AddFieldRow(panel, ref y, "Longitude", _longitudeBox, 160);

        AddNote(
            panel,
            ref y,
            "Sunrise/sunset uses your location and local time; fixed times are fallback.");

        _scopeControl = new SegmentedControl
        {
            Options = ["Apps + system", "Apps", "System"],
        };
        AddSegmentedRow(panel, ref y, "Apply to", _scopeControl);

        _daysControl = new SegmentedControl
        {
            Options = ["Every day", "Weekdays", "Weekends"],
        };
        AddSegmentedRow(panel, ref y, "Days", _daysControl);

        _schedulePausedRow = AddToggleRow(
            panel,
            ref y,
            "Pause schedule",
            "Suspend switching indefinitely");

        _scheduleStatus = AddNote(panel, ref y, string.Empty, 24);
    }

    private void BuildHotkeysSection(Panel panel)
    {
        int y = AddPageHeader(
            panel,
            "Hotkeys",
            "Global shortcuts that work from anywhere.",
            0);

        _toggleHotkeyBox = CreateBox(readOnly: true);
        AddFieldRow(panel, ref y, "Toggle dark mode", _toggleHotkeyBox, 200);

        _openHotkeyBox = CreateBox(readOnly: true);
        AddFieldRow(panel, ref y, "Open flyout", _openHotkeyBox, 200);

        var applyButton = new PillButton("Apply hotkeys", primary: true)
        {
            Bounds = new Rectangle(140, y, 130, 30),
        };
        applyButton.Click += (_, _) => ApplyHotkeyChanges();
        panel.Controls.Add(applyButton);

        _hotkeyStatus = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Palette.TextMuted,
            Font = new Font("Segoe UI", 8.5F),
            Bounds = new Rectangle(280, y, 160, 30),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        panel.Controls.Add(_hotkeyStatus);
        y += 42;

        AddNote(
            panel,
            ref y,
            "Click a box and press a combination including Ctrl, Alt, or Shift.");
    }

    private void BuildUpdatesSection(Panel panel)
    {
        int y = AddPageHeader(
            panel,
            "Updates",
            "Choose where Quick Theme looks for updates.",
            0);

        AddLabel(
            panel,
            "Update feed URL",
            new Rectangle(8, y, 420, 22),
            Palette.Text,
            9F);
        y += 24;

        _feedBox = CreateBox(placeholder: "https://example.com/update.json");
        _feedBox.Bounds = new Rectangle(8, y, 420, 30);
        panel.Controls.Add(_feedBox);
        y += 38;

        var saveButton = new PillButton("Save")
        {
            Bounds = new Rectangle(8, y, 76, 30),
        };
        saveButton.Click += (_, _) => SaveFeed();
        panel.Controls.Add(saveButton);

        var checkButton = new PillButton("Check now", primary: true)
        {
            Bounds = new Rectangle(96, y, 100, 30),
        };
        checkButton.Click += (_, _) =>
        {
            SaveFeed();
            _checkForUpdates();
        };
        panel.Controls.Add(checkButton);

        var openSettingsButton = new PillButton("Open settings")
        {
            Bounds = new Rectangle(208, y, 112, 30),
        };
        openSettingsButton.Click += (_, _) => OpenSettingsFile();
        panel.Controls.Add(openSettingsButton);
        y += 40;

        _feedStatus = AddNote(panel, ref y, string.Empty, 20);

        AddNote(
            panel,
            ref y,
            $"Settings file: {SettingsService.SettingsFilePath}",
            22);
    }

    private void BuildAboutSection(Panel panel)
    {
        int y = AddPageHeader(
            panel,
            "About",
            "A lightweight tray utility for Windows appearance.",
            0);

        AddLabel(
            panel,
            $"Quick Theme {AppInfo.DisplayVersion}",
            new Rectangle(8, y, 420, 26),
            Palette.Text,
            12F,
            FontStyle.Bold);
        y += 30;

        AddNote(panel, ref y, "MIT License · open source · Windows 10/11 · .NET 10", 22);

        var githubButton = new PillButton("GitHub");
        githubButton.Bounds = new Rectangle(8, y, 100, 30);
        githubButton.Click += (_, _) => OpenUrl("https://github.com/riad-boussaid/quick-theme");
        panel.Controls.Add(githubButton);

        var releasesButton = new PillButton("Releases");
        releasesButton.Bounds = new Rectangle(116, y, 100, 30);
        releasesButton.Click += (_, _) => OpenUrl("https://github.com/riad-boussaid/quick-theme/releases/latest");
        panel.Controls.Add(releasesButton);

        var licenseButton = new PillButton("License");
        licenseButton.Bounds = new Rectangle(224, y, 100, 30);
        licenseButton.Click += (_, _) => OpenUrl("https://github.com/riad-boussaid/quick-theme/blob/main/LICENSE");
        panel.Controls.Add(licenseButton);
        y += 40;

        AddNote(
            panel,
            ref y,
            "github.com/riad-boussaid/quick-theme",
            22);
    }

    private int AddPageHeader(Panel panel, string title, string subtitle, int y)
    {
        AddLabel(
            panel,
            title,
            new Rectangle(8, y, 420, 30),
            Palette.Text,
            14F,
            FontStyle.Bold);

        AddLabel(
            panel,
            subtitle,
            new Rectangle(8, y + 30, 420, 20),
            Palette.TextMuted,
            8.5F);

        return y + 60;
    }

    private ThemeRow AddToggleRow(Panel panel, ref int y, string title, string subtitle)
    {
        var row = new ThemeRow(title, subtitle, "On", "Off")
        {
            Bounds = new Rectangle(2, y, 426, 50),
        };
        panel.Controls.Add(row);
        y += 54;
        return row;
    }

    private void AddSegmentedRow(Panel panel, ref int y, string label, SegmentedControl control)
    {
        AddLabel(panel, label, new Rectangle(8, y + 3, 120, 26), Palette.Text, 9F);
        control.Bounds = new Rectangle(140, y, 290, 30);
        panel.Controls.Add(control);
        y += 38;
    }

    private void AddFieldRow(Panel panel, ref int y, string label, RoundedTextBox box, int width)
    {
        AddLabel(panel, label, new Rectangle(8, y + 3, 120, 26), Palette.Text, 9F);
        box.Bounds = new Rectangle(140, y, width, 30);
        panel.Controls.Add(box);
        y += 38;
    }

    private Label AddNote(Panel panel, ref int y, string text, int height = 20)
    {
        var label = AddLabel(panel, text, new Rectangle(8, y, 420, height), Palette.TextMuted, 8.5F);
        y += height + 4;
        return label;
    }

    private static Label AddLabel(
        Panel panel,
        string text,
        Rectangle bounds,
        Color color,
        float size,
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
        panel.Controls.Add(label);
        return label;
    }

    private static RoundedTextBox CreateBox(
        bool readOnly = false,
        bool center = false,
        string? placeholder = null)
    {
        var box = new RoundedTextBox();
        box.InnerTextBox.ReadOnly = readOnly;
        box.InnerTextBox.TextAlign = center ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        box.InnerTextBox.PlaceholderText = placeholder ?? string.Empty;

        if (readOnly)
        {
            box.Cursor = Cursors.Hand;
            box.InnerTextBox.Cursor = Cursors.Hand;
        }

        return box;
    }

    private void HookEvents()
    {
        _scheduleEnabledRow.CheckedChanged += OnScheduleEnabledChanged;
        _schedulePausedRow.CheckedChanged += OnSchedulePausedChanged;
        _modeControl.SelectedIndexChanged += OnScheduleModeChanged;
        _scopeControl.SelectedIndexChanged += OnScheduleOptionChanged;
        _daysControl.SelectedIndexChanged += OnScheduleOptionChanged;

        _lightTimeBox.InnerTextBox.Leave += OnScheduleFieldLeave;
        _darkTimeBox.InnerTextBox.Leave += OnScheduleFieldLeave;
        _latitudeBox.InnerTextBox.Leave += OnScheduleFieldLeave;
        _longitudeBox.InnerTextBox.Leave += OnScheduleFieldLeave;

        _startWithWindowsRow.CheckedChanged += OnStartWithWindowsChanged;
        _startupUpdatesRow.CheckedChanged += OnStartupUpdatesChanged;

        _toggleHotkeyBox.InnerTextBox.KeyDown += OnHotkeyBoxKeyDown;
        _openHotkeyBox.InnerTextBox.KeyDown += OnHotkeyBoxKeyDown;
    }

    private void LoadSettings()
    {
        AppSettings settings = SettingsService.Current;

        _scheduleEnabledRow.SetCheckedSilently(settings.ScheduleEnabled);
        _schedulePausedRow.SetCheckedSilently(settings.SchedulePaused);
        _modeControl.SelectedIndex = (int)settings.ScheduleMode;
        _scopeControl.SelectedIndex = (int)settings.ScheduleScope;
        _daysControl.SelectedIndex = (int)settings.ScheduleDays;
        _startWithWindowsRow.SetCheckedSilently(StartupService.IsEnabled);
        _startupUpdatesRow.SetCheckedSilently(settings.CheckForUpdatesOnStartup);
        _toggleHotkeyBox.InnerTextBox.Text = settings.HotkeyToggleDark;
        _openHotkeyBox.InnerTextBox.Text = settings.HotkeyOpenPanel;
        _feedBox.InnerTextBox.Text = settings.UpdateFeedUrl;

        SetScheduleTextFields();
    }

    private void SetScheduleTextFields()
    {
        AppSettings settings = SettingsService.Current;
        _lightTimeBox.InnerTextBox.Text = settings.ScheduleLightTime.ToString("HH:mm", CultureInfo.CurrentCulture);
        _darkTimeBox.InnerTextBox.Text = settings.ScheduleDarkTime.ToString("HH:mm", CultureInfo.CurrentCulture);
        _latitudeBox.InnerTextBox.Text = settings.ScheduleLatitude.ToString("0.####", CultureInfo.CurrentCulture);
        _longitudeBox.InnerTextBox.Text = settings.ScheduleLongitude.ToString("0.####", CultureInfo.CurrentCulture);
    }

    public void RefreshFromSettings()
    {
        _loading = true;
        try
        {
            LoadSettings();
        }
        finally
        {
            _loading = false;
        }

        UpdateScheduleStatus();
    }

    private void ShowSection(int index)
    {
        for (int section = 0; section < _sections.Length; section++)
        {
            bool visible = section == index;
            _sections[section].Visible = visible;
            _navButtons[section].SetSelected(visible);
        }
    }

    private void LayoutUi()
    {
        if (_header is null || _sections is null)
        {
            return;
        }

        _header.Bounds = new Rectangle(0, 0, ClientSize.Width, Scale(64));
        _closeButton.Location = new Point(ClientSize.Width - Scale(16) - _closeButton.Width, Scale(18));

        int contentLeft = Scale(200);
        int contentTop = _header.Bottom + Scale(16);
        int contentWidth = Math.Max(0, ClientSize.Width - contentLeft - Scale(16));
        int contentHeight = Math.Max(0, ClientSize.Height - contentTop - Scale(16));

        foreach (Panel panel in _sections)
        {
            panel.Bounds = new Rectangle(contentLeft, contentTop, contentWidth, contentHeight);
        }

        _footer.Location = new Point(Scale(18), ClientSize.Height - Scale(32));
    }

    private void UpdateWindowRegion()
    {
        using var path = Ui.RoundedRect(new RectangleF(0f, 0f, Width, Height), Scale(14));
        Region? oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
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

    private void OnScheduleFieldLeave(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        CommitScheduleFields();
    }

    private void CommitScheduleFields()
    {
        if (!TimeOnly.TryParse(
                _lightTimeBox.InnerTextBox.Text,
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out TimeOnly light) ||
            !TimeOnly.TryParse(
                _darkTimeBox.InnerTextBox.Text,
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out TimeOnly dark) ||
            !TryParseCoordinate(_latitudeBox.InnerTextBox.Text, -90, 90, out double latitude) ||
            !TryParseCoordinate(_longitudeBox.InnerTextBox.Text, -180, 180, out double longitude))
        {
            SetScheduleTextFields();
            _scheduleStatus.ForeColor = Palette.Danger;
            _scheduleStatus.Text = "Enter a valid time and coordinate range.";
            return;
        }

        SettingsService.Current.ScheduleLightTime = light;
        SettingsService.Current.ScheduleDarkTime = dark;
        SettingsService.Current.ScheduleLatitude = latitude;
        SettingsService.Current.ScheduleLongitude = longitude;

        SetScheduleTextFields();
        SaveAndNotifySchedule();
    }

    private static bool TryParseCoordinate(string text, double minimum, double maximum, out double value)
    {
        if (!double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out value) &&
            !double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value))
        {
            return false;
        }

        return value >= minimum && value <= maximum;
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
        _hotkeyStatus.Text = "Apply to confirm.";
    }

    private void ApplyHotkeyChanges()
    {
        string? error = _applyHotkeys(
            _toggleHotkeyBox.InnerTextBox.Text,
            _openHotkeyBox.InnerTextBox.Text);

        if (error is not null)
        {
            _hotkeyStatus.ForeColor = Palette.Danger;
            _hotkeyStatus.Text = error;
            return;
        }

        _toggleHotkeyBox.InnerTextBox.Text = SettingsService.Current.HotkeyToggleDark;
        _openHotkeyBox.InnerTextBox.Text = SettingsService.Current.HotkeyOpenPanel;
        _hotkeyStatus.ForeColor = Palette.TextMuted;
        _hotkeyStatus.Text = "Hotkeys applied.";
    }

    private void SaveFeed()
    {
        SettingsService.Current.UpdateFeedUrl = _feedBox.InnerTextBox.Text.Trim();
        SettingsService.Save();
        _feedStatus.Text = "Saved.";
    }

    private static void OpenSettingsFile()
    {
        try
        {
            SettingsService.EnsureSettingsFile();
            Process.Start(new ProcessStartInfo(SettingsService.SettingsFilePath)
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            // Best effort.
        }
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

        if (ScheduleOverrideService.Describe(settings, DateTimeOffset.Now) is { } overrideText)
        {
            _scheduleStatus.ForeColor = Palette.Accent;
            _scheduleStatus.Text = $"{overrideText} (temporary)";
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

    private void AttachDrag(Control control)
    {
        control.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                _ = SendMessage(Handle, WmNcLButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
            }
        };
    }

    private int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96.0);

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

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);
}
