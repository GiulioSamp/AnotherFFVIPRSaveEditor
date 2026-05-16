using System.Text.Json.Nodes;

namespace Ffvi.SaveTool;

public class Character
{
    public JsonObject Node { get; }
    public CharacterStats Stats { get; }
    public CharacterAbilities Abilities { get; }
    public Equipment Equipment { get; }

    private readonly JsonArray _parentArray;
    private readonly int _indexInParent;

    internal Character(JsonObject node, JsonArray parentArray, int indexInParent, Inventory inventory)
    {
        Node = node;
        _parentArray = parentArray;
        _indexInParent = indexInParent;
        Stats = new CharacterStats(NestedJson.Unwrap(node, "parameter").AsObject(), node);
        Abilities = new CharacterAbilities(
            NestedJson.Unwrap(node, "abilityList").AsObject(),
            NestedJson.Unwrap(node, "abilityDictionary").AsObject(),
            node);
        Equipment = new Equipment(NestedJson.Unwrap(node, "equipmentList").AsObject(), node, inventory);
    }

    public int Id => Node["id"]?.GetValue<int>() ?? -1;
    public string Name => Node["name"]?.GetValue<string>() ?? "";

    public int JobId
    {
        get => Node["jobId"]?.GetValue<int>() ?? 0;
        set => Node["jobId"] = value;
    }

    public int CurrentExp
    {
        get => Node["currentExp"]?.GetValue<int>() ?? 0;
        set => Node["currentExp"] = value;
    }

    public int EquippedEsperId
    {
        get => Node["magicStoneId"]?.GetValue<int>() ?? 0;
        set => Node["magicStoneId"] = value;
    }

    internal void Commit()
    {
        Stats.Commit();
        Abilities.Commit();
        Equipment.Commit();
        _parentArray[_indexInParent] = JsonValue.Create(Node.ToJsonString(SaveFile.JsonOpts));
    }
}
