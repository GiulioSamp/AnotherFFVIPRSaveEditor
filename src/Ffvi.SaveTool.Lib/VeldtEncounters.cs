using System.Text.Json.Nodes;

namespace Ffvi.SaveTool;

// Wraps top.mapData.beastFieldEncountExchangeFlags — a flat array of ints (0 or 1),
// one per monster formation in the game. A "1" means that formation is unlocked on the Veldt.
// Note: lives in top-level mapData, not userData like most other game state.
public class VeldtEncounters
{
    private readonly JsonObject _mapData;
    public List<bool> Encounters { get; }

    internal VeldtEncounters(JsonObject mapData)
    {
        _mapData = mapData;
        var arr = mapData["beastFieldEncountExchangeFlags"]?.AsArray();
        Encounters = arr is null
            ? new List<bool>()
            : arr.Select(v => (v?.GetValue<int>() ?? 0) == 1).ToList();
    }

    public int TotalCount => Encounters.Count;
    public int SeenCount => Encounters.Count(b => b);

    public void MarkAllSeen()
    {
        for (var i = 0; i < Encounters.Count; i++) Encounters[i] = true;
    }

    public void ClearAll()
    {
        for (var i = 0; i < Encounters.Count; i++) Encounters[i] = false;
    }

    internal void Commit()
    {
        var arr = new JsonArray();
        foreach (var b in Encounters) arr.Add(b ? 1 : 0);
        _mapData["beastFieldEncountExchangeFlags"] = arr;
    }
}
