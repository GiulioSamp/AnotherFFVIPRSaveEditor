namespace Ffvi.SaveTool.Data;

public record EsperInfo(int Id, string Name);

public static class Espers
{
    public const int FirstId = 62;
    public const int LastId = 88;

    public static readonly IReadOnlyList<EsperInfo> All =
    [
        new(62, "Ramuh"),       new(63, "Kirin"),        new(64, "Siren"),
        new(65, "Cait Sith"),   new(66, "Ifrit"),        new(67, "Shiva"),
        new(68, "Unicorn"),     new(69, "Maduin"),       new(70, "Catoblepas"),
        new(71, "Phantom"),     new(72, "Carbuncle"),    new(73, "Bismark"),
        new(74, "Golem"),       new(75, "Zona Seeker"),  new(76, "Seraph"),
        new(77, "Quetzalli"),   new(78, "Fenrir"),       new(79, "Valigarmanda"),
        new(80, "Midgardsormr"),new(81, "Lakshmi"),      new(82, "Alexander"),
        new(83, "Phoenix"),     new(84, "Odin"),         new(85, "Bahamut"),
        new(86, "Ragnarok"),    new(87, "Crusader"),     new(88, "Raiden"),
    ];

    public static EsperInfo? ById(int id) => All.FirstOrDefault(e => e.Id == id);
    public static string NameFor(int id) => id == 0 ? "(none)" : ById(id)?.Name ?? $"#{id}";
}
