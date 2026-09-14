namespace Ffvi.SaveTool.Data;

public record RosterEntry(int Id, int JobId, string EnglishName, bool IsNpc);

// Canonical character roster.
//
// Never identify a character by the save's `name` string: it holds the LOCALISED name
// (a save made in Chinese, Japanese and so on stores it in that language) and the player
// can rename characters in-game. Use Resolve(id, jobId) instead.
//
// Neither key is sufficient on its own:
//
//   - `id` is a roster slot and is NOT stable across game versions. Ids 1 to 19 are
//     confirmed against real Pixel Remaster saves, but a player reported ids 22 to 25
//     holding Celes, Cyan, ?????? and Gau, where the table below (taken from the Go
//     reference editor) has Cyan, ??????, Gau and Celes. The late-game ids here are
//     therefore suspect and are only used as a last resort.
//   - `jobId` is the character's class identity and follows the canonical FFVI character
//     order, so it survives any reshuffling of slots. But it is not unique: Mog shares
//     job 11 with the nine NPC moogles, and Wedge shares job 18 with Biggs.
//
// Resolve prefers an entry where both agree, then falls back to jobId, which is what
// actually distinguishes one playable character from another.
// Source: KiameV/final-fantasy-vi-save-editor, models/pr/baseOffsets.go.
public static class CharacterRoster
{
    public static readonly IReadOnlyList<RosterEntry> All =
    [
        new( 1,  1, "Terra",  false),
        new( 2, 18, "Wedge",  false),
        new( 3, 18, "Biggs",  false),
        // Kefka is absent from the Go reference's table but appears in real saves as
        // id 4 / job 21 (observed in Pixel Remaster saves from the opening sequence).
        new( 4, 21, "Kefka",  true),
        new( 5,  2, "Locke",  false),
        new( 6, 11, "Moglin", true),
        new( 7, 11, "Mogret", true),
        new( 8, 11, "Moggie", true),
        new( 9, 11, "Molulu", true),
        new(10, 11, "Moghan", true),
        new(11, 11, "Moguel", true),
        new(12, 11, "Mogsy",  true),
        new(13, 11, "Mogwin", true),
        new(14, 11, "Mugmug", true),
        new(15, 11, "Cosmog", true),
        new(16, 11, "Mog",    false),
        new(17,  5, "Edgar",  false),
        new(18,  6, "Sabin",  false),
        new(19,  4, "Shadow", false),
        // Ids from here down are unverified against real saves, see the note above.
        // The JobIds are the reliable part and are what Resolve keys off.
        new(20, 15, "Banon",  false),
        new(22,  3, "Cyan",   false),
        new(23, 17, "??????", true),
        new(24, 12, "Gau",    false),
        new(25,  7, "Celes",  false),
        new(26, 10, "Setzer", false),
        new(27, 20, "Maduin", true),
        new(28,  8, "Strago", false),
        new(29,  9, "Relm",   false),
        new(30, 16, "Leo",    false),
        new(32, 14, "Umaro",  false),
        new(33, 13, "Gogo",   false),
    ];

    private static readonly Dictionary<int, RosterEntry> ById = All.ToDictionary(e => e.Id);

    // Identify the character a save entry refers to.
    //
    // 1. An entry whose id AND jobId both match is unambiguous. This covers the verified
    //    low ids, including telling Mog apart from the moogles that share his job.
    // 2. Otherwise trust jobId, which carries character identity across slot reshuffles.
    //    Where several entries share a job, the single non-NPC one wins (Mog over the
    //    moogles, Wedge over Biggs).
    // 3. Only if the job is unknown do we fall back to the id.
    public static RosterEntry? Resolve(int id, int jobId)
    {
        if (ById.TryGetValue(id, out var byId) && byId.JobId == jobId) return byId;

        var byJob = All.Where(e => e.JobId == jobId).ToList();
        if (byJob.Count == 1) return byJob[0];
        if (byJob.Count > 1)
        {
            var playable = byJob.Where(e => !e.IsNpc).ToList();
            if (playable.Count == 1) return playable[0];
        }

        return ById.GetValueOrDefault(id);
    }

    public static RosterEntry? ForId(int id) => ById.GetValueOrDefault(id);

    // Canonical English name for a roster id. This is for the skill tab labels, which are
    // defined by the roster constants below. For a character read out of a save, go through
    // Resolve instead: the save's id alone can point at the wrong entry.
    public static string EnglishNameFor(int id) => ForId(id)?.EnglishName ?? $"#{id}";

    // Character ids that own each learnable skill set. These index the table above; the
    // skill tabs derive the job id from them, which is what the save is matched on.
    public const int GauId    = 24; // Rages
    public const int CyanId   = 22; // Bushido
    public const int StragoId = 28; // Lore
    public const int SabinId  = 18; // Blitz
    public const int MogId    = 16; // Dance
    public const int SetzerId = 26; // Slot
}
