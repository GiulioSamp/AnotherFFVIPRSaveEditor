using System.Text.Json.Nodes;

namespace Ffvi.SaveTool;

internal static class NestedJson
{
    public static JsonNode Unwrap(JsonObject parent, string key)
    {
        var node = parent[key] ?? throw new InvalidOperationException($"Missing key '{key}'");
        return node is JsonValue jv && jv.TryGetValue<string>(out var s)
            ? JsonNode.Parse(s)!
            : node;
    }

    public static JsonNode UnwrapString(string s) => JsonNode.Parse(s)!;

    public static void Rewrap(JsonObject parent, string key, JsonNode value) =>
        parent[key] = JsonValue.Create(value.ToJsonString(SaveFile.JsonOpts));
}
