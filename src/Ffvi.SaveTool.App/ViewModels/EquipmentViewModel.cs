using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Ffvi.SaveTool.Data;

namespace Ffvi.SaveTool.App.ViewModels;

public sealed record EquipOption(int Id, string Display);

public partial class EquipSlot(string label, int key, ItemCategory category, int emptyId, Action<EquipSlot> onChanged) : ObservableObject
{
    public string Label { get; } = label;
    public int Key { get; } = key;
    public ItemCategory Category { get; } = category;
    public int EmptyId { get; } = emptyId;
    public IReadOnlyList<EquipOption> BaseOptions { get; set; } = [];

    [ObservableProperty] private IReadOnlyList<EquipOption> _options = [];

    [ObservableProperty] private EquipOption? _selected;

    partial void OnSelectedChanged(EquipOption? value) => onChanged(this);
}

public class EquipmentViewModel : TabViewModel
{
    private bool _loading;

    public EquipmentViewModel(MainViewModel main) : base(main)
    {
        Slots =
        [
            new("Weapon", Equipment.WeaponKey, ItemCategory.Weapon, Equipment.EmptyWeaponShieldId, OnSlotChanged),
            new("Shield", Equipment.ShieldKey, ItemCategory.Shield, Equipment.EmptyWeaponShieldId, OnSlotChanged),
            new("Helmet", Equipment.HelmetKey, ItemCategory.Helmet, Equipment.EmptyHelmetId, OnSlotChanged),
            new("Armor", Equipment.ArmorKey, ItemCategory.Armor, Equipment.EmptyArmorId, OnSlotChanged),
            new("Relic 1", Equipment.Relic1Key, ItemCategory.Relic, Equipment.EmptyRelicId, OnSlotChanged),
            new("Relic 2", Equipment.Relic2Key, ItemCategory.Relic, Equipment.EmptyRelicId, OnSlotChanged),
        ];
        foreach (var s in Slots) s.BaseOptions = BuildOptions(s.Category, s.EmptyId);
    }

    public IReadOnlyList<EquipSlot> Slots { get; }

    private static List<EquipOption> BuildOptions(ItemCategory category, int emptyId)
    {
        var list = new List<EquipOption> { new(emptyId, "(empty)") };
        list.AddRange(Items.Normal
            .Where(i => i.Category == category)
            .OrderBy(i => i.Name)
            .Select(i => new EquipOption(i.Id, i.Name)));
        return list;
    }

    protected override void OnContextChanged()
    {
        _loading = true;
        foreach (var s in Slots)
        {
            if (SelectedCharacter is not { } c) { s.Options = s.BaseOptions; s.Selected = null; continue; }
            var id = c.Equipment.GetSlot(s.Key);
            var match = s.BaseOptions.FirstOrDefault(o => o.Id == id);
            if (match is null)
            {
                match = new EquipOption(id, $"Unknown ({id})");
                s.Options = [..s.BaseOptions, match];
            }
            else s.Options = s.BaseOptions;
            s.Selected = match;
        }
        _loading = false;
    }

    private void OnSlotChanged(EquipSlot slot)
    {
        if (_loading || SelectedCharacter is not { } c || Save is null || slot.Selected is not { } o) return;
        c.Equipment.SetSlot(slot.Key, o.Id);
        // The game unequips items that aren't in the inventory on load, so make sure the item is owned.
        Save.UserData.NormalInventory.EnsureOwned(o.Id);
        Main.Inventory.Refresh();
        MarkDirty();
    }
}
