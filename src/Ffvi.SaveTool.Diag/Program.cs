// Ffvi.SaveTool.Diag — read-only inspector for FFVI Pixel Remaster saves.
// Decrypts the most recently modified slot save and prints a summary of every
// userData field plus the top-level fields, with previews. Useful for reverse-engineering
// schema fields we haven't yet mapped in the library.
// Never writes to disk. Safe to run on a live save folder.

using System.Text.Json.Nodes;
using Ffvi.SaveTool;

var savesDir = SaveFile.DefaultSaveDirectory();
if (!Directory.Exists(savesDir))
{
    Console.WriteLine($"Save folder not found at: {savesDir}");
    return 1;
}

var slotFile = Directory.GetFiles(savesDir)
    .Where(f => !Path.GetFileName(f).Equals("steam_autocloud.vdf", StringComparison.OrdinalIgnoreCase))
    .Select(f => (Path: f, Info: new FileInfo(f)))
    .Where(x => x.Info.Length > 30000)
    .OrderByDescending(x => x.Info.LastWriteTime)
    .Select(x => x.Path)
    .FirstOrDefault();

if (slotFile is null)
{
    Console.WriteLine($"No slot save files (>30 KB) found in: {savesDir}");
    return 1;
}

Console.WriteLine($"Inspecting most recently modified slot: {Path.GetFileName(slotFile)}");
Console.WriteLine($"  ({new FileInfo(slotFile).Length} bytes, mtime {new FileInfo(slotFile).LastWriteTime:yyyy-MM-dd HH:mm:ss})\n");

var save = SaveFile.Load(slotFile);
Console.WriteLine($"slot id: {save.SlotId}  |  timestamp: {save.Timestamp}  |  characters: {save.UserData.Characters.Count}");
Console.WriteLine($"gil: {save.UserData.Gil}  |  total gil: {save.UserData.TotalGil}  |  steps: {save.UserData.Steps}\n");

Console.WriteLine("=== top-level fields ===\n");
foreach (var (k, v) in save.Top)
{
    if (k == "pictureData") { Console.WriteLine($"--- {k} (skipped — large base64 PNG) ---\n"); continue; }
    var s = v is JsonValue jv && jv.TryGetValue<string>(out var str) ? str : v?.ToJsonString() ?? "";
    var preview = s.Length > 400 ? s[..400] + "..." : s;
    Console.WriteLine($"--- {k} ({s.Length} chars) ---");
    Console.WriteLine($"  {preview}\n");
}

Console.WriteLine("=== userData fields ===\n");
foreach (var (k, v) in save.UserData.Node)
{
    var s = v is JsonValue jv && jv.TryGetValue<string>(out var str) ? str : v?.ToJsonString() ?? "";
    var preview = s.Length > 300 ? s[..300] + "..." : s;
    Console.WriteLine($"--- {k} ({s.Length} chars) ---");
    Console.WriteLine($"  {preview}\n");
}

return 0;
