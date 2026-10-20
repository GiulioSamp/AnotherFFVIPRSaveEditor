using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ffvi.SaveTool.Data;

namespace Ffvi.SaveTool.App.ViewModels;

public partial class VeldtViewModel(MainViewModel main) : TabViewModel(main)
{
    private bool _loading;

    public ObservableCollection<CheckItem> Formations { get; } = [];

    [ObservableProperty] private string _filter = "";
    [ObservableProperty] private string _countText = "";
    [ObservableProperty] private bool _hasVeldt;

    partial void OnFilterChanged(string value) => Rebuild();

    protected override void OnSaveChanged() => Rebuild();

    private void Rebuild()
    {
        _loading = true;
        Formations.Clear();
        var veldt = Save?.Veldt;
        HasVeldt = veldt is not null;
        if (veldt is null)
            CountText = Save is null ? "" : "No Veldt data in this save.";
        else
        {
            var filter = Filter.Trim();
            for (var i = 0; i < veldt.Encounters.Count; i++)
            {
                var name = VeldtFormations.NameFor(i);
                if (filter.Length > 0 && !name.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
                Formations.Add(new CheckItem(i, $"{i,3}  {name}", veldt.Encounters[i], OnSeenChanged));
            }
            UpdateCount();
        }
        _loading = false;
    }

    private void UpdateCount() =>
        CountText = $"Seen: {Save!.Veldt!.SeenCount} / {Save.Veldt.TotalCount}";

    private void OnSeenChanged(int index, bool seen)
    {
        if (_loading || Save?.Veldt is null) return;
        Save.Veldt.Encounters[index] = seen;
        UpdateCount();
        MarkDirty();
    }

    [RelayCommand] private void MarkVisible() => SetVisible(true);

    [RelayCommand] private void ClearVisible() => SetVisible(false);

    private void SetVisible(bool seen)
    {
        if (Save?.Veldt is null) return;
        foreach (var f in Formations) Save.Veldt.Encounters[f.Id] = seen;
        MarkDirty();
        Rebuild();
    }
}
