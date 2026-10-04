using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Ffvi.SaveTool;

namespace Ffvi.SaveTool.App.ViewModels;

/// <summary>Base for every tab. Exposes the selected character and a dirty hook from the main VM.</summary>
public abstract partial class TabViewModel : ObservableObject
{
    protected MainViewModel Main { get; }

    protected TabViewModel(MainViewModel main)
    {
        Main = main;
        main.PropertyChanged += OnMainChanged;
    }

    public SaveFile? Save => Main.Save;
    public Character? SelectedCharacter => Main.SelectedCharacter;

    protected void MarkDirty() => Main.MarkDirty();

    /// <summary>Called when the selected character or loaded save changes. Override to refresh state.</summary>
    protected virtual void OnContextChanged() { }

    /// <summary>Called only when the loaded save changes, before <see cref="OnContextChanged"/>. Override for save-wide state.</summary>
    protected virtual void OnSaveChanged() { }

    private void OnMainChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedCharacter))
            OnPropertyChanged(nameof(SelectedCharacter));
        else if (e.PropertyName == nameof(MainViewModel.Save))
        {
            OnPropertyChanged(nameof(Save));
            OnSaveChanged();
        }
        else return;
        OnContextChanged();
    }
}
