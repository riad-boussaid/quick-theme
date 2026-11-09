using System.Drawing.Drawing2D;
using QuickTheme.UI;

namespace QuickTheme.UI.Controls;

internal sealed class SettingsNavButton : Control
{
    private bool _selected;
    private bool _hover;

    public SettingsNavButton(string text)
    {
        Text = text;

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        Height = 36;
        Cursor = Cursors.Hand;
        TabStop = true;
        Font = new Font("Segoe UI", 9.5F);
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = text;
    }

    public void SetSelected(bool selected)
    {
        if (_selected == selected)
        {
            return;
        }

        _selected = selected;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (_selected || _hover)
        {
            using var path = Ui.RoundedRect(new Rectangle(0, 0, Width, Height), 7f);
            using var brush = new SolidBrush(Palette.RowHover);
            g.FillPath(brush, path);
        }

        if (_selected)
        {
            using var accentBrush = new SolidBrush(Palette.Accent);
            using var accentPath = Ui.RoundedRect(new Rectangle(4, (Height - 16) / 2, 3, 16), 1.5f);
            g.FillPath(accentBrush, accentPath);
        }

        var textRect = new Rectangle(_selected ? 20 : 16, 0, Width - 28, Height);
        TextRenderer.DrawText(
            g,
            Text,
            Font,
            textRect,
            _selected ? Palette.Text : Palette.TextMuted,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding |
            TextFormatFlags.EndEllipsis);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(
                g,
                new Rectangle(2, 2, Width - 4, Height - 4),
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
}
