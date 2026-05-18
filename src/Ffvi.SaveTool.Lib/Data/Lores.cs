namespace Ffvi.SaveTool.Data;

public record LoreInfo(int Id, string Name);

public static class Lores
{
    public const int FirstId = 140;
    public const int LastId  = 163;
    public const int ContentIdOffset = 330;
    public const string OwnerCharacterName = "Strago";

    public static readonly IReadOnlyList<LoreInfo> All =
    [
        new(140, "Doom"),
        new(141, "Roulette"),
        new(142, "Tsunami"),
        new(143, "Aqua Breath"),
        new(144, "Aero"),
        new(145, "1000 Needles"),
        new(146, "Mighty Guard"),
        new(147, "Revenge Blast"),
        new(148, "White Wind"),
        new(149, "Lv. 5 Death"),
        new(150, "Lv. 4 Flare"),
        new(151, "Lv. 3 Confuse"),
        new(152, "Reflect ???"),
        new(153, "Lv. ? Holy"),
        new(154, "Traveler"),
        new(155, "Force Field"),
        new(156, "Dischord"),
        new(157, "Bad Breath"),
        new(158, "Transfusion"),
        new(159, "Rippler"),
        new(160, "Stone"),
        new(161, "Quasar"),
        new(162, "Grand Delta"),
        new(163, "Self-Destruct"),
    ];

    public static LoreInfo? ById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static string NameFor(int id) => ById(id)?.Name ?? $"#{id}";
}
