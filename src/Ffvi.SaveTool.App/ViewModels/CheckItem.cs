using CommunityToolkit.Mvvm.ComponentModel;

namespace Ffvi.SaveTool.App.ViewModels;

/// <summary>One row of a checklist (spells, skills, espers, Veldt). Writes through to the model on toggle.</summary>
public partial class CheckItem(int id, string label, bool isChecked, Action<int, bool> onChanged) : ObservableObject
{
    public int Id { get; } = id;
    public string Label { get; } = label;

    [ObservableProperty] private bool _isChecked = isChecked;

    partial void OnIsCheckedChanged(bool value) => onChanged(Id, value);
}
