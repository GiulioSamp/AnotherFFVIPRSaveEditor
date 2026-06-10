using Ffvi.SaveTool;

// Regression check: library-only load + save must produce identical JSON for a
// clean game-written save. Any diff below means an editor "repair" fired or we broke parity.

var root = AppContext.BaseDirectory;
var dir = new DirectoryInfo(root);
while (dir is not null && !Directory.Exists(System.IO.Path.Combine(dir.FullName, "FFVI saved normal")))
    dir = dir.Parent;
if (dir is null) { Console.WriteLine("No 'FFVI saved normal' folder"); return 1; }

var sourcePath = Directory.GetFiles(System.IO.Path.Combine(dir.FullName, "FFVI saved normal")).First();
var outPath = sourcePath + ".lib-roundtrip";

var originalJson = SaveCrypto.Decrypt(File.ReadAllBytes(sourcePath));
var save = SaveFile.Load(sourcePath);
save.Save(outPath);
var roundtrippedJson = SaveCrypto.Decrypt(File.ReadAllBytes(outPath));
File.Delete(outPath);

Console.WriteLine($"original len:     {originalJson.Length}");
Console.WriteLine($"roundtripped len: {roundtrippedJson.Length}");
Console.WriteLine($"identical?        {originalJson == roundtrippedJson}");

if (originalJson != roundtrippedJson)
{
    var minLen = Math.Min(originalJson.Length, roundtrippedJson.Length);
    for (var i = 0; i < minLen; i++)
    {
        if (originalJson[i] != roundtrippedJson[i])
        {
            var s = Math.Max(0, i - 80);
            Console.WriteLine($"\nfirst diff at {i}:");
            Console.WriteLine($"  orig: ...{originalJson[s..Math.Min(minLen, i + 80)]}...");
            Console.WriteLine($"  new:  ...{roundtrippedJson[s..Math.Min(minLen, i + 80)]}...");
            break;
        }
    }
    return 1;
}

Console.WriteLine("\nLibrary roundtrip parity holds after the dedupe/invariant changes.");
return 0;
