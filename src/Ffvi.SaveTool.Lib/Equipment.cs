using System.Text.Json.Nodes;

namespace Ffvi.SaveTool;

public class Equipment
{
    public const int WeaponKey = 1;
    public const int ShieldKey = 2;
    public const int ArmorKey = 3;
    public const int HelmetKey = 4;
    public const int Relic1Key = 5;
    public const int Relic2Key = 6;

    // Per the Go reference saver.go: empty placeholders the game expects per slot type.
    // Note: armor/helmet are SWAPPED compared to the "EmptyArmor"/"EmptyHelmet" constants
    // in the Go reference's inventory.go — saver.go is the authoritative source.
    public const int EmptyWeaponShieldId = 93;
    public const int EmptyHelmetId = 198;
    public const int EmptyArmorId = 199;
    public const int EmptyRelicId = 200;

    private readonly Dictionary<int, int> _slotItemIds = new();
    private readonly JsonObject _characterNode;
    private readonly Inventory _inventory;

    internal Equipment(JsonObject equipNode, JsonObject characterNode, Inventory inventory)
    {
        _characterNode = characterNode;
        _inventory = inventory;

        var keys = equipNode["keys"]!.AsArray();
        var values = equipNode["values"]!.AsArray();
        for (var i = 0; i < keys.Count; i++)
        {
            var slotKey = keys[i]?.GetValue<int>() ?? 0;
            var obj = JsonNode.Parse(values[i]!.GetValue<string>())!.AsObject();
            _slotItemIds[slotKey] = obj["contentId"]?.GetValue<int>() ?? 0;
        }
    }

    public int GetSlot(int slotKey) => _slotItemIds.GetValueOrDefault(slotKey, 0);
    public void SetSlot(int slotKey, int itemId) => _slotItemIds[slotKey] = itemId;

    public int Weapon { get => GetSlot(WeaponKey); set => SetSlot(WeaponKey, value); }
    public int Shield { get => GetSlot(ShieldKey); set => SetSlot(ShieldKey, value); }
    public int Armor { get => GetSlot(ArmorKey); set => SetSlot(ArmorKey, value); }
    public int Helmet { get => GetSlot(HelmetKey); set => SetSlot(HelmetKey, value); }
    public int Relic1 { get => GetSlot(Relic1Key); set => SetSlot(Relic1Key, value); }
    public int Relic2 { get => GetSlot(Relic2Key); set => SetSlot(Relic2Key, value); }

    // Rebuilds equipmentList at commit time, with each slot's `count` mirroring the
    // current inventory count of that item. The game validates this: a mismatch
    // (e.g. count=1 in equipment slot but count=3 in inventory) causes the save to be
    // rejected as corrupted. Default to 1 if item isn't in inventory at all.
    internal void Commit()
    {
        var keys = new JsonArray();
        var values = new JsonArray();
        foreach (var (slotKey, itemId) in _slotItemIds.OrderBy(kv => kv.Key))
        {
            var count = _inventory.Stacks
                .Where(s => s.ItemId == itemId)
                .Sum(s => s.Count);
            if (count == 0) count = 1;
            keys.Add(slotKey);
            var entry = new JsonObject
            {
                ["contentId"] = itemId,
                ["count"] = count,
            }.ToJsonString(SaveFile.JsonOpts);
            values.Add(JsonValue.Create(entry));
        }
        var equipNode = new JsonObject { ["keys"] = keys, ["values"] = values };
        NestedJson.Rewrap(_characterNode, "equipmentList", equipNode);
    }
}
