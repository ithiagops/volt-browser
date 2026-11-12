namespace Volt;

// The page always keeps the full viewport. Only the overlaid panel is resized.
static class SidebarLayout
{
    public static int Width(int viewport,int preferred)
        => Math.Clamp(preferred,Math.Min(340,Math.Max(0,viewport-265)),Math.Max(0,viewport-265));
}
