namespace Ffvi.SaveTool.Data;

public record CommandInfo(int Id, string Name);

// Battle command IDs. Each character has 8 command slots in commandList.
// Editing these can soft-lock or crash the game if a character is given a command
// they don't actually own (e.g. Magitek on someone who isn't a Magitek Knight).
public static class Commands
{
    public const int NoneId = 4;

    public static readonly IReadOnlyList<CommandInfo> All =
    [
        new( 4, "[none]"),  new( 1, "Attack"),   new( 2, "Defend"),
        new( 3, "Items"),   new( 5, "Row"),      new( 6, "Skip"),
        new( 7, "Magitek"), new( 8, "Trance"),   new( 9, "Revert"),
        new(10, "Steal"),   new(11, "Mug"),      new(12, "Bushido"),
        new(13, "Throw"),   new(14, "Tools"),    new(15, "Blitz"),
        new(16, "Runic"),   new(17, "Lore"),     new(18, "Sketch"),
        new(19, "Control"), new(20, "Slot"),     new(21, "Gil Toss"),
        new(22, "Dance"),   new(23, "Rage"),     new(24, "Leap"),
        new(25, "Mimic"),   new(26, "Magic"),    new(27, "Pray"),
        new(28, "Shock"),   new(29, "Possess"),  new(30, "Jump"),
        new(31, "Dualcast"),
    ];

    public static CommandInfo? ById(int id) => All.FirstOrDefault(c => c.Id == id);
    public static string NameFor(int id) => ById(id)?.Name ?? $"#{id}";
}
