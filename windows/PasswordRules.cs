using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace Volt;

sealed record PasswordEntry(string Origin,string Username,string Password);
sealed record PasswordCsv(List<PasswordEntry> Entries,int Skipped);

static class PasswordRules
{
    public static string? Origin(string value)
    {
        if(!Uri.TryCreate(value.Trim(),UriKind.Absolute,out var uri)
            || uri.Scheme is not ("http" or "https") || string.IsNullOrEmpty(uri.Host) || uri.UserInfo.Length>0)return null;
        var builder=new UriBuilder(uri.Scheme,uri.IdnHost,uri.IsDefaultPort?-1:uri.Port);
        return builder.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }
    public static PasswordCsv ReadCsv(string path)
    {
        if(new FileInfo(path).Length>10*1024*1024)throw new InvalidDataException("O CSV deve ter no máximo 10 MB.");
        using var parser=new TextFieldParser(path,new UTF8Encoding(false,true),true){HasFieldsEnclosedInQuotes=true,TrimWhiteSpace=false};
        string first;
        using(var reader=new StreamReader(path,new UTF8Encoding(false,true),true))first=reader.ReadLine()??"";
        parser.SetDelimiters(first.Contains(';')&&!first.Contains(',')?";":",");
        try
        {
            var headers=parser.ReadFields()?.Select(x=>x.Trim().TrimStart('\uFEFF').ToLowerInvariant()).ToArray()??[];
            if(headers.Distinct().Count()!=headers.Length)throw new InvalidDataException("O CSV contém cabeçalhos duplicados.");
            int Column(params string[] names)=>Array.FindIndex(headers,h=>names.Contains(h));
            int url=Column("url","website","origin","login_uri","hostname","site","endereço","endereco");
            int user=Column("username","user_name","login_username","usuario","usuário","nome de usuário","nome de usuario","utilizador","login");
            int pass=Column("password","senha","login_password");
            if(url<0||pass<0)throw new InvalidDataException("Use um CSV com as colunas url e password (ou equivalentes).");
            List<PasswordEntry> entries=[];int skipped=0,count=0;
            while(!parser.EndOfData)
            {
                if(++count>5000)throw new InvalidDataException("Importe no máximo 5.000 linhas por arquivo.");
                var fields=parser.ReadFields()??[];
                if(fields.Length>headers.Length)throw new InvalidDataException("O CSV está malformado. Exporte novamente do navegador.");
                string Field(int at)=>at>=0&&at<fields.Length?fields[at]:"";
                var site=Origin(Field(url));var password=Field(pass);var username=Field(user);
                if(site is null || password.Length==0 || password.Length>16384 || username.Length>4096){skipped++;continue;}
                entries.Add(new(site,username,password));
            }
            return new(entries,skipped);
        }
        catch(MalformedLineException){throw new InvalidDataException("O CSV está malformado. Exporte novamente do navegador.");}
    }
    public static List<PasswordEntry> Merge(IEnumerable<PasswordEntry> existing,IEnumerable<PasswordEntry> incoming,out int added,out int updated)
    {
        var values=new Dictionary<(string,string),PasswordEntry>();
        foreach(var entry in existing)values[(entry.Origin,entry.Username)]=entry;
        added=updated=0;
        foreach(var entry in incoming)
        {
            var key=(entry.Origin,entry.Username);
            if(!values.TryGetValue(key,out var previous))added++;
            else if(previous.Password!=entry.Password)updated++;
            values[key]=entry;
        }
        return values.Values.ToList();
    }
}
