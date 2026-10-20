using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ffvi.SaveTool.Data;

namespace Ffvi.SaveTool.App.ViewModels;

public sealed record SkillOwnerOption(int Id, string Display);

/// <summary>One skill sub-tab (Rages, Bushido, ...): owner picker, filter, and a learned checklist.</summary>
public partial class SkillListViewModel : TabViewModel
{
    private readonly string _name;
    private readonly int _ownerRosterId;
    private readonly string _ownerEnglishName;
    private readonly int _firstId, _lastId, _offset;
    private readonly IReadOnlyList<(int Id, string Name)> _items;
    private bool _populating;

    public SkillListViewModel(MainViewModel main, string name, int ownerRosterId, string ownerEnglishName,
        int firstId, int lastId, int offset, IEnumerable<(int Id, string Name)> items) : base(main)
    {
        _name = name;
        _ownerRosterId = ownerRosterId;
        _ownerEnglishName = ownerEnglishName;
        _firstId = firstId;
        _lastId = lastId;
        _offset = offset;
        _items = items.ToList();
        Refresh();
    }

    public ObservableCollection<SkillOwnerOption> Owners { get; } = new();
    public ObservableCollection<CheckItem> Rows { get; } = new();

    [ObservableProperty] private SkillOwnerOption? _selectedOwner;
    [ObservableProperty] private string _filter = "";
    [ObservableProperty] private string _header = "";
    [ObservableProperty] private bool _hasOwner;

    private int? ManualOwnerId => SelectedOwner is { Id: not -1 } o ? o.Id : null;

    partial void OnSelectedOwnerChanged(SkillOwnerOption? value) { if (!_populating) Refresh(); }
    partial void OnFilterChanged(string value) => Refresh();

    protected override void OnSaveChanged() => Refresh();

    private Character? AutoOwner() =>
        Save is null ? null : CharacterRoster.FindOwner(Save.UserData.Characters, _ownerRosterId);

    private Character? GetOwner() => ManualOwnerId is int id
        ? Save?.UserData.Characters.FirstOrDefault(c => c.Id == id)
        : AutoOwner();

    private void PopulateOwners()
    {
        var manual = ManualOwnerId;
        _populating = true;
        Owners.Clear();
        if (Save is not null)
        {
            var auto = AutoOwner();
            Owners.Add(new(-1, $"Auto-detect ({(auto is null ? "not found" : MainViewModel.DisplayName(auto))})"));
            foreach (var c in CharacterRoster.OwnerCandidates(Save.UserData.Characters, _ownerRosterId)) Owners.Add(new(c.Id, MainViewModel.DisplayName(c)));
        }
        SelectedOwner = Owners.FirstOrDefault(o => o.Id == (manual ?? -1));
        _populating = false;
    }

    private void Refresh()
    {
        PopulateOwners();
        var owner = GetOwner();
        _populating = true;
        Rows.Clear();
        HasOwner = owner is not null;
        if (owner is null)
        {
            Header = ManualOwnerId is null
                ? $"{_name} is {_ownerEnglishName}'s skill. {_ownerEnglishName} isn't in this save yet."
                : $"{_name}: selected character isn't in this save.";
        }
        else
        {
            var learned = new HashSet<int>(owner.Abilities.LearnedSkillsInRange(_firstId, _lastId).Select(a => a.AbilityId));
            var filter = Filter.Trim();
            foreach (var (id, name) in _items)
            {
                if (filter.Length > 0
                    && !name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    && !id.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;
                Rows.Add(new CheckItem(id, $"{id,4}  {name}", learned.Contains(id), OnItemChanged));
            }
            UpdateHeader(owner);
        }
        _populating = false;
    }

    private void UpdateHeader(Character owner)
    {
        var count = owner.Abilities.LearnedSkillsInRange(_firstId, _lastId).Count();
        var filter = Filter.Trim();
        Header = filter.Length > 0
            ? $"{MainViewModel.DisplayName(owner)}'s {_name}: {count} / {_items.Count} learned. Showing {Rows.Count} matching \"{filter}\"."
            : $"{MainViewModel.DisplayName(owner)}'s {_name}: {count} / {_items.Count} learned.";
    }

    private void OnItemChanged(int id, bool learned)
    {
        if (_populating || GetOwner() is not { } owner) return;
        if (learned) owner.Abilities.LearnSkill(id, _offset);
        else owner.Abilities.ForgetSkill(id);
        UpdateHeader(owner);
        MarkDirty();
    }

    // Bulk actions cover the full ability set, not just the filtered rows.
    private void SetAll(bool learned)
    {
        if (GetOwner() is not { } owner) return;
        foreach (var (id, _) in _items)
        {
            if (learned) owner.Abilities.LearnSkill(id, _offset);
            else owner.Abilities.ForgetSkill(id);
        }
        MarkDirty();
        Refresh();
    }

    [RelayCommand] private void LearnAll() => SetAll(true);
    [RelayCommand] private void ForgetAll() => SetAll(false);
}
