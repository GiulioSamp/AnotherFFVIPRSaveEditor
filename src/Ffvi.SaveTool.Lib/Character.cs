using System.Text.Json.Nodes;

namespace Ffvi.SaveTool;

public class Character
{
    public JsonObject Node { get; }
    public CharacterStats Stats { get; }
    public CharacterAbilities Abilities { get; }
    public Equipment Equipment { get; }
    public CharacterCommands Commands { get; }

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
        Commands = new CharacterCommands(node);
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

    // Level and experience must stay consistent: the game recalculates level from
    // currentExp after every battle, so a level written on its own is discarded (and the
    // player sees a spurious "Level Up" as it resets). Always set both together.
    public void SetLevel(int level)
    {
        level = Math.Clamp(level, Data.LevelGrowth.MinLevel, Data.LevelGrowth.MaxLevel);
        Stats.AdditionalLevel = level;
        CurrentExp = Data.LevelGrowth.ExpForLevel(level);
    }

    // Null when the character isn't in the base-stat table.
    public Data.RawStats? BaseStats => Data.CharacterBaseStats.ForCharacter(Id, JobId);

    public int GetBaseStat(TotalStat stat)
    {
        var b = BaseStats;
        if (b is null) return 0;
        return stat switch
        {
            TotalStat.Strength => b.Strength,
            TotalStat.Stamina => b.Stamina,
            TotalStat.Speed => b.Speed,
            TotalStat.Magic => b.Magic,
            TotalStat.Attack => b.Attack,
            TotalStat.Defense => b.Defense,
            TotalStat.MagicDefense => b.MagicDefense,
            TotalStat.Evasion => b.Evasion,
            TotalStat.MagicEvasion => b.MagicEvasion,
            _ => throw new ArgumentOutOfRangeException(nameof(stat)),
        };
    }

    public int GetTotalStat(TotalStat stat) => GetBaseStat(stat) + Stats.GetBonus(stat);

    // Never writes a negative bonus: the game only ever stores additive bonuses in the
    // addtional* fields and rejects saves containing negative values.
    public void SetTotalStat(TotalStat stat, int total) =>
        Stats.SetBonus(stat, Math.Max(0, total - GetBaseStat(stat)));

    // Level the game will derive from CurrentExp after the next battle.
    public int ImpliedLevel => Data.LevelGrowth.LevelForExp(CurrentExp);

    public bool HasLevelExpMismatch => ImpliedLevel != Stats.AdditionalLevel;

    internal void Commit()
    {
        Stats.Commit();
        Abilities.Commit();
        Equipment.Commit();
        Commands.Commit();
        _parentArray[_indexInParent] = JsonValue.Create(Node.ToJsonString(SaveFile.JsonOpts));
    }
}
