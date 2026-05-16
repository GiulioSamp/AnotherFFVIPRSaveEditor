using System.Text.Json.Nodes;

namespace Ffvi.SaveTool;

public class CharacterStats
{
    public JsonObject Node { get; }
    private readonly JsonObject _characterNode;

    internal CharacterStats(JsonObject node, JsonObject characterNode)
    {
        Node = node;
        _characterNode = characterNode;
    }

    private int Get(string key) => Node[key]?.GetValue<int>() ?? 0;
    private void Set(string key, int value) => Node[key] = value;

    public int CurrentHp { get => Get("currentHP"); set => Set("currentHP", value); }
    public int CurrentMp { get => Get("currentMP"); set => Set("currentMP", value); }

    public int AdditionalLevel { get => Get("addtionalLevel"); set => Set("addtionalLevel", value); }
    public int AdditionalMaxHp { get => Get("addtionalMaxHp"); set => Set("addtionalMaxHp", value); }
    public int AdditionalMaxMp { get => Get("addtionalMaxMp"); set => Set("addtionalMaxMp", value); }

    public int AdditionalPower { get => Get("addtionalPower"); set => Set("addtionalPower", value); }
    public int AdditionalVitality { get => Get("addtionalVitality"); set => Set("addtionalVitality", value); }
    public int AdditionalAgility { get => Get("addtionalAgility"); set => Set("addtionalAgility", value); }
    public int AdditionalIntelligence { get => Get("addtionalIntelligence"); set => Set("addtionalIntelligence", value); }
    public int AdditionalSpirit { get => Get("addtionalSpirit"); set => Set("addtionalSpirit", value); }
    public int AdditionalAttack { get => Get("addtionalAttack"); set => Set("addtionalAttack", value); }
    public int AdditionalDefence { get => Get("addtionalDefense"); set => Set("addtionalDefense", value); }
    public int AdditionalMagic { get => Get("addtionalMagic"); set => Set("addtionalMagic", value); }
    public int AdditionalLuck { get => Get("addtionalLuck"); set => Set("addtionalLuck", value); }
    public int AdditionalAccuracyRate { get => Get("addtionalAccuracyRate"); set => Set("addtionalAccuracyRate", value); }
    public int AdditionalEvasionRate { get => Get("addtionalEvasionRate"); set => Set("addtionalEvasionRate", value); }
    public int AdditionalCriticalRate { get => Get("addtionalCriticalRate"); set => Set("addtionalCriticalRate", value); }

    // "Ability" in the game's data model = "Magic" in player-facing terms.
    public int AdditionalMagicDefense { get => Get("addtionalAbilityDefense"); set => Set("addtionalAbilityDefense", value); }
    public int AdditionalMagicEvasionRate { get => Get("addtionalAbilityEvasionRate"); set => Set("addtionalAbilityEvasionRate", value); }

    internal void Commit() => NestedJson.Rewrap(_characterNode, "parameter", Node);
}
