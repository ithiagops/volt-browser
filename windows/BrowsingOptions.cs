using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace Volt;

sealed partial class BrowserWindow
{
    sealed class BrowsingPreferences
    {
        public int SleepMinutes { get; set; } = 5;
        public int PriorityTabs { get; set; }
        public List<string> History { get; set; } = new();
    }
    BrowsingPreferences browsing = new();
    AddressSuggestions? addressSuggestions;
    int sleepMinutes => browsing.SleepMinutes;
    string BrowsingFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VOLT","browsing.json");
    void LoadBrowsingPreferences()
    {
        try
        {
            if(File.Exists(BrowsingFile) && new FileInfo(BrowsingFile).Length<=1_500_000)
                browsing=JsonSerializer.Deserialize<BrowsingPreferences>(File.ReadAllText(BrowsingFile)) ?? new();
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException) { }
        if(!new[]{0,1,5,10,15,30}.Contains(browsing.SleepMinutes))browsing.SleepMinutes=5;
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
            if(tab.Ready)tab.View!.CoreWebView2.Resume();
            else if(tab.Unloaded && tab.Url is { } url)_=NavigateTab(tab,url);
        }
    }
    void InitializeBrowsingOptions(ContextMenuStrip menu)
    {
        address.AutoCompleteMode=AutoCompleteMode.None;
        address.AutoCompleteSource=AutoCompleteSource.None;
        addressSuggestions=new AddressSuggestions(this,address,Navigate,darkMode);
        RefreshAddressSuggestions();
        var timeMenu=new ToolStripMenuItem("Tempo para hibernar");
        foreach(int minutes in new[]{1,5,10,15,30,0})
        {
            var choice=new ToolStripMenuItem(minutes==0?"Nunca":$"{minutes} minuto(s)") {Checked=minutes==sleepMinutes};
            choice.Click+=(_,_)=>
            {
                browsing.SleepMinutes=minutes;SaveBrowsingPreferences();
                foreach(ToolStripMenuItem item in timeMenu.DropDownItems)item.Checked=item==choice;
                foreach(var tab in items.Values)
                    if(tab.Ready && tab.View!.CoreWebView2.IsSuspended){tab.View.CoreWebView2.Resume();tab.HiddenSince=DateTime.UtcNow;}
            };
            timeMenu.DropDownItems.Add(choice);
        }
        menu.Items.Add(timeMenu);
        menu.Items.Add("Primeiras abas protegidas…",null,(_,_)=>
        {
            using var dialog=new Form {Text="Prioridade das abas",ClientSize=new(430,155),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false};
            var label=new Label {Text="Manter as primeiras N abas ativas, da esquerda para a direita:",AutoSize=false,Bounds=new(16,16,400,44)};
            var count=new NumericUpDown {Minimum=0,Maximum=100,Value=browsing.PriorityTabs,Bounds=new(16,65,90,30)};
            var ok=new Button {Text="Salvar",DialogResult=DialogResult.OK,Bounds=new(310,110,100,30)};
            dialog.Controls.AddRange(new Control[]{label,count,ok});dialog.AcceptButton=ok;
            if(dialog.ShowDialog(this)!=DialogResult.OK)return;
            browsing.PriorityTabs=(int)count.Value;SaveBrowsingPreferences();ResumePriorityTabs();UpdateChrome();
        });
        menu.Items.Add("Limpar histórico de endereços",null,(_,_)=>{browsing.History.Clear();SaveBrowsingPreferences();RefreshAddressSuggestions();});
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
            using var bytes=new MemoryStream();var buffer=new byte[8192];int read;
            while((read=await stream.ReadAsync(buffer))>0)
            {if(bytes.Length+read>1_048_576)return;bytes.Write(buffer,0,read);}
            if(closing || tab.View!=view || revision!=tab.IconRevision || !items.ContainsKey(tab.Page) || bytes.Length==0)return;
            bytes.Position=0;using var image=Image.FromStream(bytes);
            var small=new Bitmap(16,16);using(var g=Graphics.FromImage(small))g.DrawImage(image,0,0,16,16);
            tab.Favicon?.Dispose();tab.Favicon=small;browserTabHeader?.Invalidate();
        }
        catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException or ArgumentException or IOException or ObjectDisposedException) { }
    }
}
