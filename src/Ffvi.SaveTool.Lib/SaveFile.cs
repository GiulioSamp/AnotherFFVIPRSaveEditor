using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ffvi.SaveTool;

public class SaveFile
{
    internal static readonly JsonSerializerOptions JsonOpts = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] Crlf = [0x0D, 0x0A];

    public string Path { get; }
    public JsonObject Top { get; }
    public UserData UserData { get; }
    public JsonObject? MapData { get; }
    public VeldtEncounters? Veldt { get; }

    private SaveFile(string path, JsonObject top, UserData userData, JsonObject? mapData, VeldtEncounters? veldt)
    {
        Path = path;
        Top = top;
        UserData = userData;
        MapData = mapData;
        Veldt = veldt;
    }

    public int SlotId => Top["id"]?.GetValue<int>() ?? -1;
    public string? Timestamp => Top["timeStamp"]?.GetValue<string>();
    public double PlayTime => Top["playTime"]?.GetValue<double>() ?? 0;

    public static SaveFile Load(string path)
    {
        var json = SaveCrypto.Decrypt(File.ReadAllBytes(path));
        var top = JsonNode.Parse(json)!.AsObject();
        var userDataNode = NestedJson.Unwrap(top, "userData").AsObject();
        var userData = new UserData(userDataNode);

        JsonObject? mapData = null;
        VeldtEncounters? veldt = null;
        if (top.ContainsKey("mapData"))
        {
            mapData = NestedJson.Unwrap(top, "mapData").AsObject();
            veldt = new VeldtEncounters(mapData);
        }

        return new SaveFile(path, top, userData, mapData, veldt);
    }

    public bool IsSlotFile() => Top.ContainsKey("id") && Top.ContainsKey("pictureData");

    public void Save() => Save(Path);

    public void Save(string outputPath)
    {
        BackupExisting(outputPath);
        UserData.Commit();
        NestedJson.Rewrap(Top, "userData", UserData.Node);

        if (MapData is not null)
        {
            Veldt?.Commit();
            NestedJson.Rewrap(Top, "mapData", MapData);
        }

        var json = Top.ToJsonString(JsonOpts);
        var encrypted = SaveCrypto.Encrypt(json);
        var framed = new byte[Bom.Length + encrypted.Length + Crlf.Length];
        Buffer.BlockCopy(Bom, 0, framed, 0, Bom.Length);
        Buffer.BlockCopy(encrypted, 0, framed, Bom.Length, encrypted.Length);
        Buffer.BlockCopy(Crlf, 0, framed, Bom.Length + encrypted.Length, Crlf.Length);

        File.WriteAllBytes(outputPath, framed);
    }

    public static string BackupDirectory { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ffvi.SaveTool", "backups");

    // Copies an existing target aside before it is overwritten. Throws on failure so the
    // original is never replaced without a backup.
    private static void BackupExisting(string path)
    {
        if (!File.Exists(path)) return;
        Directory.CreateDirectory(BackupDirectory);
        var baseName = System.IO.Path.Combine(BackupDirectory,
            $"{System.IO.Path.GetFileName(path)}.{DateTime.Now:yyyyMMdd-HHmmss}");
        var dest = baseName;
        for (var n = 1; File.Exists(dest); n++) dest = $"{baseName}-{n}";
        File.Copy(path, dest);
    }

    public static string DefaultSaveDirectory()
    {
        const string gameDir = "My Games/FINAL FANTASY VI PR/Steam";
        var steamRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "FINAL FANTASY VI PR", "Steam");

        if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            foreach (var steam in new[] { ".steam/steam", ".local/share/Steam" })
            {
                var root = System.IO.Path.Combine(home, steam, "steamapps/compatdata/1173820/pfx/drive_c/users/steamuser/Documents", gameDir);
                if (!Directory.Exists(root)) continue;
                steamRoot = root;
                break;
            }
        }

        if (!Directory.Exists(steamRoot)) return steamRoot;
        var first = Directory.GetDirectories(steamRoot).FirstOrDefault();
        return first ?? steamRoot;
    }
}
