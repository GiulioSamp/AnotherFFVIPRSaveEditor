namespace Ffvi.SaveTool.App.ViewModels;

public class CharactersViewModel(MainViewModel main) : TabViewModel(main)
{
    public StatsViewModel Stats { get; } = new(main);
    public MagicViewModel Magic { get; } = new(main);
    public EquipmentViewModel Equipment { get; } = new(main);
    public CommandsViewModel Commands { get; } = new(main);
}
