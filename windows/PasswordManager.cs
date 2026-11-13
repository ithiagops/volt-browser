using System.Text.Json;
using System.Security.Cryptography;

namespace Volt;

sealed partial class BrowserWindow
{
    void ShowPasswords()
    {
        Color bg=darkMode?Color.FromArgb(24,30,39):Color.FromArgb(243,247,251);
        Color surface=darkMode?Color.FromArgb(35,43,54):Color.White;
        Color fg=darkMode?Color.FromArgb(229,236,244):Color.FromArgb(37,51,67);
        Color muted=darkMode?Color.FromArgb(158,174,193):Color.FromArgb(92,111,132);
        Color selected=darkMode?Color.FromArgb(52,67,84):Color.FromArgb(223,235,248);
        using var dialog=new Form {Text="Senhas — VOLT",Size=new(744,504),MinimumSize=new(680,460),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.None,ShowInTaskbar=false,BackColor=bg,ForeColor=fg,Font=new("Segoe UI",10),Padding=new(1),AutoScaleMode=AutoScaleMode.Dpi,KeyPreview=true};
        void RoundDialog(){using var shape=ChromeShape.Rounded(dialog.ClientRectangle,16*dialog.DeviceDpi/96f);var old=dialog.Region;dialog.Region=new Region(shape);old?.Dispose();}
        dialog.SizeChanged+=(_,_)=>RoundDialog();RoundDialog();
        dialog.Paint+=(_,e)=>{e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using var pen=new Pen(darkMode?Color.FromArgb(48,61,78):Color.FromArgb(213,224,236));using var outline=ChromeShape.Rounded(new RectangleF(0,0,dialog.ClientSize.Width-1,dialog.ClientSize.Height-1),16);e.Graphics.DrawPath(pen,outline);};
        var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new(28,20,28,18),ColumnCount=1,RowCount=5,BackColor=bg};
        layout.ColumnStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,72));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,40));layout.RowStyles.Add(new(SizeType.Absolute,44));layout.RowStyles.Add(new(SizeType.Absolute,0));
        VaultActionButton ActionButton(string title,int width,bool primary=false)=>new() {Text=title,AccessibleName=title,Primary=primary,Dark=darkMode,Width=width,Height=38,Margin=new(0,0,10,0),BackColor=bg,ForeColor=fg,Font=new("Segoe UI",10)};
        var header=new Panel {Dock=DockStyle.Fill,BackColor=bg,Margin=Padding.Empty};
        var heading=new Label {Text="Senhas",Font=new("Segoe UI",20,FontStyle.Regular),ForeColor=fg,AutoSize=true,Location=new(0,0)};
        var subtitle=new Label {Text="Seu cofre pessoal no VOLT",ForeColor=muted,AutoSize=true,Location=new(2,38)};
        var topClose=new ChromeButton {Kind="close",AccessibleName="Fechar gerenciador de senhas",Size=new(32,32),Anchor=AnchorStyles.Top|AnchorStyles.Right,DarkTheme=darkMode,BackColor=bg,ForeColor=muted};
        header.SizeChanged+=(_,_)=>topClose.Location=new(Math.Max(0,header.Width-topClose.Width),0);
        header.Controls.Add(heading);header.Controls.Add(subtitle);header.Controls.Add(topClose);
        void DragDialog(object? sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)BeginWindowDrag(dialog);}
        header.MouseDown+=DragDialog;heading.MouseDown+=DragDialog;subtitle.MouseDown+=DragDialog;
        var list=new DataGridView {Dock=DockStyle.Fill,BackgroundColor=surface,BorderStyle=BorderStyle.None,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AllowUserToResizeRows=false,RowHeadersVisible=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,CellBorderStyle=DataGridViewCellBorderStyle.None,ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None,EnableHeadersVisualStyles=false,ColumnHeadersHeight=40,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing,AccessibleName="Logins no cofre",GridColor=surface};
        list.DefaultCellStyle=new DataGridViewCellStyle {BackColor=surface,ForeColor=fg,SelectionBackColor=selected,SelectionForeColor=fg,Padding=new(12,0,8,0),Font=new("Segoe UI",10)};
        list.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle {BackColor=surface,ForeColor=muted,SelectionBackColor=surface,SelectionForeColor=muted,Padding=new(12,0,8,0),Font=new("Segoe UI",9,FontStyle.Bold)};
        list.RowTemplate.Height=44;list.Columns.Add("site","SITE");list.Columns.Add("username","USUÁRIO");list.Columns[0].FillWeight=58;list.Columns[1].FillWeight=42;
        foreach(DataGridViewColumn column in list.Columns)column.SortMode=DataGridViewColumnSortMode.NotSortable;
        var content=new Panel {Dock=DockStyle.Fill,Margin=Padding.Empty,BackColor=bg};
        var card=new Panel {Dock=DockStyle.Fill,BackColor=surface,Padding=new(6),Visible=false};card.Controls.Add(list);content.Controls.Add(card);
        card.SizeChanged+=(_,_)=>{if(card.Width<1||card.Height<1)return;using var shape=ChromeShape.Rounded(card.ClientRectangle,12);var old=card.Region;card.Region=new Region(shape);old?.Dispose();};
        var empty=new VaultEmptyState {Dock=DockStyle.Fill,Dark=darkMode,BackColor=bg,Visible=false};
        var emptyImport=ActionButton("Importar senhas",174,true);empty.Controls.Add(emptyImport);content.Controls.Add(empty);
        void PlaceEmptyButton()=>emptyImport.Location=new(Math.Max(0,(empty.Width-emptyImport.Width)/2),empty.ButtonTop);
        empty.SizeChanged+=(_,_)=>PlaceEmptyButton();
        var statusLabel=new Label {Dock=DockStyle.Fill,Text="Abrindo cofre…",ForeColor=muted,TextAlign=ContentAlignment.MiddleLeft,Font=new("Segoe UI",9),Margin=Padding.Empty};
        var privacy=new Label {Dock=DockStyle.Fill,Text="Protegido pelo seu usuário do Windows.\nO CSV exportado contém senhas em texto claro. Exclua-o após importar.",ForeColor=muted,Font=new("Segoe UI",9),Margin=Padding.Empty};
        var buttons=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false,BackColor=bg,Margin=Padding.Empty,Visible=false};
        var import=ActionButton("Importar CSV",126);var fill=ActionButton("Preencher nesta página",196,true);var remove=ActionButton("Remover",96);var close=ActionButton("Fechar",82);
        buttons.Controls.AddRange([fill,import,remove,close]);layout.Controls.Add(header,0,0);layout.Controls.Add(content,0,1);layout.Controls.Add(statusLabel,0,2);layout.Controls.Add(privacy,0,3);layout.Controls.Add(buttons,0,4);dialog.Controls.Add(layout);
        bool busy=false;
        void UpdateActions(){import.Enabled=emptyImport.Enabled=!busy;fill.Enabled=remove.Enabled=!busy&&list.SelectedRows.Count==1;}
        list.SelectionChanged+=(_,_)=>UpdateActions();UpdateActions();
        void Render(List<PasswordEntry> entries)
        {
            list.SuspendLayout();list.Rows.Clear();
            foreach(var entry in entries){int row=list.Rows.Add(entry.Origin,entry.Username.Length==0?"(sem usuário)":entry.Username);list.Rows[row].Tag=entry;}
            list.ClearSelection();list.ResumeLayout();bool hasEntries=entries.Count>0;
            card.Visible=buttons.Visible=hasEntries;empty.Visible=!hasEntries;
            layout.RowStyles[4].Height=hasEntries?48*dialog.DeviceDpi/96f:0;
            if(hasEntries)card.BringToFront();else{empty.BringToFront();PlaceEmptyButton();}
            UpdateActions();statusLabel.Text=hasEntries?$"{entries.Count} acesso(s) · Selecione um login para preencher sem enviar o formulário.":"";
        }
        async Task Run<T>(Func<T> action,Action<T> done)
        {
            if(busy)return;busy=true;UpdateActions();
            try{done(await Task.Run(action));}
            catch(InvalidDataException ex){statusLabel.Text=ex.Message;}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or CryptographicException or JsonException or System.Text.DecoderFallbackException)
            {statusLabel.Text="Não foi possível acessar ou gravar o cofre. Os dados existentes não serão substituídos por um cofre vazio.";}
            finally{busy=false;UpdateActions();}
        }
        async Task ImportCsv()
        {
            using var file=new OpenFileDialog {Title="Importar senhas",Filter="CSV de senhas (*.csv)|*.csv",CheckFileExists=true};
            if(file.ShowDialog(dialog)!=DialogResult.OK)return;
            statusLabel.Text="Importando para o cofre criptografado…";
            await Run(()=>PasswordVault.Import(file.FileName),result=>{Render(result.Entries);statusLabel.Text=$"Importados: {result.Added}. Atualizados: {result.Updated}. Linhas ignoradas: {result.Skipped}.";});
        }
        import.Click+=async(_,_)=>await ImportCsv();emptyImport.Click+=async(_,_)=>await ImportCsv();
        fill.Click+=async(_,_)=>
        {
            if(list.SelectedRows.Count==0 || list.SelectedRows[0].Tag is not PasswordEntry entry)return;
            if(Current is not {Ready:true,View:{} view} tab || PasswordRules.Origin(tab.Url??"")!=entry.Origin)
            {statusLabel.Text="Abra a página de login do mesmo endereço (protocolo, domínio e porta) antes de preencher.";return;}
            busy=true;UpdateActions();
            try
            {
                string result=await view.CoreWebView2.ExecuteScriptAsync(PasswordFillScript(entry));
                statusLabel.Text=JsonSerializer.Deserialize<string>(result)=="ok"?"Login preenchido. Confira os campos e entre quando desejar.":"Não foi possível identificar um formulário de login seguro. Nenhum formulário foi enviado.";
            }
            catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException or JsonException){statusLabel.Text="A página mudou. Abra novamente o formulário e tente preencher.";}
            finally{busy=false;UpdateActions();}
        };
        remove.Click+=async(_,_)=>
        {
            if(list.SelectedRows.Count==0 || list.SelectedRows[0].Tag is not PasswordEntry entry)return;
            if(MessageBox.Show(dialog,"Remover este login do cofre do VOLT?","Remover login",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            await Run(()=>PasswordVault.Remove(entry),Render);
        };
        close.Click+=(_,_)=>dialog.Close();topClose.Click+=(_,_)=>dialog.Close();dialog.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Escape){e.Handled=true;dialog.Close();}};dialog.FormClosing+=(_,e)=>{if(busy)e.Cancel=true;};
        dialog.Shown+=async(_,_)=>await Run(PasswordVault.Load,Render);dialog.ShowDialog(this);
    }
    static string PasswordFillScript(PasswordEntry entry)
    {
        string args=JsonSerializer.Serialize(new[]{entry.Origin,entry.Username,entry.Password});
        return """
        ((data)=>{
          const [site,user,secret]=data;
          if(location.origin!==site)return 'origin';
          const visible=e=>!e.disabled&&!e.readOnly&&e.getClientRects().length>0;
          const passwords=[...document.querySelectorAll('input[type="password"]')].filter(e=>visible(e)&&e.autocomplete!=='new-password');
          if(passwords.length!==1)return 'fields';
          const password=passwords[0],scope=password.form||document;
          const candidates=[...scope.querySelectorAll('input')].filter(e=>visible(e)&&['text','email','tel'].includes(e.type)&&e.autocomplete!=='one-time-code');
          const username=candidates.find(e=>e.autocomplete==='username')||candidates.find(e=>e.type==='email')||candidates.find(e=>/user|login|email/i.test(e.name+' '+e.id))||(candidates.length===1?candidates[0]:null);
          const set=(e,v)=>{Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(e,v);e.dispatchEvent(new Event('input',{bubbles:true}));e.dispatchEvent(new Event('change',{bubbles:true}));};
          if(username&&user)set(username,user);set(password,secret);return 'ok';
        })
        """+"("+args+")";
    }
}
