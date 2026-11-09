using System.Drawing.Drawing2D;

namespace QuickTheme.UI;

internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Palette.MenuSurface);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled)
        {
            return;
        }

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // The renderer's graphics is already translated to the item's top-left.
        var rect = new Rectangle(3, 1, e.Item.Width - 6, e.Item.Height - 2);

        using var path = Ui.RoundedRect(rect, 6);
        using var brush = new SolidBrush(Palette.MenuHover);
        g.FillPath(brush, path);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled
            ? (e.Item.Selected ? Palette.Text : Palette.TextMuted)
            : Palette.TextDisabled;

        base.OnRenderItemText(e);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Palette.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
    }
}

internal sealed class DarkColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Palette.MenuSurface;
    public override Color MenuBorder => Palette.Border;
    public override Color MenuItemBorder => Palette.MenuHover;
    public override Color MenuItemSelected => Palette.MenuHover;
    public override Color MenuItemSelectedGradientBegin => Palette.MenuHover;
    public override Color MenuItemSelectedGradientEnd => Palette.MenuHover;
    public override Color MenuItemPressedGradientBegin => Palette.MenuHover;
    public override Color MenuItemPressedGradientEnd => Palette.MenuHover;
    public override Color ImageMarginGradientBegin => Palette.MenuSurface;
    public override Color ImageMarginGradientMiddle => Palette.MenuSurface;
    public override Color ImageMarginGradientEnd => Palette.MenuSurface;
    public override Color CheckBackground => Palette.MenuSurface;
    public override Color CheckPressedBackground => Palette.MenuSurface;
    public override Color CheckSelectedBackground => Palette.MenuSurface;
    public override Color SeparatorDark => Palette.Separator;
    public override Color SeparatorLight => Palette.Separator;
}
