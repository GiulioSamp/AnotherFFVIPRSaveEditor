using Ffvi.SaveTool;

// Final verification: no-op rewrite of File 1 with the padding fix. Confirms the output
// envelope now matches the game's own framing (padded base64 + BOM + CRLF) and that the
// rewritten base64 length is valid (divisible by 4).

var savesDir = SaveFile.DefaultSaveDirectory();
string? targetPath = null;
foreach (var f in Directory.GetFiles(savesDir).Where(f => new FileInfo(f).Length > 30000 && !f.EndsWith(".backup")))
{
    if (SaveFile.Load(f).SlotId == 1) { targetPath = f; break; }
}
if (targetPath is null) { Console.WriteLine("slot id=1 not found"); return 1; }
var backupPath = targetPath + ".backup";

File.Copy(backupPath, targetPath, overwrite: true);
var originalJson = SaveCrypto.Decrypt(File.ReadAllBytes(targetPath));

var save = SaveFile.Load(targetPath);
save.Save();

var bytes = File.ReadAllBytes(targetPath);
var rewrittenJson = SaveCrypto.Decrypt(bytes);
Console.WriteLine($"JSON identical: {originalJson == rewrittenJson}");

// Validate envelope: BOM + base64 (length % 4 == 0) + CRLF
var hasBom = bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
var hasCrlf = bytes[^2] == 0x0D && bytes[^1] == 0x0A;
var b64Len = bytes.Length - 3 - 2;
Console.WriteLine($"BOM: {hasBom}  CRLF: {hasCrlf}  base64 length: {b64Len} (mod 4 = {b64Len % 4})");
Console.WriteLine($"tail: ...{System.Text.Encoding.ASCII.GetString(bytes[^10..^2])}\\r\\n");
Console.WriteLine();
Console.WriteLine("Load the entry stamped 10/06/2026 22:43 in-game. This is the definitive writer test.");
return 0;
