using System.ComponentModel;
using System.Drawing.Drawing2D;
using QuickTheme.UI;

namespace QuickTheme.UI.Controls;

internal sealed class LinkButton : Control
{
    private readonly bool _danger;
    private bool _hover;
    private bool _actionable = true;

    public LinkButton(string text, bool danger = false)
    {
        Text = text;
        _danger = danger;

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
        Font = new Font("Segoe UI", 8.5F);
        AccessibleRole = AccessibleRole.Link;
        AccessibleName = text;
        AccessibleDefaultActionDescription = "Open";
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleLeft;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Actionable
    {
        get => _actionable;
        set
        {
            if (_actionable == value)
            {
                return;
            }

            _actionable = value;
            TabStop = value;
            Cursor = value ? Cursors.Hand : Cursors.Default;
            AccessibleRole = value ? AccessibleRole.Link : AccessibleRole.StaticText;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Color color = !_actionable
            ? Palette.TextMuted
            : _hover
                ? _danger ? Palette.Danger : Palette.Accent
                : Palette.TextMuted;

        const TextFormatFlags commonFlags =
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding |
            TextFormatFlags.EndEllipsis;

        TextFormatFlags textFlags = TextAlign switch
        {
            ContentAlignment.MiddleCenter => commonFlags | TextFormatFlags.HorizontalCenter,
            ContentAlignment.MiddleRight => commonFlags | TextFormatFlags.Right,
            _ => commonFlags | TextFormatFlags.Left,
        };

        TextRenderer.DrawText(graphics, Text, Font, ClientRectangle, color, textFlags);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(
                graphics,
                new Rectangle(1, 1, Width - 2, Height - 2),
                color,
                Color.Transparent);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        if (_actionable)
        {
            _hover = true;
            Invalidate();
        }

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
        if (_actionable && e.Button == MouseButtons.Left)
        {
            Focus();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_actionable)
        {
            base.OnMouseUp(e);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_actionable && e.KeyCode is Keys.Space or Keys.Enter)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
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
}
