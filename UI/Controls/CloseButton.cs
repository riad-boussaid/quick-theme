using System.Drawing.Drawing2D;
using QuickTheme.UI;

namespace QuickTheme.UI.Controls;

internal sealed class CloseButton : Control
{
    private bool _hover;

    public CloseButton()
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
        AccessibleName = "Close";
        AccessibleRole = AccessibleRole.PushButton;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (_hover)
        {
            using var hoverPath = Ui.RoundedRect(new Rectangle(0, 0, Width, Height), Width / 2);
            using var hoverBrush = new SolidBrush(Palette.RowHover);
            g.FillPath(hoverBrush, hoverPath);
        }

        float inset = Width * 0.34f;
        using var pen = new Pen(_hover ? Palette.Text : Palette.TextMuted, Math.Max(1.4f, Width / 18f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };

        g.DrawLine(pen, inset, inset, Width - inset, Height - inset);
        g.DrawLine(pen, Width - inset, inset, inset, Height - inset);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(g, new Rectangle(1, 1, Width - 2, Height - 2), Palette.Text, Color.Transparent);
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
