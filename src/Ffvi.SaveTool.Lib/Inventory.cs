using System.Text.Json.Nodes;

namespace Ffvi.SaveTool;

public class Inventory
{
    public const int MaxStackCount = 99;

    private readonly JsonObject _wrapperNode;
    private readonly JsonObject _parentNode;
    private readonly string _parentKey;
    public List<ItemStack> Stacks { get; private set; }

    internal Inventory(JsonObject parentNode, string parentKey)
    {
        _parentNode = parentNode;
        _parentKey = parentKey;
        _wrapperNode = NestedJson.Unwrap(parentNode, parentKey).AsObject();
        Stacks = ParseStacks(_wrapperNode["target"]!.AsArray());
    }

    private static List<ItemStack> ParseStacks(JsonArray target)
    {
        var list = new List<ItemStack>(target.Count);
        foreach (var item in target)
        {
            var obj = JsonNode.Parse(item!.GetValue<string>())!.AsObject();
            list.Add(new ItemStack(
                obj["contentId"]?.GetValue<int>() ?? 0,
                obj["count"]?.GetValue<int>() ?? 0));
        }
        return list;
    }

    public void Set(int index, int itemId, int count) => Stacks[index] = new ItemStack(itemId, count);

    public void Add(int itemId, int count)
    {
        var existing = Stacks.FindIndex(s => s.ItemId == itemId);
        if (existing >= 0)
            Stacks[existing] = Stacks[existing] with { Count = Math.Min(MaxStackCount, Stacks[existing].Count + count) };
        else
            Stacks.Add(new ItemStack(itemId, Math.Min(MaxStackCount, count)));
    }

    public void RemoveAt(int index) => Stacks.RemoveAt(index);

    public void Clear() => Stacks.Clear();

    // Merges duplicate contentId stacks in place (first-seen order, counts summed and
    // capped). The game rejects saves with duplicate inventory entries, and equipment
    // count validation reads per-item totals — so duplicates must never reach disk.
    public void MergeDuplicates()
    {
        var merged = new List<ItemStack>(Stacks.Count);
        var indexById = new Dictionary<int, int>();
        foreach (var s in Stacks)
        {
            if (indexById.TryGetValue(s.ItemId, out var at))
                merged[at] = merged[at] with { Count = Math.Min(MaxStackCount, merged[at].Count + s.Count) };
            else
            {
                indexById[s.ItemId] = merged.Count;
                merged.Add(s);
            }
        }
        Stacks.Clear();
        Stacks.AddRange(merged);
    }

    internal void Commit()
    {
        MergeDuplicates();
        var target = new JsonArray();
        foreach (var s in Stacks)
        {
            // Drop empty rows and known crash-triggering item IDs (per Go reference saver.go).
            if (s.ItemId == 0 || s.Count == 0) continue;
            if (s.ItemId is 184 or 243) continue;
            target.Add(JsonValue.Create(new JsonObject
            {
                ["contentId"] = s.ItemId,
                ["count"] = s.Count,
            }.ToJsonString(SaveFile.JsonOpts)));
        }
        _wrapperNode["target"] = target;
        NestedJson.Rewrap(_parentNode, _parentKey, _wrapperNode);
    }
}

public record ItemStack(int ItemId, int Count);
