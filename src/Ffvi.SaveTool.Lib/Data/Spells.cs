namespace Ffvi.SaveTool.Data;

public static class Spells
{
    public const int FirstId = 31;
    public const int LastId = 84;

    public static readonly IReadOnlyList<SpellInfo> All =
    [
        new(31, "Cure"),     new(32, "Cura"),      new(33, "Curaga"),
        new(34, "Raise"),    new(35, "Arise"),     new(36, "Poisona"),
        new(37, "Esuna"),    new(38, "Regen"),     new(39, "Reraise"),
        new(40, "Fire"),     new(41, "Blizzard"),  new(42, "Thunder"),
        new(43, "Poison"),   new(44, "Drain"),     new(45, "Fira"),
        new(46, "Blizzara"), new(47, "Thundara"),  new(48, "Bio"),
        new(49, "Firaga"),   new(50, "Blizzaga"),  new(51, "Thundaga"),
        new(52, "Break"),    new(53, "Death"),     new(54, "Holy"),
        new(55, "Flare"),    new(56, "Gravity"),   new(57, "Graviga"),
        new(58, "Banish"),   new(59, "Meteor"),    new(60, "Ultima"),
        new(61, "Quake"),    new(62, "Tornado"),   new(63, "Meltdown"),
        new(64, "Libra"),    new(65, "Slow"),      new(66, "Rasp"),
        new(67, "Silence"),  new(68, "Protect"),   new(69, "Sleep"),
        new(70, "Confuse"),  new(71, "Haste"),     new(72, "Stop"),
        new(73, "Berserk"),  new(74, "Float"),     new(75, "Imp"),
        new(76, "Reflect"),  new(77, "Shell"),     new(78, "Vanish"),
        new(79, "Hastega"),  new(80, "Slowga"),    new(81, "Osmose"),
        new(82, "Teleport"), new(83, "Quick"),     new(84, "Dispel"),
    ];

    public static SpellInfo? ById(int id) => All.FirstOrDefault(s => s.Id == id);

    public static string NameFor(int id) => ById(id)?.Name ?? $"#{id}";
}

public record SpellInfo(int Id, string Name);
