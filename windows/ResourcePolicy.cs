namespace Volt;

static class ResourcePolicy
{
    public static readonly string[] TrackerDomains =
    ["google-analytics.com", "googletagmanager.com", "doubleclick.net", "hotjar.com", "hotjar.io", "clarity.ms"];

    public static bool MemoryPressure(bool active, long bytes)
        => active ? bytes > 1_000_000_000L : bytes > 1_200_000_000L;

    public static bool IsHttp(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == "https" || uri.Scheme == "http");

    public static string Address(string input)
    {
        var value = input.Trim();
        if (IsHttp(value)) return value;
        if (!value.Any(char.IsWhiteSpace) && (value.Contains('.') || value.StartsWith("localhost")) && IsHttp("https://" + value)) return "https://" + value;
        return "https://www.google.com/search?q=" + Uri.EscapeDataString(value);
    }

    public static bool IsTracker(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && TrackerDomains.Any(domain => uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));

    public static bool CanSleep(bool selected, bool ready, bool loading, bool pinned, int downloads, bool audio)
        => !selected && ready && !loading && !pinned && downloads == 0 && !audio;

    public static int NewTabIndex(int selectedIndex, int count)
        => Math.Clamp(selectedIndex + 1, 0, count);

    public static int MoveTabIndex(int source, int slot, int count)
        => Math.Clamp(slot > source ? slot - 1 : slot, 0, Math.Max(0, count - 1));

    public static string SleepProtection(bool selected, bool ready, bool loading, bool pinned, int downloads, bool audio)
    {
        if (selected) return "Em uso";
        if (!ready) return "Sem página carregada";
        if (pinned) return "Manter ativa";
        if (downloads > 0) return "Download em andamento";
        if (audio) return "Áudio em reprodução";
        if (loading) return "Carregando página";
        return "";
    }

    public static bool ShouldUnload(bool enabled, TimeSpan inactive, bool eligible, int loadedPages = 0)
        => enabled && eligible && (inactive >= TimeSpan.FromMinutes(3)
            || loadedPages > 2 && inactive >= TimeSpan.FromSeconds(45));
}
