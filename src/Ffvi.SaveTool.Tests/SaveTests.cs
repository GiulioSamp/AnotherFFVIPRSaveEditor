using Xunit;
using System.Text.Json.Nodes;
using Ffvi.SaveTool;

namespace Ffvi.SaveTool.Tests;

public class SaveTests
{
    private static readonly string Fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.sav");

    private static string TempPath() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".sav");

    [Fact]
    public void Crypto_RoundTrips()
    {
        const string json = """{"a":1,"b":"hello","c":[1,2,3]}""";
        Assert.Equal(json, SaveCrypto.Decrypt(SaveCrypto.Encrypt(json)));
    }

    [Fact]
    public void Save_RoundTripPreservesJson()
    {
        var original = SaveFile.Load(Fixture);
        var tmp = TempPath();
        try
        {
            original.Save(tmp);
            var reloaded = SaveFile.Load(tmp);
            Assert.True(JsonNode.DeepEquals(original.Top, reloaded.Top));
            Assert.True(JsonNode.DeepEquals(original.UserData.Node, reloaded.UserData.Node));
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void Edits_Persist()
    {
        var save = SaveFile.Load(Fixture);
        var ch = save.UserData.Characters[0];
        var id = ch.Id;
        save.UserData.Gil = 123456;
        ch.SetLevel(42);
        var tmp = TempPath();
        try
        {
            save.Save(tmp);
            var reloaded = SaveFile.Load(tmp);
            var ch2 = reloaded.UserData.Characters.First(c => c.Id == id);
            Assert.Equal(123456, reloaded.UserData.Gil);
            Assert.Equal(Data.LevelGrowth.ExpForLevel(42), ch2.CurrentExp);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void Roster_ResolvesByJob()
    {
        Assert.Equal("Mog", Data.CharacterRoster.Resolve(16, 11)?.EnglishName);
        Assert.Equal("Mog", Data.CharacterRoster.Resolve(99, 11)?.EnglishName);
        Assert.Equal("Biggs", Data.CharacterRoster.Resolve(3, 18)?.EnglishName);
        Assert.Equal("Terra", Data.CharacterRoster.Resolve(1, 1)?.EnglishName);
    }

    [Fact]
    public void LevelGrowth_IsConsistent()
    {
        Assert.Equal(0, Data.LevelGrowth.ExpForLevel(1));
        for (var l = 1; l <= 99; l++)
            Assert.Equal(l, Data.LevelGrowth.LevelForExp(Data.LevelGrowth.ExpForLevel(l)));
        Assert.Equal(2637112, Data.LevelGrowth.ExpForLevel(99));
    }
}
