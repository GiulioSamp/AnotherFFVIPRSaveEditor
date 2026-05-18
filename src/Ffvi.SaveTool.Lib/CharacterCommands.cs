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
