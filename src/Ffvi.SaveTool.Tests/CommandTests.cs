using Xunit;
using Ffvi.SaveTool;

namespace Ffvi.SaveTool.Tests;

public class CommandTests
{
    private static readonly string Fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.sav");
    private const int TerraJobId = 1;
    private const int LockeJobId = 2;
    private const int TranceId = 8;

    private static Character? Find(SaveFile save, int jobId) =>
        save.UserData.Characters.FirstOrDefault(c => c.JobId == jobId);

    [Fact]
    public void Terra_MayEquipTrance_OthersMayNot()
    {
        var save = SaveFile.Load(Fixture);
        var terra = Find(save, TerraJobId)!;
        Assert.Contains(terra.Commands.AllowedCommands(terra.JobId), c => c.Id == TranceId);

        var locke = Find(save, LockeJobId);
        if (locke is null) return;
        Assert.DoesNotContain(locke.Commands.AllowedCommands(locke.JobId), c => c.Id == TranceId);
    }

    [Fact]
    public void TranceSlot_RoundTrips()
    {
        var save = SaveFile.Load(Fixture);
        var terra = Find(save, TerraJobId)!;
        terra.Commands.Slots[4] = TranceId;

        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".sav");
        try
        {
            save.Save(tmp);
            var reloaded = Find(SaveFile.Load(tmp), TerraJobId)!;
            Assert.Equal(TranceId, reloaded.Commands.Slots[4]);
        }
        finally { File.Delete(tmp); }
    }
}
