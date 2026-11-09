using System.ComponentModel;
using System.Drawing.Drawing2D;
using QuickTheme.UI;

namespace QuickTheme.UI.Controls;

internal sealed class RoundedTextBox : Control
{
    private readonly TextBox _inner;
    private bool _focused;

    public RoundedTextBox()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;

        _inner = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Palette.MenuSurface,
            ForeColor = Palette.Text,
            Font = new Font("Segoe UI", 9F),
            AutoSize = false,
        };
        _inner.GotFocus += (_, _) =>
        {
            _focused = true;
            Invalidate();
        };
        _inner.LostFocus += (_, _) =>
        {
            _focused = false;
            Invalidate();
        };
        Controls.Add(_inner);
        Height = 30;
        PerformLayout();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox InnerTextBox => _inner;

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = Ui.RoundedRect(bounds, 7f);
        using var fill = new SolidBrush(Palette.MenuSurface);
        g.FillPath(fill, path);

        using var border = new Pen(_focused ? Palette.Accent : Palette.Border);
        g.DrawPath(border, path);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        if (_inner is null)
        {
            return;
        }

        int inset = Math.Max(8, Width / 35);
        int textHeight = _inner.PreferredHeight;
        _inner.SetBounds(inset, (Height - textHeight) / 2, Width - inset * 2, textHeight);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!_inner.Focused)
        {
            _inner.Focus();
        }

        base.OnMouseDown(e);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);

        if (_inner is not null)
        {
            _inner.Font = Font;
            PerformLayout();
        }
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        _inner.Enabled = Enabled;
        Invalidate();
    }
}
