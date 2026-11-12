using System.Security.Cryptography;
using System.Text.Json;

namespace Volt;

static class PasswordVault
{
    static string FilePath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VOLT","passwords.dpapi");
    static T Locked<T>(Func<T> operation)
    {
        using var gate=new Mutex(false,"Local\\VOLT.PasswordVault");bool held=false;
        try
        {
            try{held=gate.WaitOne(TimeSpan.FromSeconds(10));}catch(AbandonedMutexException){held=true;}
            if(!held)throw new IOException("Cofre ocupado.");
            return operation();
        }
        finally{if(held)gate.ReleaseMutex();}
    }
    static List<PasswordEntry> Read()
    {
        if(!File.Exists(FilePath))return [];
        if(new FileInfo(FilePath).Length>20*1024*1024)throw new CryptographicException();
        byte[] plain=ProtectedData.Unprotect(File.ReadAllBytes(FilePath),null,DataProtectionScope.CurrentUser);
        try
        {
            var entries=JsonSerializer.Deserialize<List<PasswordEntry>>(plain)??throw new CryptographicException();
            if(entries.Any(e=>e is null || e.Origin is null || e.Username is null || e.Password is null || PasswordRules.Origin(e.Origin)!=e.Origin))throw new CryptographicException();
            return entries;
        }
        finally{CryptographicOperations.ZeroMemory(plain);}
    }
    static void Write(List<PasswordEntry> entries)
    {
        byte[] plain=JsonSerializer.SerializeToUtf8Bytes(entries);byte[] encrypted;
        if(plain.Length>19*1024*1024)
        {CryptographicOperations.ZeroMemory(plain);throw new InvalidDataException("O cofre atingiu o limite de tamanho. Nenhuma entrada foi alterada.");}
        try{encrypted=ProtectedData.Protect(plain,null,DataProtectionScope.CurrentUser);}
        finally{CryptographicOperations.ZeroMemory(plain);}
        if(encrypted.Length>20*1024*1024)throw new InvalidDataException("O cofre atingiu o limite de tamanho. Nenhuma entrada foi alterada.");
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        string temp=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllBytes(temp,encrypted);File.Move(temp,FilePath,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public static List<PasswordEntry> Load()=>Locked(Read);
    public static (List<PasswordEntry> Entries,int Added,int Updated,int Skipped) Import(string path)
    {
        var parsed=PasswordRules.ReadCsv(path);
        if(parsed.Entries.Count==0)throw new InvalidDataException("Nenhuma senha de site válida foi encontrada no CSV.");
        return Locked(()=>
        {
            var entries=PasswordRules.Merge(Read(),parsed.Entries,out int added,out int updated);
            Write(entries);return(entries,added,updated,parsed.Skipped);
        });
    }
    public static List<PasswordEntry> Remove(PasswordEntry selected)=>Locked(()=>
    {
        var entries=Read();entries.RemoveAll(e=>e.Origin==selected.Origin&&e.Username==selected.Username);Write(entries);return entries;
    });
}
