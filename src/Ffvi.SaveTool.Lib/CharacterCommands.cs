using System.Text.Json.Nodes;

namespace Ffvi.SaveTool;

// Wraps a character's commandList (battle command slots). FFVI characters have 8 slots,
// each holding a command id (see Ffvi.SaveTool.Data.Commands for the id table).
// Editing these can soft-lock the game if assigned to characters who don't legitimately
// own the command (e.g. Magitek on a non-Magitek-Knight character).
public class CharacterCommands
{
    public List<int> Slots { get; }

    // Snapshot of slot values at load time. The editor's GUI uses this as the
    // allowed-command set (along with universal safe commands) to avoid cross-class
    // assignments that would corrupt the save.
    public IReadOnlyList<int> OriginalSlots { get; }

    private readonly JsonObject _characterNode;

    internal CharacterCommands(JsonObject characterNode)
    {
        _characterNode = characterNode;
        var list = NestedJson.Unwrap(characterNode, "commandList").AsObject();
        var loaded = list["target"]!.AsArray()
            .Select(v => v?.GetValue<int>() ?? 0)
            .ToList();
        Slots = new List<int>(loaded);
        OriginalSlots = loaded.AsReadOnly();
    }

    // Commands that are safe to set on any character regardless of class.
    private static readonly int[] UniversalCommandIds = [4, 1, 2, 3, 5];

    // Commands a character (by job id) owns later in the story and may be given early.
    // Unverified in-game. Revert is not listed: the game swaps it in during Trance.
    private static readonly Dictionary<int, int[]> EarlyCommandIds = new()
    {
        [1] = [8], // Terra: Trance
    };

    // Commands the character already had at load plus the universal ones and any early
    // signature command for the job, [none] first then by name.
    public IReadOnlyList<Data.CommandInfo> AllowedCommands(int jobId)
    {
        var allowed = new HashSet<int>(UniversalCommandIds);
        foreach (var id in OriginalSlots) allowed.Add(id);
        if (EarlyCommandIds.TryGetValue(jobId, out var early)) allowed.UnionWith(early);
        return Data.Commands.All
            .Where(cmd => allowed.Contains(cmd.Id))
            .OrderBy(cmd => cmd.Id == Data.Commands.NoneId ? 0 : 1)
            .ThenBy(cmd => cmd.Name)
            .ToList();
    }

    public void ResetToOriginal()
    {
        Slots.Clear();
        Slots.AddRange(OriginalSlots);
    }

    internal void Commit()
    {
        var arr = new JsonArray();
        foreach (var c in Slots) arr.Add(c);
        var node = new JsonObject { ["target"] = arr };
        NestedJson.Rewrap(_characterNode, "commandList", node);
    }
}
