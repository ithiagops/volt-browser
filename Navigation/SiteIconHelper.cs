using System.Drawing.Drawing2D;

namespace Volt;

static class SiteIconHelper
{
    static readonly Dictionary<string, Image> cache = new(StringComparer.OrdinalIgnoreCase);
    static Image? homeIcon;

    public static Image GetIconForUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url == "about:blank")
        {
            return GetHomeIcon();
        }

        string domain = GetDomain(url);
        if (string.IsNullOrEmpty(domain)) return GetHomeIcon();

        lock (cache)
        {
            if (cache.TryGetValue(domain, out var cached)) return cached;

            Image created = CreateBrandOrDomainIcon(domain);
            cache[domain] = created;
            return created;
        }
    }

    public static string GetDomain(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        try
        {
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                string host = uri.Host.ToLowerInvariant();
                if (host.StartsWith("www.")) host = host[4..];
                return host;
            }
        }
        catch { }
        return "";
    }

    public static Image GetHomeIcon()
    {
        if (homeIcon != null) return homeIcon;
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // Clean circular cyan badge with lightning bolt
        using var brush = new SolidBrush(Color.FromArgb(14, 165, 233));
        g.FillEllipse(brush, 2, 2, 28, 28);

        // White lightning bolt
        using var bolt = new SolidBrush(Color.White);
        PointF[] pts =
        [
            new(17, 6),
            new(9, 17),
            new(15, 17),
            new(13, 26),
            new(23, 15),
            new(17, 15)
        ];
        g.FillPolygon(bolt, pts);
        homeIcon = bmp;
        return homeIcon;
    }

    static Image CreateBrandOrDomainIcon(string domain)
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        if (domain.Contains("youtube.com") || domain.Contains("youtu.be"))
        {
            // YouTube: Iconic Red rounded rect with white play triangle
            using var bg = new SolidBrush(Color.FromArgb(255, 0, 0));
            using var path = CreateRoundedRect(new RectangleF(2, 6, 28, 20), 5);
            g.FillPath(bg, path);

            // White play triangle
            using var playBrush = new SolidBrush(Color.White);
            PointF[] tri = [new(13, 11), new(21, 16), new(13, 21)];
            g.FillPolygon(playBrush, tri);
            return bmp;
        }

        if (domain.Contains("google.com"))
        {
            // Google: Clean white circular background with Google colored G
            using var bg = new SolidBrush(Color.White);
            g.FillEllipse(bg, 2, 2, 28, 28);
            using var border = new Pen(Color.FromArgb(220, 224, 230), 1);
            g.DrawEllipse(border, 2, 2, 28, 28);

            using var font = new Font("Segoe UI", 16, FontStyle.Bold, GraphicsUnit.Pixel);
            TextRenderer.DrawText(g, "G", font, new Rectangle(2, 2, 28, 28), Color.FromArgb(66, 133, 244),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            return bmp;
        }

        if (domain.Contains("github.com"))
        {
            // GitHub: Dark badge with white silhouette
            using var bg = new SolidBrush(Color.FromArgb(36, 41, 47));
            g.FillEllipse(bg, 2, 2, 28, 28);

            using var white = new SolidBrush(Color.White);
            g.FillEllipse(white, 8, 11, 16, 14);
            // Ears
            PointF[] leftEar = [new(8, 13), new(11, 7), new(14, 11)];
            PointF[] rightEar = [new(18, 11), new(21, 7), new(24, 13)];
            g.FillPolygon(white, leftEar);
            g.FillPolygon(white, rightEar);
            return bmp;
        }

        if (domain.Contains("whatsapp.com"))
        {
            // WhatsApp: Green badge with white chat bubble
            using var bg = new SolidBrush(Color.FromArgb(37, 211, 102));
            g.FillEllipse(bg, 2, 2, 28, 28);

            using var bubble = new SolidBrush(Color.White);
            g.FillEllipse(bubble, 7, 7, 18, 16);
            PointF[] tail = [new(9, 21), new(6, 25), new(13, 22)];
            g.FillPolygon(bubble, tail);
            return bmp;
        }

        if (domain.Contains("netflix.com"))
        {
            // Netflix: Black badge with red bold N
            using var bg = new SolidBrush(Color.FromArgb(20, 20, 20));
            using var path = CreateRoundedRect(new RectangleF(2, 2, 28, 28), 5);
            g.FillPath(bg, path);

            using var font = new Font("Segoe UI", 17, FontStyle.Bold, GraphicsUnit.Pixel);
            TextRenderer.DrawText(g, "N", font, new Rectangle(2, 2, 28, 28), Color.FromArgb(229, 9, 20),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            return bmp;
        }

        if (domain.Contains("wikipedia.org"))
        {
            // Wikipedia: White badge with bold W
            using var bg = new SolidBrush(Color.White);
            g.FillEllipse(bg, 2, 2, 28, 28);
            using var border = new Pen(Color.FromArgb(200, 205, 215), 1);
            g.DrawEllipse(border, 2, 2, 28, 28);

            using var font = new Font("Georgia", 16, FontStyle.Bold, GraphicsUnit.Pixel);
            TextRenderer.DrawText(g, "W", font, new Rectangle(2, 2, 28, 28), Color.FromArgb(30, 30, 30),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            return bmp;
        }

        if (domain.Contains("reddit.com"))
        {
            // Reddit: Orange circle with smile
            using var bg = new SolidBrush(Color.FromArgb(255, 69, 0));
            g.FillEllipse(bg, 2, 2, 28, 28);

            using var white = new SolidBrush(Color.White);
            g.FillEllipse(white, 8, 10, 16, 12);
            return bmp;
        }

        if (domain.Contains("twitter.com") || domain == "x.com")
        {
            // X: Black badge with white X
            using var bg = new SolidBrush(Color.Black);
            using var path = CreateRoundedRect(new RectangleF(2, 2, 28, 28), 5);
            g.FillPath(bg, path);

            using var font = new Font("Segoe UI", 16, FontStyle.Bold, GraphicsUnit.Pixel);
            TextRenderer.DrawText(g, "X", font, new Rectangle(2, 2, 28, 28), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            return bmp;
        }

        if (domain.Contains("facebook.com"))
        {
            // Facebook: Blue badge with white f
            using var bg = new SolidBrush(Color.FromArgb(24, 119, 242));
            using var path = CreateRoundedRect(new RectangleF(2, 2, 28, 28), 5);
            g.FillPath(bg, path);

            using var font = new Font("Segoe UI", 18, FontStyle.Bold, GraphicsUnit.Pixel);
            TextRenderer.DrawText(g, "f", font, new Rectangle(4, 2, 28, 28), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            return bmp;
        }

        if (domain.Contains("instagram.com"))
        {
            // Instagram: Pink/Purple badge with camera
            using var bg = new SolidBrush(Color.FromArgb(225, 48, 108));
            using var path = CreateRoundedRect(new RectangleF(2, 2, 28, 28), 6);
            g.FillPath(bg, path);

            using var pen = new Pen(Color.White, 2);
            using var camPath = CreateRoundedRect(new RectangleF(8, 8, 16, 16), 4);
            g.DrawPath(pen, camPath);
            g.DrawEllipse(pen, 13, 13, 6, 6);
            return bmp;
        }

        if (domain.Contains("spotify.com"))
        {
            // Spotify: Green circle with sound wave
            using var bg = new SolidBrush(Color.FromArgb(29, 185, 84));
            g.FillEllipse(bg, 2, 2, 28, 28);

            using var pen = new Pen(Color.White, 2);
            g.DrawArc(pen, 8, 9, 16, 14, -150, 80);
            g.DrawArc(pen, 10, 13, 12, 10, -150, 80);
            return bmp;
        }

        if (domain.Contains("chatgpt.com") || domain.Contains("openai.com"))
        {
            // ChatGPT: Teal badge
            using var bg = new SolidBrush(Color.FromArgb(16, 163, 127));
            using var path = CreateRoundedRect(new RectangleF(2, 2, 28, 28), 5);
            g.FillPath(bg, path);

            using var white = new SolidBrush(Color.White);
            g.FillEllipse(white, 10, 10, 12, 12);
            return bmp;
        }

        if (domain.Contains("globo.com") || domain.Contains("g1.globo.com"))
        {
            // Globo: Clean blue circle with inner white circle
            using var bg = new SolidBrush(Color.FromArgb(6, 105, 255));
            g.FillEllipse(bg, 2, 2, 28, 28);
            using var white = new SolidBrush(Color.White);
            g.FillEllipse(white, 9, 9, 14, 14);
            using var blue = new SolidBrush(Color.FromArgb(6, 105, 255));
            g.FillEllipse(blue, 12, 12, 8, 8);
            return bmp;
        }

        // Generic domain: Generate a clean pastel badge with the domain's first letter
        Color color = GetColorForDomain(domain);
        using (var bg = new SolidBrush(color))
        {
            using var path = CreateRoundedRect(new RectangleF(2, 2, 28, 28), 6);
            g.FillPath(bg, path);
        }

        char initial = char.ToUpperInvariant(domain.FirstOrDefault(c => char.IsLetterOrDigit(c)));
        if (initial == '\0') initial = 'W';

        using (var font = new Font("Segoe UI", 15, FontStyle.Bold, GraphicsUnit.Pixel))
        {
            TextRenderer.DrawText(g, initial.ToString(), font, new Rectangle(2, 2, 28, 28), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        return bmp;
    }

    static Color GetColorForDomain(string domain)
    {
        int hash = Math.Abs(domain.GetHashCode());
        Color[] palette =
        [
            Color.FromArgb(59, 130, 246),  // Blue
            Color.FromArgb(16, 185, 129),  // Emerald
            Color.FromArgb(245, 158, 11),  // Amber
            Color.FromArgb(239, 68, 68),   // Red
            Color.FromArgb(139, 92, 246),  // Violet
            Color.FromArgb(236, 72, 153),  // Pink
            Color.FromArgb(20, 184, 166),  // Teal
            Color.FromArgb(99, 102, 241)   // Indigo
        ];
        return palette[hash % palette.Length];
    }

    static GraphicsPath CreateRoundedRect(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
