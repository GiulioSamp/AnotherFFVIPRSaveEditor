using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Ffvi.SaveTool.Data;

namespace Ffvi.SaveTool.App.ViewModels;

public partial class StatRow : ObservableObject
{
    private readonly Func<Character, int> _get;
    private readonly Action<Character, int> _set;
    private readonly Func<Character, decimal>? _floor;
    private readonly Action? _changed;
    private readonly Action _dirty;
    private Character? _character;
    private bool _loading;

    public string Label { get; }
    public string Description { get; }
    public decimal Maximum { get; }

    [ObservableProperty] private decimal _minimum;
    [ObservableProperty] private decimal? _value;
    [ObservableProperty] private string _baseText = "—";
    [ObservableProperty] private bool _isEnabled;

    public StatRow(string label, int min, int max, string description, Action dirty,
        Func<Character, int> get, Action<Character, int> set,
        Func<Character, decimal>? floor = null, Action? changed = null)
    {
        Label = label;
        _minimum = min;
        Maximum = max;
        Description = description;
        _dirty = dirty;
        _get = get;
        _set = set;
        _floor = floor;
        _changed = changed;
    }

    public void Load(Character? c)
    {
        _loading = true;
        _character = c;
        IsEnabled = c is not null;
        if (c is null)
        {
            BaseText = "—";
            Value = null;
        }
        else
        {
            if (_floor is not null)
            {
                Minimum = _floor(c);
                BaseText = c.BaseStats is not null ? Minimum.ToString("0") : "—";
            }
            Value = Math.Clamp(_get(c), Minimum, Maximum);
        }
        _loading = false;
    }

    partial void OnValueChanged(decimal? value)
    {
        if (_loading || _character is null || value is null) return;
        _set(_character, (int)value.Value);
        _changed?.Invoke();
        _dirty();
    }
}

public partial class StatsViewModel : TabViewModel
{
    private readonly List<StatRow> _all = [];

    public IReadOnlyList<StatRow> Vitals { get; }
    public IReadOnlyList<StatRow> CoreStats { get; }
    public IReadOnlyList<StatRow> Combat { get; }
    public IReadOnlyList<StatRow> BonusOnly { get; }

    [ObservableProperty] private string _expText = "";
    [ObservableProperty] private bool _expMismatch;

    public StatsViewModel(MainViewModel main) : base(main)
    {
        const string vestigial = "Not documented as a real stat in Pixel Remaster. Present in the save data but likely vestigial.";

        Vitals =
        [
            Row("Current HP", 0, 99999, "Current health. Party members are capped at 9999 in-game.", c => c.Stats.CurrentHp, (c, v) => c.Stats.CurrentHp = v),
            Row("+ Max HP", 0, 99999, "Bonus added to base Max HP. Total Max HP = base (from level) + this. Level-based base isn't shown here yet.", c => c.Stats.AdditionalMaxHp, (c, v) => c.Stats.AdditionalMaxHp = v),
            Row("Current MP", 0, 99999, "Current MP. Party members are capped at 999 in-game. Will be clamped to Max MP on load.", c => c.Stats.CurrentMp, (c, v) => c.Stats.CurrentMp = v),
            Row("+ Max MP", 0, 9999, "Bonus added to base Max MP. Total Max MP = base (from level) + this. Level-based base isn't shown here yet.", c => c.Stats.AdditionalMaxMp, (c, v) => c.Stats.AdditionalMaxMp = v),
            Row("Level", 1, 99, "Character level. Changing this also sets the character's experience to the total required for that level, because the game recalculates level from experience after every battle. Max HP and MP are derived from level, so they follow automatically. The other stats do not grow with level in FFVI: they only change through equipment and espers.",
                c => c.Stats.AdditionalLevel, (c, v) => c.SetLevel(v), RefreshExp),
        ];

        CoreStats =
        [
            Total("Strength", TotalStat.Strength, "Physical damage stat. Effective cap is 128 even though it can be raised higher. Doubled and added to Attack in the damage formula."),
            Total("Stamina", TotalStat.Stamina, "Resists Death attacks. Increases Regen heal, Poison/Sap damage taken, Tintinnabulum step-healing."),
            Total("Speed", TotalStat.Speed, "Fills the ATB gauge faster. +20 baseline plus Haste/Slow effects."),
            Total("Magic", TotalStat.Magic, "Magic Power. Increases magical damage. No 128 cap, unlike Strength. Sabin's Blitzes use this too."),
        ];

        Combat =
        [
            Total("Attack", TotalStat.Attack, "Battle Power (weapon-based). Added to (Strength x 2) for physical damage. Normally only changed by weapons."),
            Total("Defense", TotalStat.Defense, "Reduces physical damage. Formula: damage * (255 - Defense) / 256 + 1."),
            Total("Magic Defense", TotalStat.MagicDefense, "Reduces magical damage. Same formula as Defense."),
            Total("Evasion", TotalStat.Evasion, "Physical block %. Block value = (255 - Evasion x 2) + 1."),
            Total("Magic Evasion", TotalStat.MagicEvasion, "Magic block %. Same formula as Evasion."),
        ];

        BonusOnly =
        [
            Row("+ Hit Rate", 0, 255, "Accuracy. Reduces miss chance. Mainly an enemy-side stat in vanilla mechanics.", c => c.Stats.AdditionalAccuracyRate, (c, v) => c.Stats.AdditionalAccuracyRate = v),
            Row("+ Critical Rate", 0, 255, "Critical hit chance.", c => c.Stats.AdditionalCriticalRate, (c, v) => c.Stats.AdditionalCriticalRate = v),
            Row("+ Luck", 0, 255, vestigial, c => c.Stats.AdditionalLuck, (c, v) => c.Stats.AdditionalLuck = v),
            Row("+ Intelligence", 0, 255, vestigial, c => c.Stats.AdditionalIntelligence, (c, v) => c.Stats.AdditionalIntelligence = v),
            Row("+ Spirit", 0, 255, vestigial, c => c.Stats.AdditionalSpirit, (c, v) => c.Stats.AdditionalSpirit = v),
        ];

        OnContextChanged();
    }

    private StatRow Row(string label, int min, int max, string description,
        Func<Character, int> get, Action<Character, int> set, Action? changed = null)
    {
        var row = new StatRow(label, min, max, description, MarkDirty, get, set, changed: changed);
        _all.Add(row);
        return row;
    }

    private StatRow Total(string label, TotalStat stat, string description)
    {
        var row = new StatRow(label, 0, 255, description, MarkDirty,
            c => c.GetTotalStat(stat), (c, v) => c.SetTotalStat(stat, v),
            floor: c => c.GetBaseStat(stat));
        _all.Add(row);
        return row;
    }

    private void RefreshExp()
    {
        var c = SelectedCharacter;
        if (c is null) { ExpText = ""; ExpMismatch = false; return; }
        var lvl = c.Stats.AdditionalLevel;
        ExpMismatch = c.HasLevelExpMismatch;
        var warn = ExpMismatch
            ? $"   Warning: this experience total corresponds to level {c.ImpliedLevel}, so the game will reset the level after the next battle."
            : "";
        ExpText = $"Experience: {c.CurrentExp:N0} (level {lvl} requires {LevelGrowth.ExpForLevel(lvl):N0}).{warn}";
    }

    protected override void OnContextChanged()
    {
        foreach (var row in _all) row.Load(SelectedCharacter);
        RefreshExp();
    }
}
