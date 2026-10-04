using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ffvi.SaveTool.App.ViewModels;

public sealed record CommandOption(int Id, string Display);

public partial class CommandSlot(string label, int index, Action<CommandSlot> onChanged) : ObservableObject
{
    public string Label { get; } = label;
    public int Index { get; } = index;

    [ObservableProperty] private IReadOnlyList<CommandOption> _options = [];
    [ObservableProperty] private CommandOption? _selected;

    partial void OnSelectedChanged(CommandOption? value) => onChanged(this);
}

public partial class CommandsViewModel : TabViewModel
{
    private bool _loading;

    public CommandsViewModel(MainViewModel main) : base(main)
    {
        Slots = Enumerable.Range(0, 8).Select(i => new CommandSlot($"Slot {i + 1}", i, OnSlotChanged)).ToList();
    }

    public IReadOnlyList<CommandSlot> Slots { get; }

    protected override void OnContextChanged()
    {
        _loading = true;
        try
        {
            if (SelectedCharacter is not { } c)
            {
                foreach (var s in Slots) { s.Options = []; s.Selected = null; }
                return;
            }
            var allowed = c.Commands.AllowedCommands(c.JobId)
                .Select(cmd => new CommandOption(cmd.Id, $"{cmd.Name} ({cmd.Id})"))
                .ToList();
            foreach (var s in Slots)
            {
                var id = s.Index < c.Commands.Slots.Count ? c.Commands.Slots[s.Index] : Data.Commands.NoneId;
                var options = new List<CommandOption>(allowed);
                var match = options.FirstOrDefault(o => o.Id == id);
                if (match is null)
                {
                    match = new CommandOption(id, $"Unknown ({id})");
                    options.Add(match);
                }
                s.Options = options;
                s.Selected = match;
            }
        }
        finally { _loading = false; }
    }

    private void OnSlotChanged(CommandSlot slot)
    {
        if (_loading || SelectedCharacter is not { } c || slot.Selected is not { } o) return;
        if (slot.Index >= c.Commands.Slots.Count) return;
        c.Commands.Slots[slot.Index] = o.Id;
        MarkDirty();
    }

    [RelayCommand]
    private void Reset()
    {
        if (SelectedCharacter is not { } c) return;
        c.Commands.ResetToOriginal();
        OnContextChanged();
        MarkDirty();
    }
}
