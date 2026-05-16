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

    private SaveFile(string path, JsonObject top, UserData userData)
    {
        Path = path;
        Top = top;
        UserData = userData;
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
        return new SaveFile(path, top, userData);
    }

    public bool IsSlotFile() => Top.ContainsKey("id") && Top.ContainsKey("pictureData");

    public void Save() => Save(Path);

    public void Save(string outputPath)
    {
        UserData.Commit();
        NestedJson.Rewrap(Top, "userData", UserData.Node);

        var json = Top.ToJsonString(JsonOpts);
        var encrypted = SaveCrypto.Encrypt(json);
        var framed = new byte[Bom.Length + encrypted.Length + Crlf.Length];
        Buffer.BlockCopy(Bom, 0, framed, 0, Bom.Length);
        Buffer.BlockCopy(encrypted, 0, framed, Bom.Length, encrypted.Length);
        Buffer.BlockCopy(Crlf, 0, framed, Bom.Length + encrypted.Length, Crlf.Length);

        File.WriteAllBytes(outputPath, framed);
    }

    public static string DefaultSaveDirectory()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var steamRoot = System.IO.Path.Combine(docs, "My Games", "FINAL FANTASY VI PR", "Steam");
        if (!Directory.Exists(steamRoot)) return steamRoot;
        var first = Directory.GetDirectories(steamRoot).FirstOrDefault();
        return first ?? steamRoot;
    }
}
