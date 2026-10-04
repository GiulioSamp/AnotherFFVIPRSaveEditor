using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ffvi.SaveTool;

namespace Ffvi.SaveTool.App.ViewModels;

public partial class InventoryRow(int stackIndex, string label, int count, Action<InventoryRow, int> onCountChanged) : ObservableObject
{
    public int StackIndex { get; } = stackIndex;
    public string Label { get; } = label;

    [ObservableProperty] private decimal? _count = count;

    partial void OnCountChanged(decimal? oldValue, decimal? newValue)
    {
        if (newValue is null) Count = oldValue;
        else onCountChanged(this, (int)newValue.Value);
    }
}

public record ItemChoice(int Id, string Label);

public partial class InventoryViewModel(MainViewModel main) : TabViewModel(main)
{
    private bool _loading;

    public ObservableCollection<InventoryRow> Rows { get; } = [];
    public ObservableCollection<ItemChoice> AddChoices { get; } = [];
    public decimal MaxCount => Inventory.MaxStackCount;

    [ObservableProperty] private string _filter = "";
    [ObservableProperty] private ItemChoice? _selectedChoice;
    [ObservableProperty] private decimal? _addCount = 1;
    [ObservableProperty] private bool _hasSave;

    partial void OnFilterChanged(string value) => Refresh();

    protected override void OnSaveChanged() => Refresh();

    /// <summary>Rebuilds the rows from the model. Called by other tabs that change the inventory (e.g. equipping).</summary>
    public void Refresh()
    {
        _loading = true;
        Rows.Clear();
        AddChoices.Clear();
        HasSave = Save is not null;
        if (Save is not null)
        {
            var inv = Save.UserData.NormalInventory;
            var filter = Filter.Trim();
            for (var i = 0; i < inv.Stacks.Count; i++)
            {
                var s = inv.Stacks[i];
                if (Equipment.IsEmptyPlaceholder(s.ItemId)) continue;
                var info = Ffvi.SaveTool.Data.Items.NormalById(s.ItemId);
                var label = info is null ? $"#{s.ItemId}" : $"[{info.Category}] {info.Name}";
                if (filter.Length > 0 && !label.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
                Rows.Add(new InventoryRow(i, label, s.Count, OnCountChanged));
            }
            foreach (var i in inv.AddableItems())
                AddChoices.Add(new ItemChoice(i.Id, $"[{i.Category}] {i.Name}"));
        }
        SelectedChoice = null;
        _loading = false;
    }

    private void OnCountChanged(InventoryRow row, int count)
    {
        if (_loading || Save is null) return;
        var inv = Save.UserData.NormalInventory;
        var stack = inv.Stacks[row.StackIndex];
        if (stack.Count == count) return;
        var merged = inv.SetStack(row.StackIndex, stack.ItemId, count);
        MarkDirty();
        if (merged) Refresh();
    }

    [RelayCommand]
    private void Remove(InventoryRow row)
    {
        if (Save is null) return;
        Save.UserData.NormalInventory.RemoveAt(row.StackIndex);
        MarkDirty();
        Refresh();
    }

    [RelayCommand]
    private void Add()
    {
        if (Save is null || SelectedChoice is null) return;
        Save.UserData.NormalInventory.Add(SelectedChoice.Id, (int)(AddCount ?? 1));
        MarkDirty();
        Refresh();
    }

    [RelayCommand]
    private void MaxAll()
    {
        if (Save is null) return;
        Save.UserData.NormalInventory.MaxAll();
        MarkDirty();
        Refresh();
    }
}
