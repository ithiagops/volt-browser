namespace Volt;

static class SiteCatalog
{
    public static readonly string[] PopularSites =
    [
        "google.com",
        "youtube.com",
        "github.com",
        "globo.com",
        "g1.globo.com",
        "uol.com.br",
        "instagram.com",
        "facebook.com",
        "twitter.com",
        "x.com",
        "web.whatsapp.com",
        "whatsapp.com",
        "netflix.com",
        "amazon.com.br",
        "amazon.com",
        "mercadolivre.com.br",
        "wikipedia.org",
        "reddit.com",
        "chatgpt.com",
        "openai.com",
        "linkedin.com",
        "twitch.tv",
        "discord.com",
        "spotify.com",
        "microsoft.com",
        "bing.com",
        "tiktok.com",
        "yahoo.com",
        "aliexpress.com",
        "shopee.com.br",
        "magazineluiza.com.br",
        "canva.com",
        "notion.so",
        "figma.com",
        "stackoverflow.com",
        "gitlab.com",
        "cnnbrasil.com.br",
        "tecmundo.com.br",
        "olx.com.br",
        "medium.com",
        "pinterest.com",
        "apple.com"
    ];

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        string val = input.Trim();
        if (val.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) val = val[8..];
        else if (val.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) val = val[7..];
        if (val.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) val = val[4..];
        return val.TrimEnd('/');
    }

    public static string? BestMatch(string query, IEnumerable<string>? extraSources = null)
    {
        string q = Normalize(query);
        if (string.IsNullOrEmpty(q)) return null;

        if (extraSources != null)
        {
            foreach (var item in extraSources)
            {
                string norm = Normalize(item);
                if (norm.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                    return norm;
            }
        }

        foreach (var site in PopularSites)
        {
            if (site.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                return site;
        }

        if (!q.Any(char.IsWhiteSpace) && !q.Contains('.'))
        {
            foreach (var site in PopularSites)
            {
                string nameOnly = site.Split('.')[0];
                if (nameOnly.Equals(q, StringComparison.OrdinalIgnoreCase))
                    return site;
            }
            return q + ".com";
        }

        return null;
    }

    public static List<string> FindCompletions(string query, IEnumerable<string>? extraSources = null, int max = 8)
    {
        var results = new List<string>();
        string q = Normalize(query);
        if (string.IsNullOrEmpty(q)) return results;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Extra sources (bookmarks / history) prefix matches
        if (extraSources != null)
        {
            foreach (var item in extraSources)
            {
                string norm = Normalize(item);
                if (norm.Length > 0 && norm.StartsWith(q, StringComparison.OrdinalIgnoreCase) && seen.Add(norm))
                {
                    results.Add(norm);
                    if (results.Count >= max) return results;
                }
            }
        }

        // 2. Popular sites prefix matches
        foreach (var site in PopularSites)
        {
            if (site.StartsWith(q, StringComparison.OrdinalIgnoreCase) && seen.Add(site))
            {
                results.Add(site);
                if (results.Count >= max) return results;
            }
        }

        // 3. Domain extension synthesis if single word without dot
        if (!q.Any(char.IsWhiteSpace) && !q.Contains('.'))
        {
            string com = q + ".com";
            if (seen.Add(com)) results.Add(com);
            if (results.Count >= max) return results;

            string comBr = q + ".com.br";
            if (seen.Add(comBr)) results.Add(comBr);
            if (results.Count >= max) return results;
        }

        // 4. Substring matches from extra sources
        if (extraSources != null)
        {
            foreach (var item in extraSources)
            {
                string norm = Normalize(item);
                if (norm.Length > 0 && norm.Contains(q, StringComparison.OrdinalIgnoreCase) && seen.Add(norm))
                {
                    results.Add(norm);
                    if (results.Count >= max) return results;
                }
            }
        }

        // 5. Substring matches from popular sites
        foreach (var site in PopularSites)
        {
            if (site.Contains(q, StringComparison.OrdinalIgnoreCase) && seen.Add(site))
            {
                results.Add(site);
                if (results.Count >= max) return results;
            }
        }

        return results;
    }
}
