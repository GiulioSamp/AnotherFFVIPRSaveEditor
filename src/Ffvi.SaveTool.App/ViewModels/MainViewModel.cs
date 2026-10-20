using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ffvi.SaveTool;
using Ffvi.SaveTool.Data;
using Ffvi.SaveTool.App.Views;

namespace Ffvi.SaveTool.App.ViewModels;

public sealed record CharacterItem(Character Character, string DisplayName);

public partial class MainViewModel : ObservableObject
{
    private readonly Window _window;
    private bool _loading;

    public MainViewModel(Window window)
    {
        _window = window;
        Characters = new CharactersViewModel(this);
        Inventory = new InventoryViewModel(this);
        Skills = new SkillsViewModel(this);
        Espers = new EspersViewModel(this);
        Veldt = new VeldtViewModel(this);
    }

    public CharactersViewModel Characters { get; }
    public InventoryViewModel Inventory { get; }
    public SkillsViewModel Skills { get; }
    public EspersViewModel Espers { get; }
    public VeldtViewModel Veldt { get; }

    public ObservableCollection<CharacterItem> CharacterItems { get; } = new();

    [NotifyPropertyChangedFor(nameof(IsLoaded), nameof(Title), nameof(StatusText))]
    [ObservableProperty] private SaveFile? _save;

    [NotifyPropertyChangedFor(nameof(SelectedCharacter))]
    [ObservableProperty] private CharacterItem? _selectedItem;

    [NotifyPropertyChangedFor(nameof(Title))]
    [ObservableProperty] private bool _isDirty;

    [NotifyPropertyChangedFor(nameof(StatusText))]
    [ObservableProperty] private string? _savedAt;

    public bool IsLoaded => Save is not null;
    public Character? SelectedCharacter => SelectedItem?.Character;

    public string Title => (IsDirty ? "*" : "")
        + (Save is null ? "FFVI Save Editor" : $"{Path.GetFileName(Save.Path)} - FFVI Save Editor");

    public string StatusText => Save is null
        ? "No file loaded"
        : $"{Path.GetFileName(Save.Path)}   Slot {Save.SlotId}   {CharacterItems.Count} characters"
          + (SavedAt is null ? "" : $"   Saved: {SavedAt}");

    public decimal? Gil
    {
        get => Save?.UserData.Gil;
        set
        {
            if (Save is null || value is not { } v || (int)v == Save.UserData.Gil) return;
            Save.UserData.SetGil((int)v);
            OnPropertyChanged(nameof(TotalGil));
            Touch();
        }
    }

    public decimal? TotalGil
    {
        get => Save?.UserData.TotalGil;
        set { if (Save is not null && value is { } v && (int)v != Save.UserData.TotalGil) { Save.UserData.TotalGil = (int)v; Touch(); } }
    }

    public decimal? Steps
    {
        get => Save?.UserData.Steps;
        set { if (Save is not null && value is { } v && (int)v != Save.UserData.Steps) { Save.UserData.Steps = (int)v; Touch(); } }
    }

    public void MarkDirty() => IsDirty = true;

    private void Touch() { if (!_loading) MarkDirty(); }

    // Saves store the localised name; append the canonical English name when it differs.
    internal static string DisplayName(Character c)
    {
        var canonical = CharacterRoster.Resolve(c.Id, c.JobId)?.EnglishName;
        return canonical is null || canonical == c.Name ? c.Name : $"{c.Name} ({canonical})";
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (!await ConfirmDiscardAsync()) return;

        var options = new FilePickerOpenOptions { Title = "Open save file", AllowMultiple = false };
        var dir = SaveFile.DefaultSaveDirectory();
        if (Directory.Exists(dir))
            options.SuggestedStartLocation = await _window.StorageProvider.TryGetFolderFromPathAsync(dir);
        var files = await _window.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0) return;

        var path = files[0].TryGetLocalPath();
        if (path is null) { await Error("Only local files are supported."); return; }

        SaveFile loaded;
        try
        {
            loaded = SaveFile.Load(path);
            if (!loaded.IsSlotFile())
            {
                await Error("This is not a save slot file.");
                return;
            }
        }
        catch (Exception ex)
        {
            await Error($"Could not load {Path.GetFileName(path)}.\n\n{ex.Message}");
            return;
        }

        _loading = true;
        try
        {
            SelectedItem = null;
            CharacterItems.Clear();
            foreach (var c in loaded.UserData.Characters) CharacterItems.Add(new(c, DisplayName(c)));
            Save = loaded;
            OnPropertyChanged(nameof(Gil));
            OnPropertyChanged(nameof(TotalGil));
            OnPropertyChanged(nameof(Steps));
            SelectedItem = CharacterItems.FirstOrDefault();
            SavedAt = null;
            IsDirty = false;
        }
        finally { _loading = false; }
    }

    [RelayCommand]
    private Task SaveAsync() => TrySaveAsync(null);

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (Save is null) return;
        CommitPendingEdit();
        var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save as",
            SuggestedFileName = Path.GetFileName(Save.Path),
        });
        var path = file?.TryGetLocalPath();
        if (path is not null) await TrySaveAsync(path);
    }

    // NumericUpDown only commits typed text on focus loss, so drop focus before saving.
    private void CommitPendingEdit() => _window.FocusManager?.Focus(null);

    private async Task<bool> TrySaveAsync(string? path)
    {
        if (Save is null) return false;
        CommitPendingEdit();
        try
        {
            if (path is null) Save.Save(); else Save.Save(path);
        }
        catch (Exception ex)
        {
            await Error($"Could not save.\n\n{ex.Message}");
            return false;
        }
        IsDirty = false;
        SavedAt = DateTime.Now.ToString("HH:mm:ss");
        return true;
    }

    /// <summary>Returns false if the user cancelled or a requested save failed.</summary>
    public async Task<bool> ConfirmDiscardAsync()
    {
        CommitPendingEdit();
        if (!IsDirty) return true;
        return await MessageDialog.ShowYesNoCancel(_window, "Unsaved changes", "Save changes?") switch
        {
            DialogResult.Yes => await TrySaveAsync(null),
            DialogResult.No => true,
            _ => false,
        };
    }

    [RelayCommand]
    private void Exit() => _window.Close();

    private Task Error(string message) => MessageDialog.ShowOk(_window, "Error", message);
}
