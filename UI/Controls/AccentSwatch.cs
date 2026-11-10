using System.ComponentModel;
using System.Drawing.Drawing2D;
using QuickTheme.UI;

namespace QuickTheme.UI.Controls;

internal sealed class AccentSwatch : Control
{
    private bool _selected;
    private bool _hover;

    public AccentSwatch(Color accentColor, string name)
    {
        AccentColor = accentColor;

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
        Size = new Size(28, 28);
        AccessibleRole = AccessibleRole.RadioButton;
        AccessibleName = $"{name} accent color";
        AccessibleDescription = $"#{accentColor.R:X2}{accentColor.G:X2}{accentColor.B:X2}";
        AccessibleDefaultActionDescription = "Apply accent color";
    }

    public Color AccentColor { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
            {
                return;
            }

            _selected = value;
            Invalidate();
            AccessibilityNotifyClients(AccessibleEvents.StateChange, -1);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int inset = Scale(2);
        var circle = new RectangleF(
            inset,
            inset,
            Math.Max(1, Width - inset * 2 - 1),
            Math.Max(1, Height - inset * 2 - 1));

        using (var brush = new SolidBrush(Enabled ? AccentColor : Color.FromArgb(90, AccentColor)))
        {
            graphics.FillEllipse(brush, circle);
        }

        using (var border = new Pen(Color.FromArgb(Enabled ? 90 : 50, Palette.Text), 1f))
        {
            graphics.DrawEllipse(border, circle);
        }

        if (_selected && Enabled)
        {
            using var ring = new Pen(Palette.Text, 2f);
            graphics.DrawEllipse(ring, new RectangleF(1f, 1f, Width - 2f, Height - 2f));

            int brightness = (AccentColor.R * 299 + AccentColor.G * 587 + AccentColor.B * 114) / 1000;
            Color markColor = brightness > 140
                ? Color.FromArgb(30, 30, 30)
                : Color.White;
            using var markPen = new Pen(markColor, 2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            graphics.DrawLines(
                markPen,
                [
                    new PointF(Width * 0.32f, Height * 0.52f),
                    new PointF(Width * 0.45f, Height * 0.65f),
                    new PointF(Width * 0.70f, Height * 0.36f),
                ]);
        }

        if (_hover && Enabled)
        {
            using var hover = new Pen(Color.FromArgb(150, Palette.Text), 1f);
            graphics.DrawEllipse(hover, circle);
        }

        if (Focused && ShowFocusCues && Enabled)
        {
            ControlPaint.DrawFocusRectangle(
                graphics,
                new Rectangle(0, 0, Width, Height),
                Palette.Text,
                Color.Transparent);
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

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Focus();
        }

        base.OnMouseDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override AccessibleObject CreateAccessibilityInstance() =>
        new AccentSwatchAccessibleObject(this);

    private int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96.0);

    private sealed class AccentSwatchAccessibleObject : ControlAccessibleObject
    {
        private readonly AccentSwatch _owner;

        public AccentSwatchAccessibleObject(AccentSwatch owner) : base(owner) => _owner = owner;

        public override AccessibleStates State =>
            base.State | (_owner.Selected ? AccessibleStates.Checked : AccessibleStates.None);
    }
}
