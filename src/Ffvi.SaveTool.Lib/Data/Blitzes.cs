namespace Ffvi.SaveTool.Data;

public record BlitzInfo(int Id, string Name);

public static class Blitzes
{
    public const int FirstId = 132;
    public const int LastId  = 139;
    public const int ContentIdOffset = 330;
    public const string OwnerCharacterName = "Sabin";

    public static readonly IReadOnlyList<BlitzInfo> All =
    [
        new(132, "Raging Fist"),
        new(133, "Aura Cannon"),
        new(134, "Meteor Strike"),
        new(135, "Rising Phoenix"),
        new(136, "Chakra"),
        new(137, "Razor Gale"),
        new(138, "Soul Spiral"),
        new(139, "Phantom Rush"),
    ];

    public static BlitzInfo? ById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static string NameFor(int id) => ById(id)?.Name ?? $"#{id}";
}
