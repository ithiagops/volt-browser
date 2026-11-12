using System.Text.Json;
using System.Security.Cryptography;

namespace Volt;

sealed partial class BrowserWindow
{
    void ShowPasswords()
    {
        using var dialog=new Form {Text="Senhas — VOLT",Size=new(740,460),MinimumSize=new(620,360),StartPosition=FormStartPosition.CenterParent};
        var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new(12),ColumnCount=1,RowCount=4};
        layout.ColumnStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,68));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,48));layout.RowStyles.Add(new(SizeType.Absolute,42));
        var help=new Label {Dock=DockStyle.Fill,Text="Importe o CSV exportado do Chrome, Opera, Edge, Firefox ou Bitwarden.\nO CSV contém senhas em texto claro: exclua a cópia exportada após importar.\nO cofre do VOLT é criptografado e vinculado ao seu usuário do Windows."};
        var list=new ListView {Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false};
        list.Columns.Add("Site",360);list.Columns.Add("Usuário",260);
        var statusLabel=new Label {Dock=DockStyle.Fill,Text="Abrindo cofre…",TextAlign=ContentAlignment.MiddleLeft};
        var buttons=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};
        var import=new Button {Text="Importar CSV…",AutoSize=true};var fill=new Button {Text="Preencher nesta página",AutoSize=true};var remove=new Button {Text="Remover",AutoSize=true};var close=new Button {Text="Fechar",AutoSize=true};
        buttons.Controls.AddRange([import,fill,remove,close]);layout.Controls.Add(help,0,0);layout.Controls.Add(list,0,1);layout.Controls.Add(statusLabel,0,2);layout.Controls.Add(buttons,0,3);dialog.Controls.Add(layout);
        BrowserTheme.Apply(dialog,darkMode);bool busy=false;
        void Render(List<PasswordEntry> entries)
        {
            list.BeginUpdate();list.Items.Clear();
            foreach(var entry in entries)list.Items.Add(new ListViewItem([entry.Origin,entry.Username.Length==0?"(sem usuário)":entry.Username]){Tag=entry});
            list.EndUpdate();statusLabel.Text=$"{entries.Count} login(s). Preencher não envia o formulário automaticamente.";
        }
        async Task Run<T>(Func<T> action,Action<T> done)
        {
            if(busy)return;busy=true;import.Enabled=fill.Enabled=remove.Enabled=false;
            try{done(await Task.Run(action));}
            catch(InvalidDataException ex){statusLabel.Text=ex.Message;}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or CryptographicException or JsonException or System.Text.DecoderFallbackException)
            {statusLabel.Text="Não foi possível acessar ou gravar o cofre. Os dados existentes não serão substituídos por um cofre vazio.";}
            finally{busy=false;import.Enabled=fill.Enabled=remove.Enabled=true;}
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
            if(list.SelectedItems.Count==0 || list.SelectedItems[0].Tag is not PasswordEntry entry)return;
            if(Current is not {Ready:true,View:{} view} tab || PasswordRules.Origin(tab.Url??"")!=entry.Origin)
            {statusLabel.Text="Abra a página de login do mesmo endereço (protocolo, domínio e porta) antes de preencher.";return;}
            busy=true;import.Enabled=fill.Enabled=remove.Enabled=false;
            try
            {
                string result=await view.CoreWebView2.ExecuteScriptAsync(PasswordFillScript(entry));
                statusLabel.Text=JsonSerializer.Deserialize<string>(result)=="ok"?"Login preenchido. Confira os campos e entre quando desejar.":"Não foi possível identificar um formulário de login seguro. Nenhum formulário foi enviado.";
            }
            catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException or JsonException){statusLabel.Text="A página mudou. Abra novamente o formulário e tente preencher.";}
            finally{busy=false;import.Enabled=fill.Enabled=remove.Enabled=true;}
        };
        remove.Click+=async(_,_)=>
        {
            if(list.SelectedItems.Count==0 || list.SelectedItems[0].Tag is not PasswordEntry entry)return;
            if(MessageBox.Show(dialog,"Remover este login do cofre do VOLT?","Remover login",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            await Run(()=>PasswordVault.Remove(entry),Render);
        };
        close.Click+=(_,_)=>dialog.Close();dialog.FormClosing+=(_,e)=>{if(busy)e.Cancel=true;};
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
