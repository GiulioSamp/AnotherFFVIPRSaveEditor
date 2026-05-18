namespace Ffvi.SaveTool.Data;

public record BushidoInfo(int Id, string Name);

public static class Bushido
{
    public const int FirstId = 124;
    public const int LastId  = 131;
    public const int ContentIdOffset = 330;
    public const string OwnerCharacterName = "Cyan";

    public static readonly IReadOnlyList<BushidoInfo> All =
    [
        new(124, "Fang"),
        new(125, "Sky"),
        new(126, "Tiger"),
        new(127, "Flurry"),
        new(128, "Dragon"),
        new(129, "Eclipse"),
        new(130, "Tempest"),
        new(131, "Oblivion"),
    ];

    public static BushidoInfo? ById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static string NameFor(int id) => ById(id)?.Name ?? $"#{id}";
}
