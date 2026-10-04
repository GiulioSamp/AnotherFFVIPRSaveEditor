using System.Text.Json.Nodes;
using Xunit;
using Ffvi.SaveTool;

namespace Ffvi.SaveTool.Tests;

public class StatsTests
{
    private static readonly string Fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.sav");

    [Fact]
    public void MaxHpMp_AreClassBasePlusStoredGrowth()
    {
        var terra = SaveFile.Load(Fixture).UserData.Characters.Single(c => c.Name == "Terra");
        var raw = terra.Stats.Node;
        var baseStats = Data.CharacterBaseStats.ByName["Terra"];

        Assert.Equal(baseStats.Hp + raw["addtionalMaxHp"]!.GetValue<int>(), terra.MaxHp);
        Assert.Equal(baseStats.Mp + raw["addtionalMaxMp"]!.GetValue<int>(), terra.MaxMp);
        Assert.Equal(248, terra.MaxHp);
        Assert.Equal(241, terra.MaxMp);
        Assert.True(terra.Stats.CurrentMp <= terra.MaxMp);
    }
}
