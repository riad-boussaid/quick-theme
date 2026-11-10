using System.Drawing.Drawing2D;
using QuickTheme.UI;

namespace QuickTheme.UI.Controls;

internal sealed class PillButton : Control
{
    private readonly bool _primary;
    private bool _hover;

    public PillButton(string text, bool primary = false)
    {
        Text = text;
        _primary = primary;

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        Height = 30;
        Cursor = Cursors.Hand;
        TabStop = true;
        Font = new Font("Segoe UI", 8.75F);
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = text;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color background = !Enabled
            ? Palette.MenuSurface
            : _primary
                ? (_hover ? ControlPaint.Light(Palette.Accent, 0.10f) : Palette.Accent)
                : (_hover ? Palette.RowHover : Palette.MenuHover);

        var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (var path = Ui.RoundedRect(bounds, Height / 2f))
        using (var brush = new SolidBrush(background))
        {
            g.FillPath(brush, path);
        }

        if (!_primary)
        {
            using var border = new Pen(Palette.Border);
            using var path = Ui.RoundedRect(bounds, Height / 2f);
            g.DrawPath(border, path);
        }

        Color textColor = !Enabled
            ? Palette.TextDisabled
            : _primary
                ? ContrastText(background)
                : Palette.Text;

        TextRenderer.DrawText(
            g,
            Text,
            Font,
            ClientRectangle,
            textColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding |
            TextFormatFlags.EndEllipsis);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(
                g,
                new Rectangle(2, 2, Width - 4, Height - 4),
                textColor,
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

    private static Color ContrastText(Color background)
    {
        int brightness = (background.R * 299 + background.G * 587 + background.B * 114) / 1000;
        return brightness > 140 ? Color.FromArgb(20, 20, 20) : Color.White;
    }
}
