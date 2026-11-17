using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace Volt;

sealed partial class BrowserWindow
{
    sealed class BrowsingPreferences
    {
        public int SleepMinutes { get; set; } = 5;
        public int PriorityTabs { get; set; } = 0;
        public List<string> History { get; set; } = new();
    }
    BrowsingPreferences browsing = new();
    AddressSuggestions? addressSuggestions;
    int sleepMinutes => browsing.SleepMinutes;
    string BrowsingFile => AppPaths.BrowsingFile;

    void LoadBrowsingPreferences()
    {
        try
        {
            if(File.Exists(BrowsingFile) && new FileInfo(BrowsingFile).Length<=1_500_000)
                browsing=JsonSerializer.Deserialize<BrowsingPreferences>(File.ReadAllText(BrowsingFile)) ?? new();
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException) { }
        if(!new[]{0,1,3,5,10,15,30,60}.Contains(browsing.SleepMinutes))browsing.SleepMinutes=5;
        browsing.PriorityTabs=Math.Clamp(browsing.PriorityTabs,0,100);
        browsing.History=(browsing.History ?? new()).Where(SafeSuggestion).Distinct(StringComparer.OrdinalIgnoreCase).Take(200).ToList();
    }

    void SaveBrowsingPreferences()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BrowsingFile)!);
            string temp=BrowsingFile+".tmp";
            File.WriteAllText(temp,JsonSerializer.Serialize(browsing));File.Move(temp,BrowsingFile,true);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
        { status.Text="Preferência aplicada, mas não foi possível salvá-la."; }
    }

    static bool SafeSuggestion(string url) => url is not null && url.Length<=2048 && ResourcePolicy.IsHttp(url)
        && Uri.TryCreate(url,UriKind.Absolute,out var uri) && uri.UserInfo.Length==0;

    void RefreshAddressSuggestions()
    {
        var values=bookmarks.Select(b=>b.Url).Concat(browsing.History).Where(SafeSuggestion)
            .SelectMany(url=>new[]{url,url[(url.IndexOf("://",StringComparison.Ordinal)+3)..]})
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(500).ToArray();
        addressSuggestions?.SetValues(values);
    }

    void RememberAddress(string url)
    {
        if(!SafeSuggestion(url))return;
        browsing.History.RemoveAll(u=>u.Equals(url,StringComparison.OrdinalIgnoreCase));
        browsing.History.Insert(0,url);
        if(browsing.History.Count>200)browsing.History.RemoveRange(200,browsing.History.Count-200);
        SaveBrowsingPreferences();RefreshAddressSuggestions();
    }

    bool HasPositionPriority(BrowserTab tab) => ResourcePolicy.PositionPriority(tabs.TabPages.IndexOf(tab.Page),browsing.PriorityTabs);

    void ResumePriorityTabs()
    {
        foreach(var tab in items.Values.Where(HasPositionPriority))
        {
            if(tab.Ready && tab.View!.CoreWebView2.IsSuspended)
            {
                tab.View.CoreWebView2.Resume();
                tab.SleepResult = "Mantida ativa por prioridade";
            }
            else if(tab.Unloaded && tab.Url is { } url)
            {
                _ = NavigateTab(tab,url);
            }
        }
    }

    void SetSleepMinutes(int minutes)
    {
        browsing.SleepMinutes = minutes;
        SaveBrowsingPreferences();
        UpdateModernMenuValues();
        foreach (var tab in items.Values)
            if (tab.Ready && tab.View!.CoreWebView2.IsSuspended) { tab.View.CoreWebView2.Resume(); tab.HiddenSince = DateTime.UtcNow; }
        status.Text = minutes > 0 ? $"Tempo para modo de economia: {minutes} minuto(s)." : "Modo de economia por tempo desativado.";
    }

    void SetPriorityTabs(int count)
    {
        browsing.PriorityTabs = Math.Clamp(count, 0, 100);
        SaveBrowsingPreferences();
        UpdateModernMenuValues();
        ResumePriorityTabs();
        UpdateChrome();
        status.Text = browsing.PriorityTabs > 0
            ? $"Prioridade: as primeiras {browsing.PriorityTabs} abas não entrarão em modo de economia."
            : "Modo de economia configurado para todas as abas elegíveis.";
    }

    void BuildBrowserMenu(
        ContextMenuStrip popup,
        Button menuButton,
        ToolStripMenuItem dark,
        ToolStripMenuItem saver,
        ToolStripMenuItem blockers,
        ToolStripMenuItem mediaPolicy,
        ToolStripMenuItem deep,
        ToolStripMenuItem automatic)
    {
        address.AutoCompleteMode = AutoCompleteMode.None;
        address.AutoCompleteSource = AutoCompleteSource.None;
        addressSuggestions = new AddressSuggestions(this, address, Navigate, darkMode);
        RefreshAddressSuggestions();

        InitializeModernToolsMenu(menuButton);
    }

    void ClearFavicon(BrowserTab tab)
    {
        tab.IconRevision++;tab.Favicon?.Dispose();tab.Favicon=null;browserTabHeader?.Invalidate();
    }

    async Task UpdateFavicon(BrowserTab tab,BrowserView view)
    {
        int revision=++tab.IconRevision;
        try
        {
            using var stream=await view.CoreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
            if (stream == null) return;
            using var bytes=new MemoryStream();var buffer=new byte[8192];int read;
            while((read=await stream.ReadAsync(buffer))>0)
            {if(bytes.Length+read>1_048_576)return;bytes.Write(buffer,0,read);}
            if(closing || tab.View!=view || revision!=tab.IconRevision || !items.ContainsKey(tab.Page) || bytes.Length==0)return;
            bytes.Position=0;using var image=Image.FromStream(bytes);
            var highRes=new Bitmap(32,32);
            using(var g=Graphics.FromImage(highRes))
            {
                g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(image,0,0,32,32);
            }
            tab.Favicon?.Dispose();tab.Favicon=highRes;browserTabHeader?.Invalidate();
        }
        catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException or ArgumentException or IOException or ObjectDisposedException) { }
    }
}
