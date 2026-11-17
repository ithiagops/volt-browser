namespace Volt;

static class MediaGuard
{
    static readonly string Source = ReadSource();
    static string ReadSource()
    {
        using var stream = typeof(MediaGuard).Assembly.GetManifestResourceStream("Volt.MediaGuard.js")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    public static string Script(bool enabled, bool background, long revision)
        => $"({Source})({(enabled ? "true" : "false")},{(background ? "true" : "false")},{revision});";

    public static string Update(bool enabled, bool background, long revision)
        => $"window.__voltMediaPolicy?.update?.({(enabled ? "true" : "false")},{(background ? "true" : "false")},{revision});";
}
