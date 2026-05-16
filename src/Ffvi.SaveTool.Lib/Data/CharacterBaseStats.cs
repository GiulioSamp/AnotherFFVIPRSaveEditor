namespace Ffvi.SaveTool.Data;

// Raw base stats per playable character (Pixel Remaster). Source: FF wiki "Final Fantasy VI stats".
// Stats other than HP/MP DO NOT grow with level — only equipment and magicite boost them.
// HP/MP grow per level (see LevelGrowth).
public record RawStats(
    int Hp,
    int Mp,
    int Strength,
    int Speed,
    int Stamina,
    int Magic,
    int Attack,
    int Defense,
    int Evasion,
    int MagicDefense,
    int MagicEvasion,
    int EscapeSuccess);

public static class CharacterBaseStats
{
    public static readonly IReadOnlyDictionary<string, RawStats> ByName = new Dictionary<string, RawStats>
    {
        ["Terra"]  = new(40, 16, 31, 33, 28, 39, 12, 42,  5, 33,  7, 4),
        ["Locke"]  = new(48,  7, 37, 40, 31, 28, 14, 46, 15, 23,  2, 5),
        ["Cyan"]   = new(53,  5, 40, 28, 33, 25, 25, 48,  6, 20,  1, 3),
        ["Shadow"] = new(51,  6, 39, 38, 30, 33, 23, 47, 28, 25,  9, 5),
        ["Edgar"]  = new(49,  6, 39, 30, 34, 29, 20, 50,  4, 22,  1, 4),
        ["Sabin"]  = new(58,  3, 47, 37, 39, 28, 26, 53, 12, 21,  4, 4),
        ["Celes"]  = new(44, 15, 34, 34, 31, 36, 16, 44,  7, 31,  9, 4),
        ["Strago"] = new(35, 13, 28, 25, 19, 34, 10, 33,  6, 27,  7, 3),
        ["Relm"]   = new(37, 18, 26, 34, 22, 44, 11, 35, 13, 30,  9, 5),
        ["Setzer"] = new(46,  9, 36, 32, 32, 29, 18, 48,  9, 26,  1, 4),
        ["Mog"]    = new(39, 16, 29, 36, 26, 35, 16, 52, 10, 36, 12, 5),
        ["Gau"]    = new(45, 10, 44, 38, 36, 34, 99, 44, 21, 34, 18, 5),
        ["Gogo"]   = new(36, 12, 25, 30, 20, 26, 13, 39, 10, 25,  6, 4),
        ["Umaro"]  = new(60,  0, 57, 33, 46, 37, 47, 89,  8, 68,  5, 3),
        ["Banon"]  = new(46, 16, 10, 24, 11, 32,  6, 56, 36, 51, 32, 2),
        ["Leo"]    = new(50, 10, 52, 38, 41, 36, 60, 63, 22, 41, 21, 3),
    };

    public static RawStats? For(string characterName) =>
        ByName.TryGetValue(characterName, out var s) ? s : null;
}
