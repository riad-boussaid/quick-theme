using System.Drawing.Drawing2D;
using QuickTheme.Models;
using QuickTheme.Services;
using QuickTheme.UI.Controls;

namespace QuickTheme.UI;

internal sealed class UpdateProgressForm : Form
{
    private const int CsDropShadow = 0x00020000;

    private readonly UpdateManifest _manifest;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly SlimProgressBar _progressBar;
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly Label _status;
    private readonly Label _cancelLink;
    private bool _finished;

    public UpdateProgressForm(UpdateManifest manifest)
    {
        _manifest = manifest;

        Palette.Apply(SettingsService.Current.FlyoutTheme, ThemeService.SystemUsesDarkTheme);

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        DoubleBuffered = true;
        ClientSize = new Size(400, 160);
        BackColor = Palette.SurfaceBottom;
        Icon = Properties.Resources.themes_256_black;
        Text = "Quick Theme update";

        _title = new Label
        {
            Text = "Downloading update",
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Palette.Text,
            Font = new Font("Segoe UI Semibold", 11.75F),
            Location = new Point(20, 18),
        };

        _subtitle = new Label
        {
            Text = $"Quick Theme {manifest.Version}",
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Palette.TextMuted,
            Font = new Font("Segoe UI", 8.5F),
            Location = new Point(21, 42),
        };

        _progressBar = new SlimProgressBar
        {
            Bounds = new Rectangle(20, 72, 360, 8),
            Indeterminate = true,
        };

        _status = new Label
        {
            Text = "Starting download...",
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Palette.TextMuted,
            Font = new Font("Segoe UI", 8.5F),
            Bounds = new Rectangle(20, 88, 300, 20),
        };

        _cancelLink = new Label
        {
            Text = "Cancel",
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Palette.TextMuted,
            Font = new Font("Segoe UI", 8.5F),
            Bounds = new Rectangle(340, 122, 40, 22),
            TextAlign = ContentAlignment.MiddleRight,
            Cursor = Cursors.Hand,
        };
        _cancelLink.MouseEnter += (_, _) => _cancelLink.ForeColor = Palette.Accent;
        _cancelLink.MouseLeave += (_, _) => _cancelLink.ForeColor = Palette.TextMuted;
        _cancelLink.Click += (_, _) => CancelDownload();

        Controls.AddRange([_title, _subtitle, _progressBar, _status, _cancelLink]);
    }

    public string? DownloadedPath { get; private set; }

    public Exception? Error { get; private set; }

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

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var path = Ui.RoundedRect(
            new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f),
            12);
        using var pen = new Pen(Palette.Border);
        e.Graphics.DrawPath(pen, path);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        _progressBar.Indeterminate = true;
        _status.Text = "Starting download...";

        try
        {
            var progress = new Progress<UpdateDownloadProgress>(ReportProgress);
            DownloadedPath = await UpdateService.DownloadAsync(_manifest, progress, _cancellation.Token);

            _finished = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
            {
                _finished = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                Error = ex;
                _finished = true;
                DialogResult = DialogResult.Abort;
                Close();
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_finished)
        {
            _cancellation.Cancel();
        }

        base.OnFormClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            CancelDownload();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellation.Dispose();
        }

        base.Dispose(disposing);
    }

    private void CancelDownload()
    {
        if (_finished)
        {
            return;
        }

        _status.Text = "Cancelling...";
        _cancelLink.Enabled = false;
        _cancellation.Cancel();
    }

    private void ReportProgress(UpdateDownloadProgress progress)
    {
        if (IsDisposed)
        {
            return;
        }

        if (progress.TotalBytes is > 0)
        {
            int percent = (int)Math.Clamp(
                progress.BytesReceived * 100L / progress.TotalBytes.Value,
                0,
                100);

            _progressBar.Indeterminate = false;
            _progressBar.Value = percent;
            _status.Text = $"{FormatBytes(progress.BytesReceived)} of {FormatBytes(progress.TotalBytes.Value)} ({percent}%)";
        }
        else
        {
            _progressBar.Indeterminate = true;
            _status.Text = $"{FormatBytes(progress.BytesReceived)} downloaded";
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        int unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }

    private void UpdateWindowRegion()
    {
        using var path = Ui.RoundedRect(
            new RectangleF(0f, 0f, Width, Height),
            Scale(12));
        Region? oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }

    private int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96.0);
}
