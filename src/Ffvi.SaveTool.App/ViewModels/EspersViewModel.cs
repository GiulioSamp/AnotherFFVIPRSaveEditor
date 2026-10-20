using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ffvi.SaveTool.Data;

namespace Ffvi.SaveTool.App.ViewModels;

public partial class EspersViewModel : TabViewModel
{
    private bool _loading;

    public ObservableCollection<CheckItem> Owned { get; } = [];
    public IReadOnlyList<EsperInfo> EquippedChoices { get; } =
        [new EsperInfo(0, "(none)"), .. Espers.All];

    [ObservableProperty] private EsperInfo? _equipped;
    [ObservableProperty] private bool _hasSave;
    [ObservableProperty] private bool _hasCharacter;

    public EspersViewModel(MainViewModel main) : base(main)
    {
        OnSaveChanged();
        OnContextChanged();
    }

    protected override void OnSaveChanged()
    {
        _loading = true;
        HasSave = Save is not null;
        Owned.Clear();
        if (Save is not null)
            foreach (var e in Espers.All)
                Owned.Add(new CheckItem(e.Id, $"{e.Id,3}  {e.Name}", Save.UserData.OwnedEsperIds.Contains(e.Id), OnOwnedChanged));
        _loading = false;
    }

    protected override void OnContextChanged()
    {
        _loading = true;
        HasCharacter = SelectedCharacter is not null;
        if (SelectedCharacter is not null)
        {
            // Ids outside the table fall back to "(none)" rather than keeping the previous character's value.
            var id = SelectedCharacter.EquippedEsperId;
            Equipped = EquippedChoices.FirstOrDefault(e => e.Id == id) ?? EquippedChoices[0];
        }
        _loading = false;
    }

    private void OnOwnedChanged(int id, bool owned)
    {
        if (_loading || Save is null) return;
        if (owned) Save.UserData.OwnedEsperIds.Add(id);
        else Save.UserData.OwnedEsperIds.Remove(id);
        MarkDirty();
    }

    partial void OnEquippedChanged(EsperInfo? value)
    {
        if (_loading || SelectedCharacter is null || value is null) return;
        SelectedCharacter.EquippedEsperId = value.Id;
        MarkDirty();
    }

    [RelayCommand] private void OwnAll() { foreach (var i in Owned) i.IsChecked = true; }

    [RelayCommand] private void OwnNone() { foreach (var i in Owned) i.IsChecked = false; }
}
