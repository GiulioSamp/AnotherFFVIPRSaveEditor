using Xunit;
using Ffvi.SaveTool;
using Ffvi.SaveTool.Data;

namespace Ffvi.SaveTool.Tests;

public class SkillOwnerTests
{
    private static readonly string Fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.sav");

    [Theory]
    [InlineData(CharacterRoster.GauId, "Gau")]
    [InlineData(CharacterRoster.CyanId, "Cyan")]
    [InlineData(CharacterRoster.StragoId, "Strago")]
    [InlineData(CharacterRoster.SabinId, "Sabin")]
    [InlineData(CharacterRoster.MogId, "Mog")]
    public void SkillOwner_IsTheRightCharacter(int ownerRosterId, string expected)
    {
        var characters = SaveFile.Load(Fixture).UserData.Characters;
        var owner = CharacterRoster.FindOwner(characters, ownerRosterId);
        Console.WriteLine($"{expected}: {(owner is null ? "absent" : "present")}");
        if (owner is null) return;

        var resolved = CharacterRoster.Resolve(owner.Id, owner.JobId);
        Assert.Equal(expected, resolved!.EnglishName);
        Assert.NotEqual("Wedge", resolved.EnglishName);
        Assert.NotEqual("Biggs", resolved.EnglishName);
        Assert.All(CharacterRoster.OwnerCandidates(characters, ownerRosterId), c => Assert.Equal(owner.JobId, c.JobId));
    }
}
