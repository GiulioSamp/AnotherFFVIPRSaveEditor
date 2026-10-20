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

    // v1.1.5: Encrypt stripped the trailing '=' padding and the game rejected the save.
    // Crypto_RoundTrips cannot catch it, because Decrypt re-pads before decoding.
    [Fact]
    public void Encrypt_KeepsBase64Padding()
    {
        // Lengths chosen so at least one ciphertext needs padding (length % 3 != 0).
        for (var n = 1; n <= 8; n++)
        {
            var encoded = System.Text.Encoding.ASCII.GetString(
                SaveCrypto.Encrypt("{\"pad\":\"" + new string('x', n) + "\"}"));
            Assert.Equal(0, encoded.Length % 4);
            Assert.DoesNotContain("=", encoded.TrimEnd('='));
        }
    }

    [Fact]
    public void Save_WritesBomAndCrlfFraming()
    {
        var tmp = TempPath();
        try
        {
            SaveFile.Load(Fixture).Save(tmp);
            var bytes = File.ReadAllBytes(tmp);
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
            Assert.Equal(new byte[] { 0x0D, 0x0A }, bytes[^2..]);
            Assert.Equal(0, (bytes.Length - 5) % 4);   // base64 body between BOM and CRLF
        }
        finally { File.Delete(tmp); }
    }

    // v1.1.8: clearing skillLevel in abilityList alone left the ability in
    // abilityDictionary and in the ownership order lists, so the game still showed it.
    [Fact]
    public void Forget_ClearsEveryOwnershipRecord()
    {
        var save = SaveFile.Load(Fixture);
        var ch = save.UserData.Characters.First(c => c.Abilities.LearnedMagic().Count > 0);
        var spell = ch.Abilities.LearnedMagic()[0].AbilityId;
        var id = ch.Id;

        ch.Abilities.ForgetSpell(spell);
        var tmp = TempPath();
        try
        {
            save.Save(tmp);
            var ch2 = SaveFile.Load(tmp).UserData.Characters.First(c => c.Id == id);
            Assert.False(ch2.Abilities.KnowsAbility(spell));
            Assert.DoesNotContain(spell, ch2.Abilities.LearnedMagic().Select(a => a.AbilityId));
            foreach (var key in new[] { "additionOrderOwnedAbilityIds", "sortOrderOwnedAbilityIds" })
            {
                var raw = ch2.Node[key] is JsonValue v && v.TryGetValue<string>(out var str)
                    ? str : "{\"target\":[]}";
                var owned = JsonNode.Parse(raw)!["target"]!.AsArray()
                    .Select(n => n!.GetValue<int>());
                Assert.DoesNotContain(spell + CharacterAbilities.ContentIdOffset, owned);
            }
        }
        finally { File.Delete(tmp); }
    }

    // v1.1.4: a total below the class base stored a negative bonus, which the game rejects.
    [Fact]
    public void SetTotalStat_NeverStoresNegativeBonus()
    {
        var ch = SaveFile.Load(Fixture).UserData.Characters.First(c => c.BaseStats is not null);
        foreach (var stat in Enum.GetValues<TotalStat>())
        {
            ch.SetTotalStat(stat, 0);
            Assert.Equal(0, ch.Stats.GetBonus(stat));
            Assert.Equal(ch.GetBaseStat(stat), ch.GetTotalStat(stat));
        }
    }

    // Duplicate contentId entries corrupt the save; the empty-slot placeholder stacks
    // (93/197/198/199/200) are game bookkeeping and must survive untouched.
    [Fact]
    public void Inventory_MergesDuplicatesAndKeepsPlaceholders()
    {
        var save = SaveFile.Load(Fixture);
        var inv = save.UserData.NormalInventory;
        var placeholdersBefore = inv.Stacks.Where(s => Equipment.IsEmptyPlaceholder(s.ItemId)).ToList();

        inv.Stacks.Add(new ItemStack(2, 30));
        inv.Stacks.Add(new ItemStack(2, 40));
        inv.MergeDuplicates();

        Assert.Single(inv.Stacks, s => s.ItemId == 2);
        Assert.True(inv.Stacks.Single(s => s.ItemId == 2).Count <= Inventory.MaxStackCount);
        Assert.Equal(placeholdersBefore,
            inv.Stacks.Where(s => Equipment.IsEmptyPlaceholder(s.ItemId)).ToList());
    }
}
