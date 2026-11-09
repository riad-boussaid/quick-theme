using System.Drawing.Drawing2D;
using QuickTheme.UI;

namespace QuickTheme.UI.Controls;

internal sealed class ThemeRow : Control
{
    private readonly ToggleSwitch _toggle;
    private readonly Font _titleFont = new("Segoe UI Semibold", 9.75F);
    private readonly Font _subtitleFont = new("Segoe UI", 8.25F);
    private readonly Font _stateFont = new("Segoe UI", 8.25F);
    private readonly string _title;
    private readonly string _subtitle;
    private readonly string _onText;
    private readonly string _offText;
    private bool _hover;

    public event EventHandler? CheckedChanged;

    public ThemeRow(string title, string subtitle, string onText, string offText)
    {
        _title = title;
        _subtitle = subtitle;
        _onText = onText;
        _offText = offText;

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);

        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = false;
        AccessibleName = $"{title}: {subtitle}";
        AccessibleRole = AccessibleRole.Grouping;

        _toggle = new ToggleSwitch
        {
            AccessibleName = title,
            AccessibleDescription = subtitle,
            TabStop = true,
        };
        _toggle.CheckedChanged += (_, _) => CheckedChanged?.Invoke(this, EventArgs.Empty);
        _toggle.MouseEnter += (_, _) => UpdateHover();
        _toggle.MouseLeave += (_, _) => UpdateHover();
        Controls.Add(_toggle);
    }

    public bool Checked => _toggle.Checked;

    public void SetCheckedSilently(bool value) => _toggle.SetChecked(value, animate: false);

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        int width = Scale(44);
        int height = Scale(24);
        _toggle.Size = new Size(width, height);
        _toggle.Location = new Point(Width - Scale(14) - width, (Height - height) / 2);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int pad = Scale(14);

        if (_hover)
        {
            var hoverRect = new Rectangle(Scale(4), 0, Width - Scale(8), Height - 1);
            using var hoverPath = Ui.RoundedRect(hoverRect, Scale(9));
            using var hoverBrush = new SolidBrush(Palette.RowHover);
            g.FillPath(hoverBrush, hoverPath);
        }

        using (var separator = new Pen(Palette.Separator))
        {
            g.DrawLine(separator, pad, Height - 1, Width - pad, Height - 1);
        }

        int textWidth = Math.Max(0, _toggle.Left - pad - Scale(8));
        var titleRect = new Rectangle(pad, Scale(8), textWidth, Scale(20));
        var subtitleRect = new Rectangle(pad, Scale(28), textWidth, Scale(18));
        const TextFormatFlags textFlags =
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding |
            TextFormatFlags.EndEllipsis;

        TextRenderer.DrawText(g, _title, _titleFont, titleRect, Palette.Text, textFlags);
        TextRenderer.DrawText(g, _subtitle, _subtitleFont, subtitleRect, Palette.TextMuted, textFlags);

        string state = Checked ? _onText : _offText;
        Size stateSize = TextRenderer.MeasureText(g, state, _stateFont, Size.Empty, TextFormatFlags.NoPadding);
        var stateRect = new Rectangle(
            _toggle.Left - Scale(10) - stateSize.Width,
            (Height - stateSize.Height) / 2,
            stateSize.Width,
            stateSize.Height);

        TextRenderer.DrawText(
            g,
            state,
            _stateFont,
            stateRect,
            Checked ? Palette.Accent : Palette.TextMuted,
            TextFormatFlags.NoPadding | TextFormatFlags.Left);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _toggle.Focus();
            _toggle.Checked = !_toggle.Checked;
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        UpdateHover();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        UpdateHover();
        base.OnMouseLeave(e);
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        PerformLayout();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleFont.Dispose();
            _subtitleFont.Dispose();
            _stateFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateHover()
    {
        bool hover = ClientRectangle.Contains(PointToClient(Cursor.Position));
        if (hover == _hover)
        {
            return;
        }

        _hover = hover;
        Invalidate();
    }

    private int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96.0);
}
