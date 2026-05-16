using System.Text.Json.Nodes;

namespace Ffvi.SaveTool;

public class CharacterAbilities
{
    public const int MagicCategoryKey = 7;
    public const int ContentIdOffset = 330;

    private readonly JsonObject _abilityListNode;
    private readonly JsonObject _abilityDictNode;
    private readonly JsonObject _characterNode;

    internal CharacterAbilities(JsonObject abilityListNode, JsonObject abilityDictNode, JsonObject characterNode)
    {
        _abilityListNode = abilityListNode;
        _abilityDictNode = abilityDictNode;
        _characterNode = characterNode;
    }

    public List<AbilityEntry> AllAbilities() => ParseTarget(_abilityListNode);

    public List<AbilityEntry> LearnedMagic()
    {
        var (_, target) = GetCategory(MagicCategoryKey);
        return target is null ? new List<AbilityEntry>() : ParseTargetArray(target);
    }

    public bool KnowsAbility(int abilityId) =>
        AllAbilities().Any(a => a.AbilityId == abilityId && a.SkillLevel >= 100);

    public void LearnAbility(int abilityId, int categoryKey, int contentId = -1)
    {
        if (contentId < 0) contentId = abilityId + ContentIdOffset;

        var target = _abilityListNode["target"]!.AsArray();
        var existing = false;
        for (var i = 0; i < target.Count; i++)
        {
            var ab = JsonNode.Parse(target[i]!.GetValue<string>())!.AsObject();
            if (ab["abilityId"]?.GetValue<int>() == abilityId)
            {
                ab["skillLevel"] = 100;
                target[i] = JsonValue.Create(ab.ToJsonString(SaveFile.JsonOpts));
                existing = true;
                break;
            }
        }
        if (!existing)
        {
            target.Add(JsonValue.Create(new JsonObject
            {
                ["abilityId"] = abilityId,
                ["contentId"] = contentId,
                ["skillLevel"] = 100,
            }.ToJsonString(SaveFile.JsonOpts)));
        }

        var (catIdx, catTarget) = GetCategory(categoryKey);
        if (catIdx == -1)
        {
            var keys = _abilityDictNode["keys"]!.AsArray();
            var values = _abilityDictNode["values"]!.AsArray();
            keys.Add(categoryKey);
            values.Add(JsonValue.Create(new JsonObject { ["target"] = new JsonArray() }.ToJsonString(SaveFile.JsonOpts)));
            (catIdx, catTarget) = GetCategory(categoryKey);
        }
        var inDict = false;
        for (var i = 0; i < catTarget!.Count; i++)
        {
            var ab = JsonNode.Parse(catTarget[i]!.GetValue<string>())!.AsObject();
            if (ab["abilityId"]?.GetValue<int>() == abilityId) { inDict = true; break; }
        }
        if (!inDict)
        {
            catTarget.Add(JsonValue.Create(new JsonObject
            {
                ["abilityId"] = abilityId,
                ["contentId"] = contentId,
                ["skillLevel"] = 100,
            }.ToJsonString(SaveFile.JsonOpts)));
            var values = _abilityDictNode["values"]!.AsArray();
            values[catIdx] = JsonValue.Create(new JsonObject { ["target"] = catTarget }.ToJsonString(SaveFile.JsonOpts));
        }
    }

    public void ForgetAbility(int abilityId, int categoryKey)
    {
        var target = _abilityListNode["target"]!.AsArray();
        for (var i = 0; i < target.Count; i++)
        {
            var ab = JsonNode.Parse(target[i]!.GetValue<string>())!.AsObject();
            if (ab["abilityId"]?.GetValue<int>() == abilityId)
            {
                ab["skillLevel"] = 0;
                target[i] = JsonValue.Create(ab.ToJsonString(SaveFile.JsonOpts));
                break;
            }
        }

        var (catIdx, catTarget) = GetCategory(categoryKey);
        if (catIdx == -1 || catTarget is null) return;
        for (var i = catTarget.Count - 1; i >= 0; i--)
        {
            var ab = JsonNode.Parse(catTarget[i]!.GetValue<string>())!.AsObject();
            if (ab["abilityId"]?.GetValue<int>() == abilityId)
                catTarget.RemoveAt(i);
        }
        var values = _abilityDictNode["values"]!.AsArray();
        values[catIdx] = JsonValue.Create(new JsonObject { ["target"] = catTarget }.ToJsonString(SaveFile.JsonOpts));
    }

    public void LearnSpell(int spellId) => LearnAbility(spellId, MagicCategoryKey);
    public void ForgetSpell(int spellId) => ForgetAbility(spellId, MagicCategoryKey);

    private (int idx, JsonArray? target) GetCategory(int key)
    {
        var keys = _abilityDictNode["keys"]!.AsArray();
        var values = _abilityDictNode["values"]!.AsArray();
        for (var i = 0; i < keys.Count; i++)
        {
            if (keys[i]?.GetValue<int>() == key)
            {
                var catObj = JsonNode.Parse(values[i]!.GetValue<string>())!.AsObject();
                return (i, catObj["target"]!.AsArray());
            }
        }
        return (-1, null);
    }

    private static List<AbilityEntry> ParseTarget(JsonObject root) =>
        ParseTargetArray(root["target"]!.AsArray());

    private static List<AbilityEntry> ParseTargetArray(JsonArray target)
    {
        var list = new List<AbilityEntry>(target.Count);
        foreach (var item in target)
        {
            var ab = JsonNode.Parse(item!.GetValue<string>())!.AsObject();
            list.Add(new AbilityEntry(
                ab["abilityId"]?.GetValue<int>() ?? 0,
                ab["contentId"]?.GetValue<int>() ?? 0,
                ab["skillLevel"]?.GetValue<int>() ?? 0));
        }
        return list;
    }

    internal void Commit()
    {
        NestedJson.Rewrap(_characterNode, "abilityList", _abilityListNode);
        NestedJson.Rewrap(_characterNode, "abilityDictionary", _abilityDictNode);
    }
}

public record AbilityEntry(int AbilityId, int ContentId, int SkillLevel);
