using System.ComponentModel;
using System.Drawing.Drawing2D;
using QuickTheme.UI;

namespace QuickTheme.UI.Controls;

internal sealed class SegmentedControl : Control
{
    private string[] _options = [];
    private int _selectedIndex;
    private int _hoverIndex = -1;

    public SegmentedControl()
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
        Height = 30;
        TabStop = true;
        AccessibleRole = AccessibleRole.Grouping;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string[] Options
    {
        get => _options;
        set
        {
            _options = value ?? [];
            _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, _options.Length - 1));
            UpdateAccessibleDescription();
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = Math.Clamp(value, 0, Math.Max(0, _options.Length - 1));
            if (clamped == _selectedIndex)
            {
                return;
            }

            _selectedIndex = clamped;
            UpdateAccessibleDescription();
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? SelectedIndexChanged;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        if (_options.Length == 0 || Width <= 2 || Height <= 2)
        {
            return;
        }

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var track = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        Color trackColor = Palette.IsHighContrast
            ? Palette.TrackOff
            : Palette.IsLight
                ? Color.FromArgb(231, 231, 238)
                : Color.FromArgb(44, 44, 54);

        using (var trackPath = Ui.RoundedRect(track, 8f))
        using (var trackBrush = new SolidBrush(trackColor))
        {
            g.FillPath(trackBrush, trackPath);
        }

        float segmentWidth = (Width - 4f) / _options.Length;
        const TextFormatFlags textFlags =
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding |
            TextFormatFlags.EndEllipsis;

        for (int index = 0; index < _options.Length; index++)
        {
            var segment = new RectangleF(
                2f + segmentWidth * index,
                2f,
                segmentWidth,
                Height - 4f);

            if (index == _selectedIndex)
            {
                using var selectedPath = Ui.RoundedRect(segment, 6f);
                using var selectedBrush = new SolidBrush(Palette.Accent);
                g.FillPath(selectedBrush, selectedPath);
            }
            else if (index == _hoverIndex)
            {
                using var hoverPath = Ui.RoundedRect(segment, 6f);
                using var hoverBrush = new SolidBrush(Palette.RowHover);
                g.FillPath(hoverBrush, hoverPath);
            }

            Color textColor = index == _selectedIndex
                ? ContrastText(Palette.Accent)
                : Palette.Text;

            TextRenderer.DrawText(
                g,
                _options[index],
                Font,
                Rectangle.Round(segment),
                textColor,
                textFlags);
        }

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(
                g,
                new Rectangle(1, 1, Width - 2, Height - 2),
                Palette.Text,
                Color.Transparent);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int hit = HitTest(e.Location);
        if (hit != _hoverIndex)
        {
            _hoverIndex = hit;
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (_hoverIndex != -1)
        {
            _hoverIndex = -1;
            Invalidate();
        }

        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Focus();
            int hit = HitTest(e.Location);
            if (hit >= 0)
            {
                SelectedIndex = hit;
            }
        }

        base.OnMouseDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Left or Keys.Up)
        {
            SelectedIndex = Math.Max(0, _selectedIndex - 1);
            e.Handled = true;
        }
        else if (e.KeyCode is Keys.Right or Keys.Down)
        {
            SelectedIndex = Math.Min(_options.Length - 1, _selectedIndex + 1);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    private void UpdateAccessibleDescription()
    {
        AccessibleDescription = _options.Length == 0
            ? null
            : $"Selected: {_options[_selectedIndex]}";
    }

    private int HitTest(Point point)
    {
        if (point.X < 0 || point.X >= Width || point.Y < 0 || point.Y >= Height)
        {
            return -1;
        }

        float segmentWidth = (Width - 4f) / Math.Max(1, _options.Length);
        int index = (int)((point.X - 2f) / segmentWidth);
        return Math.Clamp(index, 0, _options.Length - 1);
    }

    private static Color ContrastText(Color background)
    {
        int brightness = (background.R * 299 + background.G * 587 + background.B * 114) / 1000;
        return brightness > 140 ? Color.FromArgb(20, 20, 20) : Color.White;
    }
}
