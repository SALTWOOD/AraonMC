using System.Text.Json.Nodes;

namespace AraonMC.Versions.Install;

/// <summary>assets 索引（<c>assets/indexes/&lt;id&gt;.json</c>）。</summary>
public sealed class AssetIndex
{
    public IReadOnlyDictionary<string, AssetObject> Objects { get; set; } = new Dictionary<string, AssetObject>();

    public sealed class AssetObject
    {
        public string Hash { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    public static AssetIndex Parse(string json)
    {
        var root = JsonNode.Parse(json)?.AsObject()
                   ?? throw new InvalidDataException("asset index is empty");
        var objs = root["objects"]?.AsObject();
        var dict = new Dictionary<string, AssetObject>(objs?.Count ?? 0);
        if (objs is not null)
        {
            foreach (var kv in objs)
            {
                if (kv.Value is not JsonObject o) continue;
                dict[kv.Key] = new AssetObject
                {
                    Hash = (string?)o["hash"] ?? string.Empty,
                    Size = (long?)o["size"] ?? 0,
                };
            }
        }
        return new AssetIndex { Objects = dict };
    }
}
