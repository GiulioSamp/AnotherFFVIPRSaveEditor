using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Ffvi.SaveTool.Data;

namespace Ffvi.SaveTool.App.ViewModels;

public partial class MagicViewModel(MainViewModel main) : TabViewModel(main)
{
    private bool _loading;

    public ObservableCollection<CheckItem> Spells { get; } = new();

    protected override void OnContextChanged()
    {
        _loading = true;
        Spells.Clear();
        if (SelectedCharacter is { } c)
        {
            var learned = new HashSet<int>(c.Abilities.LearnedMagic().Select(a => a.AbilityId));
            foreach (var s in Data.Spells.All)
                Spells.Add(new CheckItem(s.Id, $"{s.Id,3}  {s.Name}", learned.Contains(s.Id), OnToggled));
        }
        _loading = false;
    }

    private void OnToggled(int id, bool learned)
    {
        if (_loading || SelectedCharacter is not { } c) return;
        if (learned) c.Abilities.LearnSpell(id); else c.Abilities.ForgetSpell(id);
        MarkDirty();
    }

    [RelayCommand] private void LearnAll() => SetAll(true);
    [RelayCommand] private void ForgetAll() => SetAll(false);

    private void SetAll(bool learned)
    {
        if (SelectedCharacter is null) return;
        foreach (var s in Spells) s.IsChecked = learned;
    }
}
