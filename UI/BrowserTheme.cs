using System.Runtime.CompilerServices;

namespace Volt;

static class BrowserTheme
{
    sealed record Original(Color Back, Color Fore, Color Border);
    static readonly ConditionalWeakTable<Control, Original> originals = new();
    public static Color Background => Color.FromArgb(24, 30, 39);
    public static Color Surface => Color.FromArgb(35, 43, 54);
    public static Color Text => Color.FromArgb(229, 236, 244);
    public static void Apply(Control control, bool dark)
    {
        if (control is BrowserView) return;
        if (control is ChromeButton painted) painted.DarkTheme=dark;
        if (control is BrowserTabsHeader headerState) headerState.Dark=dark;
        if (control is HomePage homeState) homeState.Dark=dark;
        var original = originals.GetValue(control, c => new(c.BackColor, c.ForeColor,
            c is Button b ? b.FlatAppearance.BorderColor : Color.Empty));
        bool chrome = Equals(control.Tag,"chrome") || control is BrowserTabsHeader;
        bool surface = Equals(control.Tag,"surface") || control is TextBox;
        control.BackColor = dark ? original.Back.A == 0 ? Color.Transparent
            : Equals(control.Tag,"search") ? Color.FromArgb(24,31,41) : chrome ? Background : surface ? Surface : control is ChromeButton ? Color.FromArgb(29,37,49) : Background
            : chrome ? Color.FromArgb(239,244,248) : surface ? Color.White : original.Back;
        control.ForeColor = dark ? Text : original.Fore;
        if (control is Button button) button.FlatAppearance.BorderColor = dark ? Color.FromArgb(70, 86, 104) : original.Border;
        if (control is BrowserTabsHeader header) header.Dark = dark;
        if (control is HomePage home) home.Dark = dark;
        foreach (Control child in control.Controls) Apply(child, dark);
        control.Invalidate();
    }
}

sealed class DarkMenuColors : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => BrowserTheme.Surface;
    public override Color ImageMarginGradientBegin => BrowserTheme.Surface;
    public override Color ImageMarginGradientMiddle => BrowserTheme.Surface;
    public override Color ImageMarginGradientEnd => BrowserTheme.Surface;
    public override Color MenuBorder => Color.FromArgb(70, 86, 104);
    public override Color MenuItemSelected => Color.FromArgb(61, 78, 99);
    public override Color MenuItemBorder => Color.FromArgb(84, 111, 142);
    public override Color CheckBackground => Color.FromArgb(61, 78, 99);
    public override Color CheckSelectedBackground => Color.FromArgb(61, 78, 99);
}
