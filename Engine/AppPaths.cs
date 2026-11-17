namespace Volt;

static class AppPaths
{
    static string? rootData;

    public static string RootDataDirectory
    {
        get
        {
            if (rootData != null) return rootData;
            string baseDir = AppContext.BaseDirectory;
            bool isPortable = File.Exists(Path.Combine(baseDir, "portable.txt"))
                           || File.Exists(Path.Combine(baseDir, ".portable"))
                           || Directory.Exists(Path.Combine(baseDir, "Data"));

            rootData = isPortable
                ? Path.Combine(baseDir, "Data")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VOLT");

            return rootData;
        }
    }

    public static string ProfileDirectory => Path.Combine(RootDataDirectory, "Profile");
    public static string ThemeFile => Path.Combine(RootDataDirectory, "theme.txt");
    public static string BrowsingFile => Path.Combine(RootDataDirectory, "browsing.json");
    public static string BookmarkFile => Path.Combine(RootDataDirectory, "bookmarks.json");
    public static string PasswordsFile => Path.Combine(RootDataDirectory, "passwords.dpapi");
}
