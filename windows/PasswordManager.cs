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
        using var dialog=new Form {Text="Senhas — VOLT",Size=new(820,560),MinimumSize=new(740,500),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.None,ShowInTaskbar=false,BackColor=bg,ForeColor=fg,Font=new("Segoe UI",10),Padding=new(1),AutoScaleMode=AutoScaleMode.Dpi,KeyPreview=true};
        void RoundDialog()
        {
            using var shape=ChromeShape.Rounded(dialog.ClientRectangle,12*dialog.DeviceDpi/96f);
            var old=dialog.Region;dialog.Region=new Region(shape);old?.Dispose();
        }
        dialog.SizeChanged+=(_,_)=>RoundDialog();RoundDialog();
        dialog.Paint+=(_,e)=>{using var pen=new Pen(darkMode?Color.FromArgb(65,79,98):Color.FromArgb(205,218,230));using var outline=ChromeShape.Rounded(new RectangleF(0,0,dialog.ClientSize.Width-1,dialog.ClientSize.Height-1),12);e.Graphics.DrawPath(pen,outline);};
        var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new(24,18,24,20),ColumnCount=1,RowCount=5,BackColor=bg};
        layout.ColumnStyles.Add(new(SizeType.Percent,100));
        foreach(var height in new[]{76,68})layout.RowStyles.Add(new(SizeType.Absolute,height));
        layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,54));layout.RowStyles.Add(new(SizeType.Absolute,48));
        ChromeButton ActionButton(string title,int width)=>new() {Text=title,AccessibleName=title,Pill=true,DarkTheme=darkMode,Width=width,Height=38,Margin=new(0,4,10,4),BackColor=bg,ForeColor=fg,Font=new("Segoe UI",10)};
        var header=new Panel {Dock=DockStyle.Fill,BackColor=bg};
        var heading=new Label {Text="Senhas",Font=new("Segoe UI",22,FontStyle.Regular),ForeColor=fg,AutoSize=true,Location=new(0,0)};
        var subtitle=new Label {Text="Seus acessos, guardados no cofre do Windows.",ForeColor=muted,AutoSize=true,Location=new(2,43)};
        var topClose=new ChromeButton {Kind="close",AccessibleName="Fechar gerenciador de senhas",Dock=DockStyle.Right,Width=36,DarkTheme=darkMode,BackColor=bg,ForeColor=fg};
        header.Controls.Add(heading);header.Controls.Add(subtitle);header.Controls.Add(topClose);
        void DragDialog(object? sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){BeginWindowDrag(dialog);}}
        header.MouseDown+=DragDialog;heading.MouseDown+=DragDialog;subtitle.MouseDown+=DragDialog;
        var help=new Label {Dock=DockStyle.Fill,Padding=new(14,10,14,8),BackColor=surface,ForeColor=muted,Text="Importe um CSV do Chrome, Opera, Edge, Firefox ou Bitwarden.\nO arquivo exportado contém senhas em texto claro. Exclua sua cópia após importar."};
        var list=new DataGridView {Dock=DockStyle.Fill,BackgroundColor=surface,BorderStyle=BorderStyle.None,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AllowUserToResizeRows=false,RowHeadersVisible=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,CellBorderStyle=DataGridViewCellBorderStyle.None,ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None,EnableHeadersVisualStyles=false,ColumnHeadersHeight=40,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing,AccessibleName="Logins no cofre",GridColor=surface};
        list.DefaultCellStyle=new DataGridViewCellStyle {BackColor=surface,ForeColor=fg,SelectionBackColor=selected,SelectionForeColor=fg,Padding=new(12,0,8,0),Font=new("Segoe UI",10)};
        list.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle {BackColor=surface,ForeColor=muted,SelectionBackColor=surface,SelectionForeColor=muted,Padding=new(12,0,8,0),Font=new("Segoe UI",9,FontStyle.Bold)};
        list.RowTemplate.Height=44;list.Columns.Add("site","SITE");list.Columns.Add("username","USUÁRIO");list.Columns[0].FillWeight=58;list.Columns[1].FillWeight=42;
        foreach(DataGridViewColumn column in list.Columns)column.SortMode=DataGridViewColumnSortMode.NotSortable;
        var card=new Panel {Dock=DockStyle.Fill,BackColor=surface,Padding=new(4),Margin=new(0,14,0,0)};card.Controls.Add(list);
        var empty=new Label {Text="Seu cofre está vazio\nImporte seus acessos para começar.",TextAlign=ContentAlignment.MiddleCenter,ForeColor=muted,BackColor=surface,Visible=false};card.Controls.Add(empty);empty.BringToFront();
        void EmptyBounds(){empty.Bounds=new Rectangle(12,55,Math.Max(0,card.Width-24),Math.Max(0,card.Height-66));}
        card.SizeChanged+=(_,_)=>{EmptyBounds();using var shape=ChromeShape.Rounded(card.ClientRectangle,12);var old=card.Region;card.Region=new Region(shape);old?.Dispose();};
        help.SizeChanged+=(_,_)=>{using var shape=ChromeShape.Rounded(help.ClientRectangle,10);var old=help.Region;help.Region=new Region(shape);old?.Dispose();};
        var statusLabel=new Label {Dock=DockStyle.Fill,Text="Abrindo cofre…",ForeColor=muted,TextAlign=ContentAlignment.MiddleLeft};
        var buttons=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false,BackColor=bg,Margin=Padding.Empty};
        var import=ActionButton("Importar CSV…",132);var fill=ActionButton("Preencher nesta página",198);var remove=ActionButton("Remover",100);var close=ActionButton("Fechar",90);
        buttons.Controls.AddRange([import,fill,remove,close]);layout.Controls.Add(header,0,0);layout.Controls.Add(help,0,1);layout.Controls.Add(card,0,2);layout.Controls.Add(statusLabel,0,3);layout.Controls.Add(buttons,0,4);dialog.Controls.Add(layout);
        bool busy=false;
        void UpdateActions(){import.Enabled=!busy;fill.Enabled=remove.Enabled=!busy&&list.SelectedRows.Count==1;}
        list.SelectionChanged+=(_,_)=>UpdateActions();UpdateActions();
        void Render(List<PasswordEntry> entries)
        {
            list.SuspendLayout();list.Rows.Clear();
            foreach(var entry in entries){int row=list.Rows.Add(entry.Origin,entry.Username.Length==0?"(sem usuário)":entry.Username);list.Rows[row].Tag=entry;}
            list.ClearSelection();list.ResumeLayout();empty.Visible=entries.Count==0;EmptyBounds();UpdateActions();statusLabel.Text=$"{entries.Count} login(s). Preencher não envia o formulário automaticamente.";
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
        import.Click+=async(_,_)=>
        {
            using var file=new OpenFileDialog {Title="Importar senhas",Filter="CSV de senhas (*.csv)|*.csv",CheckFileExists=true};
            if(file.ShowDialog(dialog)!=DialogResult.OK)return;
            statusLabel.Text="Importando para o cofre criptografado…";
            await Run(()=>PasswordVault.Import(file.FileName),result=>{Render(result.Entries);statusLabel.Text=$"Importados: {result.Added}. Atualizados: {result.Updated}. Linhas ignoradas: {result.Skipped}.";});
        };
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
