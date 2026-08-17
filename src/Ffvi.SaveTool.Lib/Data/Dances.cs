namespace Ffvi.SaveTool.Data;

public record DanceInfo(int Id, string Name);

// Not present in the reference editor's tables. Confirmed two ways: a real save's Mog
// character has a clean, contiguous 164-171 block in abilityList using contentId = abilityId
// + 330, the same offset already used for Blitz/Bushido/Lore; and KiameV/final-fantasy-vi-save-editor's
// models/consts/pr/dances.go independently has the same ids and offset. Names are the Pixel
// Remaster localization, which differs from the SNES-era English names (e.g. "Wind Song" is
// now "Wind Rhapsody").
public static class Dances
{
    public const int FirstId = 164;
    public const int LastId  = 171;
    public const int ContentIdOffset = 330;
    public const string OwnerCharacterName = "Mog";

    public static readonly IReadOnlyList<DanceInfo> All =
    [
        new(164, "Wind Rhapsody"),
        new(165, "Forest Nocturne"),
        new(166, "Desert Lullaby"),
        new(167, "Love Serenade"),
        new(168, "Earth Blues"),
        new(169, "Water Harmony"),
        new(170, "Twilight Requiem"),
        new(171, "Snowman Rondo"),
    ];

    public static DanceInfo? ById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static string NameFor(int id) => ById(id)?.Name ?? $"#{id}";
}
