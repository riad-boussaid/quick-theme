using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using QuickTheme.Models;
using QuickTheme.Services;
using QuickTheme.UI.Controls;

namespace QuickTheme.UI;

internal sealed class ThemeFlyoutForm : Form
{
    private const int CsDropShadow = 0x00020000;
    private const int WmSettingChange = 0x001A;

    private readonly ThemeRow _appsRow;
    private readonly ThemeRow _systemRow;
    private readonly ThemeRow _transparencyRow;
    private readonly Label _accentTitle;
    private readonly Label _accentValue;
    private readonly AccentSwatch[] _accentSwatches;
    private readonly PillButton _automaticAccentButton;
    private readonly LinkButton _customAccentLink;
    private readonly LinkButton _accentMoreLink;
    private readonly ToolTip _accentToolTip = new();
    private readonly LinkButton _scheduleStatus;
    private readonly LinkButton _restartLink;
    private readonly LinkButton _settingsLink;
    private readonly LinkButton _quitLink;
    private readonly CloseButton _closeButton;
    private readonly PictureBox _logo;
    private readonly Label _title;
    private readonly Label _subtitle;
    private bool _refreshing;
    private bool _suppressDeactivateHide;
    private bool _systemRefreshQueued;
    private bool _automaticAccentBusy;

    public event EventHandler? SystemThemeChanged;

    public event Action<ThemeSetting, bool>? SettingToggleRequested;

    public event Action? SettingsRequested;

    public event Action? AccentSettingsRequested;

    public event Action? ResumeScheduleRequested;

    public ThemeFlyoutForm()
    {
        Palette.Apply(SettingsService.Current.FlyoutTheme, ThemeService.SystemUsesDarkTheme);

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Icon = Properties.Resources.themes_256_black;
        Text = "Quick Theme";
        AccessibleName = "Quick Theme";
        AccessibleDescription = "Quick theme controls, accent color, and appearance status.";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        MinimizeBox = false;
        MaximizeBox = false;
        KeyPreview = true;
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Palette.SurfaceBottom;
        ClientSize = new Size(344, 364);
        Font = new Font("Segoe UI", 9F);

        _logo = new PictureBox
        {
            Image = Properties.Resources.themes_white,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Bounds = new Rectangle(14, 14, 32, 32),
        };

        _title = new Label
        {
            Text = "Quick Theme",
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Palette.Text,
            Font = new Font("Segoe UI Semibold", 11.75F),
            Location = new Point(58, 12),
        };

        _subtitle = new Label
        {
            Text = "Windows appearance",
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Palette.TextMuted,
            Font = new Font("Segoe UI", 8.25F),
            Location = new Point(59, 35),
        };

        _closeButton = new CloseButton
        {
            Bounds = new Rectangle(ClientSize.Width - 14 - 28, 14, 28, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        _closeButton.Click += (_, _) => HidePanel();

        _appsRow = new ThemeRow("App theme", "Apps and Settings", "Dark", "Light")
        {
            Bounds = new Rectangle(12, 70, ClientSize.Width - 24, 56),
        };

        _systemRow = new ThemeRow("System theme", "Taskbar and Start", "Dark", "Light")
        {
            Bounds = new Rectangle(12, 126, ClientSize.Width - 24, 56),
        };

        _transparencyRow = new ThemeRow("Transparency", "Window effects", "On", "Off")
        {
            Bounds = new Rectangle(12, 182, ClientSize.Width - 24, 56),
        };

        _appsRow.CheckedChanged += OnRowCheckedChanged;
        _systemRow.CheckedChanged += OnRowCheckedChanged;
        _transparencyRow.CheckedChanged += OnRowCheckedChanged;

        _accentTitle = new Label
        {
            Text = "Accent color",
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Palette.Text,
            Font = new Font("Segoe UI Semibold", 9.75F),
            Bounds = new Rectangle(14, 242, 140, 18),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _accentValue = new Label
        {
            Text = string.Empty,
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Palette.TextMuted,
            Font = new Font("Segoe UI", 8.25F),
            Bounds = new Rectangle(170, 242, 110, 18),
            TextAlign = ContentAlignment.MiddleRight,
            AccessibleName = "Current accent color",
        };

        (string Name, Color Color)[] quickAccents = AccentColors.Quick;
        _accentSwatches = new AccentSwatch[quickAccents.Length];

        for (int index = 0; index < quickAccents.Length; index++)
        {
            (string name, Color color) = quickAccents[index];
            var swatch = new AccentSwatch(color, name)
            {
                Bounds = new Rectangle(14 + index * 28, 263, 24, 24),
            };
            swatch.Click += (_, _) => ApplyAccent(swatch.AccentColor);
            _accentToolTip.SetToolTip(swatch, $"{name} · #{color.R:X2}{color.G:X2}{color.B:X2}");
            _accentSwatches[index] = swatch;
        }

        _automaticAccentButton = new PillButton("Auto")
        {
            Bounds = new Rectangle(242, 263, 42, 24),
            AccessibleDescription = "Toggle automatic accent color from the wallpaper.",
        };
        _automaticAccentButton.Click += OnAutomaticAccentClick;
        _accentToolTip.SetToolTip(_automaticAccentButton, "Automatic accent from wallpaper");

        _customAccentLink = new LinkButton("Custom")
        {
            Bounds = new Rectangle(290, 263, 44, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            AccessibleDescription = "Choose a custom Windows accent color.",
        };
        _customAccentLink.Click += OnCustomAccentClick;
        _accentToolTip.SetToolTip(_customAccentLink, "Choose a custom accent color");

        _accentMoreLink = new LinkButton("More...")
        {
            Bounds = new Rectangle(286, 242, 44, 18),
            TextAlign = ContentAlignment.MiddleRight,
            AccessibleDescription = "Open more accent colors in Settings.",
        };
        _accentMoreLink.Click += (_, _) => AccentSettingsRequested?.Invoke();
        _accentToolTip.SetToolTip(_accentMoreLink, "Open more accent colors in Settings");

        _scheduleStatus = new LinkButton(string.Empty)
        {
            Bounds = new Rectangle(14, 300, ClientSize.Width - 28, 18),
            TextAlign = ContentAlignment.MiddleLeft,
            AccessibleName = "Schedule status",
        };
        _scheduleStatus.Click += (_, _) => ResumeScheduleRequested?.Invoke();

        _restartLink = new LinkButton("Restart Explorer")
        {
            Bounds = new Rectangle(82, 330, 140, 24),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _restartLink.Click += OnRestartExplorerClick;

        _settingsLink = new LinkButton("Settings")
        {
            Bounds = new Rectangle(14, 330, 60, 24),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _settingsLink.Click += (_, _) => SettingsRequested?.Invoke();

        _quitLink = new LinkButton("Quit", danger: true)
        {
            Bounds = new Rectangle(ClientSize.Width - 14 - 60, 330, 60, 24),
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        _quitLink.Click += (_, _) => Application.Exit();

        Controls.AddRange(
        [
            _logo,
            _title,
            _subtitle,
            _closeButton,
            _appsRow,
            _systemRow,
            _transparencyRow,
            _accentTitle,
            _accentValue,
            .._accentSwatches,
            _automaticAccentButton,
            _customAccentLink,
            _accentMoreLink,
            _scheduleStatus,
            _settingsLink,
            _restartLink,
            _quitLink,
        ]);

        RefreshThemeState();
    }

    public void ShowPanel()
    {
        RefreshThemeState();
        PositionNearTray();

        Show();
        Activate();
        BringToFront();
    }

    public void HidePanel() => Hide();

    public void RefreshThemeState()
    {
        ThemeService.RefreshAccentColor();
        ApplyPalette();
        RefreshAccentState();
        RefreshState();
    }

    public void ShowThemeError(string message) =>
        ShowError("Could not change the theme", message);

    private void ShowError(string heading, string message)
    {
        _suppressDeactivateHide = true;
        try
        {
            var page = new TaskDialogPage
            {
                Caption = "Quick Theme",
                Heading = heading,
                Text = message,
                Icon = TaskDialogIcon.Error,
                Buttons = { TaskDialogButton.OK },
            };

            TaskDialog.ShowDialog(this, page);
        }
        finally
        {
            _suppressDeactivateHide = false;
        }
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
            UpdateWindowRegion();
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
        {
            return;
        }

        using var brush = new LinearGradientBrush(
            ClientRectangle,
            Palette.SurfaceTop,
            Palette.SurfaceBottom,
            LinearGradientMode.Vertical);

        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int pad = Scale(14);
        using (var borderPath = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Scale(14)))
        using (var borderPen = new Pen(Palette.Border))
        {
            g.DrawPath(borderPen, borderPath);
        }

        using (var separator = new Pen(Palette.Separator))
        {
            g.DrawLine(separator, pad, Scale(62), Width - pad, Scale(62));
            g.DrawLine(separator, pad, Scale(294), Width - pad, Scale(294));
        }
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);

        if (!_suppressDeactivateHide && Visible)
        {
            HidePanel();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            HidePanel();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HidePanel();
        }
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg != WmSettingChange || m.LParam == IntPtr.Zero)
        {
            return;
        }

        string? setting = Marshal.PtrToStringUni(m.LParam);
        if (string.Equals(setting, "ImmersiveColorSet", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(setting, "WindowMetrics", StringComparison.OrdinalIgnoreCase))
        {
            QueueSystemRefresh();
        }
    }

    private void QueueSystemRefresh()
    {
        if (_systemRefreshQueued || IsDisposed || !IsHandleCreated)
        {
            return;
        }

        _systemRefreshQueued = true;

        try
        {
            BeginInvoke(() =>
            {
                _systemRefreshQueued = false;

                if (IsDisposed)
                {
                    return;
                }

                RefreshThemeState();
                SystemThemeChanged?.Invoke(this, EventArgs.Empty);
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            _systemRefreshQueued = false;
        }
    }

    private void ApplyPalette()
    {
        Palette.Apply(SettingsService.Current.FlyoutTheme, ThemeService.SystemUsesDarkTheme);

        BackColor = Palette.SurfaceBottom;
        _title.ForeColor = Palette.Text;
        _subtitle.ForeColor = Palette.TextMuted;
        _accentTitle.ForeColor = Palette.Text;
        _logo.Image = Palette.IsLight
            ? Properties.Resources.themes_black
            : Properties.Resources.themes_white;

        Invalidate(invalidateChildren: true);
    }

    private void RefreshAccentState()
    {
        Color current = AccentColorService.CurrentAccent;
        bool automatic = AccentColorService.AutomaticAccent;
        bool enabled = !SystemInformation.HighContrast;

        _accentTitle.ForeColor = enabled ? Palette.Text : Palette.TextMuted;
        _automaticAccentButton.Enabled = enabled && !_automaticAccentBusy;
        _customAccentLink.Actionable = enabled;
        _accentMoreLink.Actionable = enabled;

        foreach (AccentSwatch swatch in _accentSwatches)
        {
            swatch.Enabled = enabled;
            swatch.Selected = enabled && !automatic && swatch.AccentColor.ToArgb() == current.ToArgb();
        }

        if (!enabled)
        {
            _accentValue.Text = "High contrast";
            _accentValue.ForeColor = Palette.TextMuted;
            _automaticAccentButton.AccessibleDescription =
                "Accent color is unavailable in high contrast mode.";
            return;
        }

        _accentValue.Text = automatic
            ? "Automatic"
            : $"#{current.R:X2}{current.G:X2}{current.B:X2}";
        _accentValue.ForeColor = automatic ? Palette.Accent : Palette.TextMuted;
        _automaticAccentButton.AccessibleDescription = automatic
            ? "Automatic accent color is on. Click to turn it off."
            : "Automatic accent color is off. Click to use the wallpaper color.";
    }

    private void ApplyAccent(Color color)
    {
        try
        {
            AccentColorService.Apply(
                color,
                AccentColorService.AccentOnTaskbar,
                AccentColorService.AccentOnTitleBars);

            RefreshThemeState();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or InvalidOperationException)
        {
            ShowError("Could not change the accent color", ex.Message);
        }
    }

    private async void OnAutomaticAccentClick(object? sender, EventArgs e)
    {
        try
        {
            if (AccentColorService.AutomaticAccent)
            {
                AccentColorService.DisableAutomaticAccent();
                RefreshThemeState();
                return;
            }

            _automaticAccentBusy = true;
            _automaticAccentButton.Enabled = false;
            _accentValue.Text = "Updating...";
            _accentValue.ForeColor = Palette.TextMuted;

            try
            {
                await AccentColorService.EnableAutomaticAccentAsync();
            }
            finally
            {
                _automaticAccentBusy = false;

                if (!_automaticAccentButton.IsDisposed)
                {
                    _automaticAccentButton.Enabled = !SystemInformation.HighContrast;
                }
            }

            if (!IsDisposed)
            {
                RefreshThemeState();
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or InvalidOperationException)
        {
            if (!IsDisposed)
            {
                RefreshThemeState();
                ShowError("Could not change the accent color", ex.Message);
            }
        }
    }

    private void OnCustomAccentClick(object? sender, EventArgs e)
    {
        _suppressDeactivateHide = true;
        try
        {
            using var dialog = new ColorDialog
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                Color = AccentColorService.CurrentAccent,
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                ApplyAccent(dialog.Color);
            }
        }
        finally
        {
            _suppressDeactivateHide = false;
        }
    }

    private void OnRowCheckedChanged(object? sender, EventArgs e)
    {
        if (_refreshing || sender is not ThemeRow row)
        {
            return;
        }

        ThemeSetting setting = row == _appsRow
            ? ThemeSetting.Apps
            : row == _systemRow
                ? ThemeSetting.System
                : ThemeSetting.Transparency;

        SettingToggleRequested?.Invoke(setting, row.Checked);
    }

    private async void OnRestartExplorerClick(object? sender, EventArgs e)
    {
        _suppressDeactivateHide = true;
        try
        {
            var page = new TaskDialogPage
            {
                Caption = "Quick Theme",
                Heading = "Restart Explorer?",
                Text = "Explorer will restart so the new theme reaches the taskbar and Start menu. Open File Explorer windows will close.",
                Icon = TaskDialogIcon.Warning,
                DefaultButton = TaskDialogButton.Cancel,
                Buttons = { TaskDialogButton.Yes, TaskDialogButton.Cancel },
            };

            if (TaskDialog.ShowDialog(this, page) != TaskDialogButton.Yes)
            {
                return;
            }
        }
        finally
        {
            _suppressDeactivateHide = false;
        }

        HidePanel();
        await ExplorerService.RestartAsync();
    }

    private void RefreshState()
    {
        _refreshing = true;
        try
        {
            _appsRow.SetCheckedSilently(ThemeService.AppsUseDarkTheme);
            _systemRow.SetCheckedSilently(ThemeService.SystemUsesDarkTheme);
            _transparencyRow.SetCheckedSilently(ThemeService.TransparencyEnabled);
        }
        finally
        {
            _refreshing = false;
        }

        RefreshScheduleStatus();
    }

    public void RefreshScheduleStatus()
    {
        AppSettings settings = SettingsService.Current;
        string? overrideText = ScheduleOverrideService.Describe(settings, DateTimeOffset.Now);
        bool paused = settings.ScheduleEnabled && settings.SchedulePaused;
        bool actionable = overrideText is not null || paused;

        string statusText;
        if (overrideText is not null)
        {
            statusText = overrideText;
        }
        else if (!settings.ScheduleEnabled)
        {
            statusText = "Schedule is off";
        }
        else if (paused)
        {
            statusText = "Schedule is paused";
        }
        else
        {
            ScheduleTransition? next = ScheduleCalculator.GetNextTransition(settings, DateTime.Now);
            statusText = next is null
                ? "No upcoming switch"
                : $"Next switch: {next.Value.At:ddd HH:mm} → {(next.Value.Dark ? "Dark" : "Light")}";
        }

        _scheduleStatus.Actionable = actionable;
        _scheduleStatus.Text = actionable ? $"{statusText}  ·  click to resume" : statusText;
        _scheduleStatus.AccessibleDescription = actionable
            ? $"{statusText}. Press Enter to resume."
            : statusText;
        _scheduleStatus.AccessibleDefaultActionDescription = actionable ? "Resume schedule" : null;
    }

    private void PositionNearTray()
    {
        Screen screen = Screen.FromPoint(Cursor.Position);
        Rectangle area = screen.WorkingArea;
        int margin = Scale(12);

        int x = Math.Max(area.Left + margin, area.Right - Width - margin);
        int y = Math.Max(area.Top + margin, area.Bottom - Height - margin);

        Location = new Point(x, y);
    }

    private void UpdateWindowRegion()
    {
        using var path = Ui.RoundedRect(new RectangleF(0f, 0f, Width, Height), Scale(14));
        Region? oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }

    private int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96.0);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _accentToolTip.Dispose();
        }

        base.Dispose(disposing);
    }
}
