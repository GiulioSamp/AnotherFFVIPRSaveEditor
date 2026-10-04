using Ffvi.SaveTool.Data;

namespace Ffvi.SaveTool.App.ViewModels;

public class SkillsViewModel(MainViewModel main) : TabViewModel(main)
{
    public SkillListViewModel Rages { get; } = new(main, "Rages", CharacterRoster.GauId, "Gau",
        Data.Rages.FirstId, Data.Rages.LastId, Data.Rages.ContentIdOffset, Data.Rages.All.Select(r => (r.Id, r.Name)));
    public SkillListViewModel Bushido { get; } = new(main, "Bushido", CharacterRoster.CyanId, "Cyan",
        Data.Bushido.FirstId, Data.Bushido.LastId, Data.Bushido.ContentIdOffset, Data.Bushido.All.Select(b => (b.Id, b.Name)));
    public SkillListViewModel Lore { get; } = new(main, "Lore", CharacterRoster.StragoId, "Strago",
        Lores.FirstId, Lores.LastId, Lores.ContentIdOffset, Lores.All.Select(l => (l.Id, l.Name)));
    public SkillListViewModel Blitz { get; } = new(main, "Blitz", CharacterRoster.SabinId, "Sabin",
        Blitzes.FirstId, Blitzes.LastId, Blitzes.ContentIdOffset, Blitzes.All.Select(b => (b.Id, b.Name)));
    public SkillListViewModel Dance { get; } = new(main, "Dance", CharacterRoster.MogId, "Mog",
        Dances.FirstId, Dances.LastId, Dances.ContentIdOffset, Dances.All.Select(d => (d.Id, d.Name)));
}
