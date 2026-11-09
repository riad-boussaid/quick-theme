using System.Drawing.Drawing2D;
using QuickTheme.UI;

namespace QuickTheme.UI.Controls;

internal sealed class ToggleSwitch : CheckBox
{
    private readonly System.Windows.Forms.Timer _animation;
    private float _progress;
    private float _target;
    private bool _hover;
    private bool _suppressAnimation;

    public ToggleSwitch()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = true;
        Size = new Size(44, 24);
        AccessibleRole = AccessibleRole.CheckButton;

        _animation = new System.Windows.Forms.Timer { Interval = 15 };
        _animation.Tick += (_, _) => Animate();
    }

    public void SetChecked(bool value, bool animate)
    {
        if (animate)
        {
            Checked = value;
            return;
        }

        _suppressAnimation = true;
        try
        {
            Checked = value;
        }
        finally
        {
            _suppressAnimation = false;
        }

        _animation.Stop();
        _progress = _target = value ? 1f : 0f;
        Invalidate();
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        _target = Checked ? 1f : 0f;

        if (_suppressAnimation)
        {
            _progress = _target;
        }
        else
        {
            _animation.Start();
        }

        base.OnCheckedChanged(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var track = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        float radius = track.Height / 2f;

        Color onColor = _hover ? ControlPaint.Light(Palette.Accent, 0.10f) : Palette.Accent;
        Color offColor = _hover ? ControlPaint.Light(Palette.TrackOff, 0.10f) : Palette.TrackOff;

        using (var path = Ui.RoundedRect(track, radius))
        using (var brush = new SolidBrush(Ui.Mix(offColor, onColor, _progress)))
        {
            g.FillPath(brush, path);
        }

        float inset = Math.Max(2f, Height * 0.11f);
        float thumbSize = track.Height - inset * 2f;
        float travel = track.Width - inset * 2f - thumbSize;
        float thumbX = track.Left + inset + travel * _progress;

        using (var shadow = new SolidBrush(Color.FromArgb(48, 0, 0, 0)))
        {
            g.FillEllipse(shadow, thumbX, track.Top + inset + 1f, thumbSize, thumbSize);
        }

        using (var thumb = new SolidBrush(Palette.ToggleThumb))
        {
            g.FillEllipse(thumb, thumbX, track.Top + inset, thumbSize, thumbSize);
        }

        if (Focused && ShowFocusCues)
        {
            using var focusPath = Ui.RoundedRect(new RectangleF(1f, 1f, Width - 2f, Height - 2f), radius);
            using var pen = new Pen(Color.FromArgb(190, Palette.ToggleThumb), 1f);
            g.DrawPath(pen, focusPath);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animation.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Animate()
    {
        const float step = 0.17f;

        if (_progress < _target)
        {
            _progress = Math.Min(_target, _progress + step);
        }
        else if (_progress > _target)
        {
            _progress = Math.Max(_target, _progress - step);
        }

        if (Math.Abs(_progress - _target) < 0.001f)
        {
            _progress = _target;
            _animation.Stop();
        }

        Invalidate();
    }
}
