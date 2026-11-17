namespace Volt;

readonly record struct TabLayout(int Start, int Visible, float Width, bool Overflow)
{
    // The rounded shape is visual only; the target reaches the top of the tab strip.
    public static System.Drawing.RectangleF HitBounds(System.Drawing.RectangleF visual, float stripHeight, float halfGap)
        => visual.Width<=0 || stripHeight<=0 ? System.Drawing.RectangleF.Empty
            : new(visual.Left-halfGap,0,visual.Width+halfGap*2,stripHeight);

    public static TabLayout Calculate(int count, float availableWidth, int start, int selected, bool reveal)
    {
        if (count <= 0) return new(0, 0, 168, false);
        float available = Math.Max(80, availableWidth - 52);
        bool overflow = count * 116 > available;
        if (overflow) available = Math.Max(80, available - 56);
        int visible = overflow ? Math.Clamp((int)(available / 116), 1, count) : count;
        float width = Math.Clamp(available / visible - 4, 76, 198);
        start = Math.Clamp(start, 0, Math.Max(0, count - visible));
        if (reveal && selected >= 0)
        {
            if (selected < start) start = selected;
            if (selected >= start + visible) start = selected - visible + 1;
        }
        return new(start, visible, width, overflow);
    }
}
