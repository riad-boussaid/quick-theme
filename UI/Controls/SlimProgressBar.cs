using System.ComponentModel;
using System.Drawing.Drawing2D;
using QuickTheme.UI;

namespace QuickTheme.UI.Controls;

internal sealed class SlimProgressBar : Control
{
    private readonly System.Windows.Forms.Timer _timer;
    private int _value;
    private bool _indeterminate;
    private float _marquee;

    public SlimProgressBar()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        Height = 8;

        _timer = new System.Windows.Forms.Timer { Interval = 30 };
        _timer.Tick += (_, _) =>
        {
            _marquee += 0.022f;
            if (_marquee > 1f)
            {
                _marquee = 0f;
            }

            Invalidate();
        };
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, 0, 100);
            if (clamped == _value)
            {
                return;
            }

            _value = clamped;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Indeterminate
    {
        get => _indeterminate;
        set
        {
            if (_indeterminate == value)
            {
                return;
            }

            _indeterminate = value;
            if (value && Visible)
            {
                _marquee = 0f;
                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }

            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float height = Height;
        var track = new RectangleF(0f, 0f, Width, height);

        using (var trackPath = Ui.RoundedRect(track, height / 2f))
        using (var trackBrush = new SolidBrush(
                   Palette.IsLight
                       ? Color.FromArgb(224, 224, 232)
                       : Color.FromArgb(52, 52, 62)))
        {
            g.FillPath(trackBrush, trackPath);
        }

        if (_indeterminate)
        {
            float segmentWidth = Width * 0.32f;
            float x = (Width + segmentWidth) * _marquee - segmentWidth;
            x = Math.Clamp(x, -segmentWidth, Width);

            if (x < Width)
            {
                var fill = new RectangleF(x, 0f, Math.Min(segmentWidth, Width - x), height);
                using var fillPath = Ui.RoundedRect(fill, height / 2f);
                using var fillBrush = new SolidBrush(Palette.Accent);
                g.FillPath(fillBrush, fillPath);
            }
        }
        else if (_value > 0)
        {
            float width = Width * (_value / 100f);
            var fill = new RectangleF(0f, 0f, width, height);
            using var fillPath = Ui.RoundedRect(fill, height / 2f);
            using var fillBrush = new SolidBrush(Palette.Accent);
            g.FillPath(fillBrush, fillPath);
        }
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (!Visible)
        {
            _timer.Stop();
        }
        else if (_indeterminate)
        {
            _timer.Start();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }
}
