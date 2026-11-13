using Volt;

int checks = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
Check(!ResourcePolicy.MemoryPressure(false, 1_200_000_000L), "Threshold boundary");
Check(ResourcePolicy.MemoryPressure(false, 1_200_000_001L), "Enter pressure");
Check(ResourcePolicy.MemoryPressure(true, 1_100_000_000L), "Hysteresis");
Check(!ResourcePolicy.MemoryPressure(true, 1_000_000_000L), "Exit pressure");
var range=new TabRangeSelection<string>();
var order=new List<string>{"A","B","C","D","E","F"};
range.Click(order,"B",false);range.Click(order,"E",true);
Check(range.Items.SetEquals(new[]{"B","C","D","E"}),"Forward inclusive range");
range.Click(order,"A",true);Check(range.Items.SetEquals(new[]{"A","B"}),"Reverse keeps original anchor");
range.Click(order,"D",false);range.Click(order,"B",true);
Check(range.Items.SetEquals(new[]{"B","C","D"}),"Backward range");
range.Click(order,"D",true);Check(range.Items.SetEquals(new[]{"D"}),"Same endpoint");
order.Remove("D");range.Prune(order);Check(range.Items.Count==0,"Removed anchor pruned");
range.Click(order,"F",true);Check(range.Items.SetEquals(new[]{"F"}),"Missing anchor fallback");
var sequence=new[]{"A","B","C","D","E","F"};
Check(TabBlockMove.Move(sequence,new[]{"D","E","F"},0).SequenceEqual(new[]{"D","E","F","A","B","C"}),"Move last three to front");
Check(TabBlockMove.Move(sequence,new[]{"A","B","C"},6).SequenceEqual(new[]{"D","E","F","A","B","C"}),"Move first three to end");
Check(TabBlockMove.Move(sequence,new[]{"C","B"},3).SequenceEqual(sequence),"Drop inside block, preserve original order");
Check(TabBlockMove.Move(sequence,sequence,4).SequenceEqual(sequence),"All tabs block");
Check(TabBlockMove.Move(sequence,Array.Empty<string>(),0).SequenceEqual(sequence),"Empty block");
for(int slot=0;slot<=sequence.Length;slot++)
{
 var moved=TabBlockMove.Move(sequence,new[]{"D","E","F"},slot);
 Check(moved.Count==sequence.Length && moved.Distinct().Count()==sequence.Length,"Preserve identities "+slot);
 Check(moved.Where(x=>x is "D" or "E" or "F").SequenceEqual(new[]{"D","E","F"}),"Block order "+slot);
}
for(int source=0;source<sequence.Length;source++)
for(int slot=0;slot<=sequence.Length;slot++)
{
 var item=sequence[source];var reordered=TabBlockMove.Move(sequence,new[]{item},slot);
 Check(reordered.IndexOf(item)==ResourcePolicy.MoveTabIndex(source,slot,sequence.Length)
     && reordered.Where(x=>x!=item).SequenceEqual(sequence.Where(x=>x!=item)),"Single and group move parity "+source+"/"+slot);
}
Check(TabCloseSelection.Next(sequence,new[]{"B","C"},"E")=="E","Closing background keeps active");
Check(TabCloseSelection.Next(sequence,new[]{"C"},"C")=="D","Close active selects right neighbor");
Check(TabCloseSelection.Next(sequence,new[]{"F"},"F")=="E","Close last selects left neighbor");
Check(TabCloseSelection.Next(sequence,new[]{"B","C","D"},"C")=="E","Close range selects final survivor");
Check(TabCloseSelection.Next(sequence,sequence,"C") is null,"Close all has no survivor");
Check(TabCloseSelection.Next(sequence,new[]{"B"},null)=="A","Missing active selects first survivor");
// Navegações reais e esquemas que nunca devem ser executados pela barra.
foreach (var (input, expected) in new[]
{
    ("youtube.com", "https://youtube.com"),
    ("  https://example.com/a?q=1  ", "https://example.com/a?q=1"),
    ("http://localhost:8000", "http://localhost:8000"),
    ("memória ram", "https://www.google.com/search?q=mem%C3%B3ria%20ram"),
    ("javascript:alert(1)", "https://www.google.com/search?q=javascript%3Aalert%281%29"),
    ("file:///C:/Windows", "https://www.google.com/search?q=file%3A%2F%2F%2FC%3A%2FWindows")
}) Check(ResourcePolicy.Address(input) == expected, "Endereço: " + input);
// Correspondência de domínio: não bloquear páginas que só citam o rastreador.
foreach (var (url, blocked) in new[]
{
    ("https://www.google-analytics.com/g/collect", true),
    ("https://google-analytics.com/g/collect", true),
    ("https://A.Clarity.MS/x", true),
    ("https://notgoogle-analytics.com/x", false),
    ("https://google-analytics.com.example.org/x", false),
    ("https://example.org/?next=google-analytics.com", false),
    ("https://www.youtube.com/watch?v=123", false),
    ("https://i.ytimg.com/image.jpg", false),
    ("not a URL", false)
}) Check(ResourcePolicy.IsTracker(url) == blocked, "Rastreador: " + url);
// Atividades que não podem ser interrompidas pelo gerenciamento automático.
Check(ResourcePolicy.CanSleep(false, true, false, false, 0, false), "Aba ociosa deve ser elegível");
Check(!ResourcePolicy.CanSleep(true, true, false, false, 0, false), "Aba selecionada protegida");
Check(!ResourcePolicy.CanSleep(false, false, false, false, 0, false), "Aba nativa sem motor protegida");
Check(!ResourcePolicy.CanSleep(false, true, true, false, 0, false), "Carregamento protegido");
Check(!ResourcePolicy.CanSleep(false, true, false, true, 0, false), "Aba fixada protegida");
Check(!ResourcePolicy.CanSleep(false, true, false, false, 1, false), "Download protegido");
Check(!ResourcePolicy.CanSleep(false, true, false, false, 0, true), "Áudio protegido");
// Inserção deve preservar o conjunto existente e posicionar a nova aba depois da atual.
Check(ResourcePolicy.NewTabIndex(0, 1) == 1, "Nova aba após a primeira");
Check(ResourcePolicy.NewTabIndex(2, 5) == 3, "Nova aba no meio da faixa");
Check(ResourcePolicy.NewTabIndex(4, 5) == 5, "Nova aba depois da última");
Check(ResourcePolicy.NewTabIndex(-1, 0) == 0, "Primeira aba antes do botão +");
Check(!ResourcePolicy.ShouldUnload(false, TimeSpan.FromHours(1), true), "Modo forte desligado nunca descarta");
Check(!ResourcePolicy.ShouldUnload(true, TimeSpan.FromSeconds(179), true), "Modo forte respeita prazo");
Check(ResourcePolicy.ShouldUnload(true, TimeSpan.FromSeconds(180), true), "Modo forte libera ao atingir prazo");
Check(!ResourcePolicy.ShouldUnload(true, TimeSpan.FromHours(1), false), "Modo forte preserva aba protegida");
Check(ResourcePolicy.SleepProtection(false, true, false, false, 1, false) == "Download em andamento", "Diagnóstico de download");
Check(ResourcePolicy.SleepProtection(false, true, false, false, 0, true) == "Áudio em reprodução", "Diagnóstico de áudio");
Check(ResourcePolicy.SleepProtection(true, true, false, false, 0, false) == "Em uso", "Diagnóstico de seleção");
Check(ResourcePolicy.MoveTabIndex(0, 4, 4) == 3, "Arrastar primeira para o fim");
Check(ResourcePolicy.MoveTabIndex(3, 0, 4) == 0, "Arrastar última para início");
Check(ResourcePolicy.MoveTabIndex(1, 2, 4) == 1, "Soltar ao lado de si mantém posição");
Check(ResourcePolicy.MoveTabIndex(2, 1, 4) == 1, "Mover aba para esquerda");
Check(ResourcePolicy.MoveTabIndex(0, 1, 1) == 0, "Única aba não sai da faixa");
Check(ResourcePolicy.ShouldUnload(true, TimeSpan.FromSeconds(45), true, 3), "Meta de duas páginas libera excedente parado");
Check(!ResourcePolicy.ShouldUnload(true, TimeSpan.FromSeconds(44), true, 8), "Meta respeita tempo mínimo");
Check(!ResourcePolicy.ShouldUnload(true, TimeSpan.FromSeconds(100), true, 2), "Meta mantém duas páginas quentes");
Check(!ResourcePolicy.ShouldUnload(true, TimeSpan.FromSeconds(100), false, 8), "Meta não descarta aba protegida");
Check(!ResourcePolicy.ShouldUnload(false, TimeSpan.FromSeconds(100), true, 8), "Meta desligada não descarta excedentes");
// Tamanhos reais de faixa: abas completas e botões dentro da largura disponível.
foreach (int width in new[] { 360, 760, 1280, 1920, 3840 })
foreach (int count in new[] { 1, 2, 5, 12, 100 })
{
    var layout = TabLayout.Calculate(count, width, 0, count - 1, true);
    Check(layout.Visible > 0 && layout.Start >= 0 && layout.Start + layout.Visible <= count, "Intervalo visível válido");
    Check(count - 1 >= layout.Start && count - 1 < layout.Start + layout.Visible, "Selecionada aparece por inteiro");
    float lastButton = 8 + layout.Visible * (layout.Width + 4) + 4 + 22 + (layout.Overflow ? 56 : 0);
    Check(lastButton <= width, "Abas e + sem corte nem sobreposição");
}
Check(TabLayout.Calculate(0, 760, 0, -1, false).Visible == 0, "Faixa vazia preserva botão +");
Check(SidebarLayout.Width(1600,760)==760,"Wide sidebar keeps preferred width");
Check(SidebarLayout.Width(708,760)==443,"Sidebar leaves the page clickable on a small viewport");
Check(SidebarLayout.Width(1600,100)==340,"Sidebar minimum width");
foreach(int viewport in new[]{0,200,500,708,1240,1920,3840})
foreach(int preferred in new[]{-100,0,340,760,2000,5000})
{
    int width=SidebarLayout.Width(viewport,preferred);
    Check(width>=0 && width+Math.Min(265,viewport)<=viewport,"Overlay never exceeds the viewport");
}
foreach(float dpi in new[]{1f,1.25f,1.5f,2f})
{
    var visual=new System.Drawing.RectangleF(8*dpi,6*dpi,198*dpi,24*dpi);
    var hit=TabLayout.HitBounds(visual,36*dpi,2*dpi);
    Check(hit.Contains(20*dpi,0),"Top edge selects tab");
    Check(hit.Contains(20*dpi,3*dpi),"Above rounded shape selects tab");
    Check(hit.Contains(20*dpi,35*dpi),"Bottom of strip selects tab");
    Check(hit.Contains(207*dpi,0),"Small divider gap remains clickable");
    Check(!hit.Contains(20*dpi,-dpi),"Outside strip above is excluded");
    Check(!hit.Contains(20*dpi,36*dpi),"Toolbar below is excluded");
    Check(!hit.Contains(5*dpi,0),"Empty title space remains draggable");
    Check(!hit.Contains(210*dpi,0),"Next tab target is separate");
}

Check(PasswordRules.Origin("https://EXAMPLE.test:443/login")=="https://example.test","Password origin canonicalization");
Check(PasswordRules.Origin("http://example.test")!=PasswordRules.Origin("https://example.test"),"Password scheme isolation");
Check(PasswordRules.Origin("https://example.test:8443")!=PasswordRules.Origin("https://example.test"),"Password port isolation");
Check(PasswordRules.Origin("https://example.test.evil.test")!=PasswordRules.Origin("https://example.test"),"Password hostname isolation");
Check(PasswordRules.Origin("https://u:p@example.test") is null,"Reject credential URLs");
Check(PasswordRules.Origin("javascript:alert(1)") is null,"Reject non-web origins");
var passwordCsv=Path.GetTempFileName();
try
{
    File.WriteAllText(passwordCsv,"\uFEFFname,url,username,password\nExample,https://example.test/login,\"user,one\",\"FAKE quote \"\" and\nnewline\"\n");
    var imported=PasswordRules.ReadCsv(passwordCsv);
    Check(imported.Entries.Count==1 && imported.Skipped==0,"Import quoted CSV with multiline password");
    Check(imported.Entries[0].Password=="FAKE quote \" and\nnewline","Preserve password bytes after CSV unquoting");
    Check(imported.Entries[0].Username=="user,one","Preserve quoted username");
    File.WriteAllText(passwordCsv,"url;username;password\nhttps://example.test;u;FAKE\n");
    Check(PasswordRules.ReadCsv(passwordCsv).Entries[0].Password=="FAKE","Import semicolon CSV");
    File.WriteAllText(passwordCsv,"login_uri,login_username,login_password\nhttps://example.test,u,FAKE\n");
    Check(PasswordRules.ReadCsv(passwordCsv).Entries.Count==1,"Import alternate export columns");
    File.WriteAllText(passwordCsv,"url,username,password\njavascript:bad,u,FAKE\nhttps://example.test,u,\nhttps://valid.test,u,FAKE\n");
    var filtered=PasswordRules.ReadCsv(passwordCsv);
    Check(filtered.Skipped==2 && filtered.Entries.Count==1,"Skip unsafe origins and empty passwords");
    var merged=PasswordRules.Merge(new[]{new PasswordEntry("https://example.test","u","FAKE old")},
        new[]{new PasswordEntry("https://example.test","u","FAKE new"),new PasswordEntry("https://example.test","v","FAKE second")},out int added,out int updated);
    Check(merged.Count==2 && added==1 && updated==1,"Merge passwords by exact origin and username");
    File.WriteAllText(passwordCsv,"url,password,password\nhttps://example.test,FAKE,FAKE\n");
    bool duplicateRejected=false;try{PasswordRules.ReadCsv(passwordCsv);}catch(InvalidDataException){duplicateRejected=true;}
    Check(duplicateRejected,"Reject duplicate export headers");
}
finally{File.Delete(passwordCsv);}
foreach(int minutes in new[]{1,5,10,15,30})
{
    Check(!ResourcePolicy.SleepDue(minutes,TimeSpan.FromSeconds(minutes*60-1)),"Prazo configurável preserva até o limite");
    Check(ResourcePolicy.SleepDue(minutes,TimeSpan.FromMinutes(minutes)),"Prazo configurável permite hibernação no limite");
}
Check(!ResourcePolicy.SleepDue(0,TimeSpan.FromDays(10)),"Nunca desativa hibernação por tempo");
Check(!ResourcePolicy.PositionPriority(-1,3),"Índice ausente não recebe prioridade");
Check(ResourcePolicy.PositionPriority(2,3) && !ResourcePolicy.PositionPriority(3,3),"Primeiras N abas têm prioridade");
Check(!ResourcePolicy.PositionPriority(0,0),"Prioridade desativada por padrão");
Console.WriteLine($"{checks} verificações passaram.");
