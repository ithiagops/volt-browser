using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace Volt;

sealed class UpdateInfo
{
    public bool HasUpdate { get; set; }
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public string Title { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public string ReleaseUrl { get; set; } = "";
    public string? DownloadUrl { get; set; }
    public string? AssetName { get; set; }
}

static class UpdateManager
{
    public static readonly string[] Repositories =
    [
        "ToltalTouch/volt-browser",
        "ithiagops/volt-browser"
    ];

    public static Version CurrentVersion
    {
        get
        {
            var ver = typeof(UpdateManager).Assembly.GetName().Version;
            return ver != null ? new Version(ver.Major, ver.Minor, Math.Max(0, ver.Build)) : new Version(0, 6, 8);
        }
    }

    public static string CurrentVersionString => $"{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";

    public static async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        var info = new UpdateInfo
        {
            CurrentVersion = CurrentVersionString
        };

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"VOLT-Browser/{CurrentVersionString} (Windows)");

        foreach (var repo in Repositories)
        {
            try
            {
                string apiUrl = $"https://api.github.com/repos/{repo}/releases/latest";
                using var response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode) continue;

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("tag_name", out var tagElem)) continue;
                string tag = tagElem.GetString() ?? "";
                string cleanTag = tag.TrimStart('v', 'V', ' ');

                if (!Version.TryParse(cleanTag, out var latestVer))
                {
                    // Fallback para versões tipo 0.6.8-beta
                    var parts = cleanTag.Split(new[] { '-', '.', '+' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int maj) && int.TryParse(parts[1], out int min))
                    {
                        int bld = parts.Length >= 3 && int.TryParse(parts[2], out int buildNum) ? buildNum : 0;
                        latestVer = new Version(maj, min, bld);
                    }
                }

                if (latestVer == null) continue;

                info.LatestVersion = cleanTag;
                info.Title = root.TryGetProperty("name", out var n) ? (n.GetString() ?? tag) : tag;
                info.ReleaseNotes = root.TryGetProperty("body", out var bodyElem) ? (bodyElem.GetString() ?? "") : "";
                info.ReleaseUrl = root.TryGetProperty("html_url", out var u) ? (u.GetString() ?? "") : $"https://github.com/{repo}/releases/tag/{tag}";

                if (latestVer > CurrentVersion)
                {
                    info.HasUpdate = true;
                }

                // Procurar arquivo executável ou instalador nos assets
                if (root.TryGetProperty("assets", out var assetsElem) && assetsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsElem.EnumerateArray())
                    {
                        string name = asset.TryGetProperty("name", out var an) ? (an.GetString() ?? "") : "";
                        string dUrl = asset.TryGetProperty("browser_download_url", out var ad) ? (ad.GetString() ?? "") : "";

                        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(dUrl)) continue;

                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                            name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            info.DownloadUrl = dUrl;
                            info.AssetName = name;
                            break;
                        }
                    }
                }

                return info;
            }
            catch
            {
                // Tenta próximo repositório em caso de erro de rede ou rate-limit
            }
        }

        return info;
    }

    public static async Task<string> DownloadUpdateAsync(UpdateInfo info, IProgress<int>? progress = null)
    {
        if (string.IsNullOrEmpty(info.DownloadUrl))
        {
            throw new InvalidOperationException("Nenhum link de download disponível para esta release.");
        }

        string tempFolder = Path.Combine(Path.GetTempPath(), "VOLT_Update");
        Directory.CreateDirectory(tempFolder);

        string fileName = info.AssetName ?? "VOLT-Update.exe";
        string targetFile = Path.Combine(tempFolder, fileName);

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"VOLT-Browser/{CurrentVersionString} (Windows)");

        using var response = await client.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        using var stream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;
            if (totalBytes.HasValue && totalBytes.Value > 0)
            {
                int percent = (int)(totalRead * 100 / totalBytes.Value);
                progress?.Report(percent);
            }
        }

        return targetFile;
    }

    public static void ApplyUpdateAndRestart(string downloadedFilePath)
    {
        string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? "";
        string currentDir = AppContext.BaseDirectory;
        bool isPortable = AppPaths.RootDataDirectory.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase);

        if (downloadedFilePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
            downloadedFilePath.Contains("Setup", StringComparison.OrdinalIgnoreCase))
        {
            // É um instalador NSIS
            Process.Start(new ProcessStartInfo
            {
                FileName = downloadedFilePath,
                UseShellExecute = true
            });
            Environment.Exit(0);
            return;
        }

        if (downloadedFilePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            // Executável direto: cria um script batch para substituir e reiniciar após o fechamento
            string scriptPath = Path.Combine(Path.GetTempPath(), "volt_updater.bat");
            string script = $@"@echo off
timeout /t 1 /nobreak > nul
:retry
copy /y ""{downloadedFilePath}"" ""{currentExe}"" > nul
if errorlevel 1 (
    timeout /t 1 /nobreak > nul
    goto retry
)
start """" ""{currentExe}""
del ""{downloadedFilePath}"" > nul
del ""%~f0"" > nul
";
            File.WriteAllText(scriptPath, script);
            Process.Start(new ProcessStartInfo
            {
                FileName = scriptPath,
                CreateNoWindow = true,
                UseShellExecute = false
            });
            Environment.Exit(0);
            return;
        }

        // Se for ZIP ou outro formato, abre a pasta ou arquivo
        Process.Start(new ProcessStartInfo
        {
            FileName = downloadedFilePath,
            UseShellExecute = true
        });
    }
}
