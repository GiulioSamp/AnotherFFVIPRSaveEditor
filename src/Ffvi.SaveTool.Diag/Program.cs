using Ffvi.SaveTool;
using Ffvi.SaveTool.Data;

// Verifies character identity resolution after merging the upstream skill-owner work.
// 1) every character in the local saves still resolves to itself
// 2) both candidate late-game id layouts resolve correctly (ids move between versions)
// 3) the skill tab constants still derive the right job

int fail = 0;

Console.WriteLine("=== 1. real saves ===");
var savesDir = SaveFile.DefaultSaveDirectory();
var seen = new SortedDictionary<int, (string Name, int JobId)>();
foreach (var f in Directory.GetFiles(savesDir).Where(f => new FileInfo(f).Length > 30000 && !f.EndsWith(".backup")))
{
    SaveFile save;
    try { save = SaveFile.Load(f); } catch { continue; }
    foreach (var c in save.UserData.Characters)
        if (c.Name != "??????") seen[c.Id] = (c.Name, c.JobId);
}
foreach (var (id, v) in seen)
{
    var got = CharacterRoster.Resolve(id, v.JobId)?.EnglishName ?? "(null)";
    if (got != v.Name) { fail++; Console.WriteLine($"  MISMATCH id {id} job {v.JobId}: save '{v.Name}' -> '{got}'"); }
}
Console.WriteLine($"  {seen.Count} characters checked, all resolve to their own name: {fail == 0}");
Console.WriteLine($"  Mog keeps his own stats: Str {CharacterBaseStats.ForCharacter(16, 11)?.Strength}");
Console.WriteLine($"  NPC moogle gets none:    {CharacterBaseStats.ForCharacter(7, 11)?.Strength.ToString() ?? "none"}");

Console.WriteLine("\n=== 2. late-game layouts ===");
void Check(string layout, (int Id, int Job, string Expect)[] rows)
{
    Console.WriteLine($"  {layout}");
    foreach (var (id, job, expect) in rows)
    {
        var got = CharacterRoster.Resolve(id, job)?.EnglishName ?? "(null)";
        var bs = CharacterBaseStats.ForCharacter(id, job);
        var ok = got == expect;
        if (!ok) fail++;
        Console.WriteLine($"    id {id} job {job,2} expect {expect,-8} -> {got,-8} Str {(bs is null ? "-" : bs.Strength.ToString()),-3} {(ok ? "ok" : "MISMATCH")}");
    }
}
Check("as reported by the issue (Celes at 22):", [
    (22, 7, "Celes"), (23, 3, "Cyan"), (24, 17, "??????"), (25, 12, "Gau")]);
Check("as the reference table assumes:", [
    (22, 3, "Cyan"), (23, 17, "??????"), (24, 12, "Gau"), (25, 7, "Celes")]);

Console.WriteLine("\n=== 3. skill tab owner constants -> job ===");
foreach (var (skill, id, who) in new[]
{
    ("Rages", CharacterRoster.GauId, "Gau"), ("Bushido", CharacterRoster.CyanId, "Cyan"),
    ("Lore", CharacterRoster.StragoId, "Strago"), ("Blitz", CharacterRoster.SabinId, "Sabin"),
    ("Dance", CharacterRoster.MogId, "Mog"),
})
{
    var e = CharacterRoster.ForId(id);
    var ok = e?.EnglishName == who;
    if (!ok) fail++;
    Console.WriteLine($"  {skill,-8} id {id,2} -> {e?.EnglishName,-8} job {e?.JobId,-3} {(ok ? "ok" : "MISMATCH")}");
}

Console.WriteLine($"\nfailures: {fail}");
return fail == 0 ? 0 : 1;
