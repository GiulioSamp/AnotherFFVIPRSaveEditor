using System.Text.Json.Nodes;

namespace Ffvi.SaveTool;

public class UserData
{
    public JsonObject Node { get; }
    public IReadOnlyList<Character> Characters { get; }
    public Inventory NormalInventory { get; }
    public Inventory ImportantInventory { get; }
    public Inventory WarehouseInventory { get; }
    public HashSet<int> OwnedEsperIds { get; }

    private readonly JsonObject _ownedCharsRoot;
    private readonly JsonObject _espersRoot;

    internal UserData(JsonObject node)
    {
        Node = node;
        // Inventory must be constructed BEFORE characters — Equipment needs a reference
        // to compute slot counts at commit time.
        NormalInventory = new Inventory(node, "normalOwnedItemList");
        ImportantInventory = new Inventory(node, "importantOwendItemList");
        WarehouseInventory = new Inventory(node, "warehouseItemList");

        _ownedCharsRoot = NestedJson.Unwrap(node, "ownedCharacterList").AsObject();
        var target = _ownedCharsRoot["target"]!.AsArray();
        var chars = new List<Character>(target.Count);
        for (var i = 0; i < target.Count; i++)
        {
            var charObj = JsonNode.Parse(target[i]!.GetValue<string>())!.AsObject();
            chars.Add(new Character(charObj, target, i, NormalInventory));
        }
        Characters = chars;

        _espersRoot = NestedJson.Unwrap(node, "ownedMagicStoneList").AsObject();
        OwnedEsperIds = new HashSet<int>(
            _espersRoot["target"]!.AsArray().Select(v => v?.GetValue<int>() ?? 0));
    }

    public int Gil
    {
        get => Node["owendGil"]?.GetValue<int>() ?? 0;
        set => Node["owendGil"] = value;
    }

    public int TotalGil
    {
        get => Node["totalGil"]?.GetValue<int>() ?? 0;
        set => Node["totalGil"] = value;
    }

    public int Steps
    {
        get => Node["steps"]?.GetValue<int>() ?? 0;
        set => Node["steps"] = value;
    }

    public int BattleCount
    {
        get => Node["battleCount"]?.GetValue<int>() ?? 0;
        set => Node["battleCount"] = value;
    }

    public int MonstersKilledCount
    {
        get => Node["monstersKilledCount"]?.GetValue<int>() ?? 0;
        set => Node["monstersKilledCount"] = value;
    }

    internal void Commit()
    {
        foreach (var c in Characters) c.Commit();
        NormalInventory.Commit();
        ImportantInventory.Commit();
        WarehouseInventory.Commit();
        NestedJson.Rewrap(Node, "ownedCharacterList", _ownedCharsRoot);

        var espersTarget = new JsonArray();
        foreach (var id in OwnedEsperIds.OrderBy(i => i)) espersTarget.Add(id);
        _espersRoot["target"] = espersTarget;
        NestedJson.Rewrap(Node, "ownedMagicStoneList", _espersRoot);
    }
}
