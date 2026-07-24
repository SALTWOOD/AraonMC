// Copyright (C) 2026 SALTWOOD and contributors
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Text.Json;
using System.Text.Json.Nodes;
using AraonMC.LaunchArgs.Rules;

namespace AraonMC.LaunchArgs.Version;

/// <summary>把 client.json 解析成 <see cref="VersionMetadata"/>。</summary>
public static class VersionMetadataReader
{
    public static VersionMetadata Read(string json)
        => Read(JsonNode.Parse(json) ?? throw new ArgumentException("version JSON is empty.", nameof(json)));

    public static VersionMetadata Read(JsonNode node)
    {
        var o = node.AsObject();

        var meta = new VersionMetadata
        {
            Id = (string?)o["id"] ?? string.Empty,
            MainClass = (string?)o["mainClass"] ?? string.Empty,
            Type = (string?)o["type"] ?? "release",
            MinimumLauncherVersion = (int?)o["minimumLauncherVersion"] ?? 0,
            InheritsFrom = (string?)o["inheritsFrom"],
            Assets = (string?)o["assets"],
        };

        if (o["assetIndex"] is JsonObject ai)
        {
            meta.AssetIndex = ParseAssetIndex(ai);
            meta.AssetsIndexName = meta.AssetIndex.Id;
        }
        if (string.IsNullOrEmpty(meta.AssetsIndexName) && !string.IsNullOrEmpty(meta.Assets))
            meta.AssetsIndexName = meta.Assets;

        if (o["javaVersion"] is JsonObject jv)
        {
            meta.JavaVersion = new JavaVersion
            {
                Component = (string?)jv["component"] ?? string.Empty,
                MajorVersion = (int?)jv["majorVersion"] ?? 0,
            };
        }

        if (o["arguments"] is JsonObject args)
        {
            meta.GameArguments = ParseArgumentList(args["game"]?.AsArray());
            meta.JvmArguments = ParseArgumentList(args["jvm"]?.AsArray());
        }

        meta.LegacyMinecraftArguments = (string?)o["minecraftArguments"];

        if (o["libraries"] is JsonArray libs)
            meta.Libraries = libs.Where(l => l is not null).Select(ParseLibrary!).ToList();

        if (o["logging"]?["client"] is JsonObject lc)
            meta.Logging = ParseLogging(lc);

        if (o["downloads"]?["client"] is JsonObject clientObj)
            meta.Downloads = new VersionDownloads { Client = ParseArtifact(clientObj) };

        return meta;
    }

    private static VersionAssetIndex ParseAssetIndex(JsonObject o) => new()
    {
        Id = (string?)o["id"] ?? string.Empty,
        Sha1 = (string?)o["sha1"] ?? string.Empty,
        Size = (long?)o["size"] ?? 0,
        TotalSize = (long?)o["totalSize"] ?? 0,
        Url = (string?)o["url"] ?? string.Empty,
    };

    private static VersionLogging ParseLogging(JsonObject lc)
    {
        var logging = new VersionLogging();
        if (lc["file"] is JsonObject lf)
        {
            logging.Client = new VersionLoggingFile
            {
                Id = (string?)lf["id"] ?? string.Empty,
                Sha1 = (string?)lf["sha1"] ?? string.Empty,
                Size = (long?)lf["size"] ?? 0,
                Url = (string?)lf["url"] ?? string.Empty,
                Path = (string?)lf["path"] ?? string.Empty,
                Argument = (string?)lc["argument"],
            };
        }
        return logging;
    }

    private static IReadOnlyList<VersionArgumentEntry> ParseArgumentList(JsonArray? arr)
    {
        if (arr is null) return [];
        var list = new List<VersionArgumentEntry>(arr.Count);
        foreach (var item in arr)
        {
            if (item is null) continue;
            if (item.GetValueKind() == JsonValueKind.String)
            {
                list.Add(new VersionArgumentEntry { Values = [item.GetValue<string>()] });
            }
            else if (item is JsonObject o)
            {
                var entry = new VersionArgumentEntry { Values = ParseValue(o["value"]) };
                if (o["rules"] is JsonArray rules)
                    entry.Rules = rules.Where(r => r is not null).Select(ParseRule!).ToList();
                list.Add(entry);
            }
        }
        return list;
    }

    private static IReadOnlyList<string> ParseValue(JsonNode? v) => v switch
    {
        null => [],
        _ when v.GetValueKind() == JsonValueKind.String => [v.GetValue<string>()],
        JsonArray arr => arr.Select(x => x is null ? string.Empty : x.GetValue<string>()).ToList(),
        _ => [v.ToJsonString()],
    };

    private static Rule ParseRule(JsonNode r)
    {
        var o = r.AsObject();
        var rule = new Rule
        {
            Action = (string?)o["action"] == "disallow" ? RuleAction.Disallow : RuleAction.Allow,
        };

        if (o["os"] is JsonObject os)
        {
            var osc = new OsCondition
            {
                Version = (string?)os["version"],
                Arch = (string?)os["arch"],
            };
            osc.Name = (string?)os["name"] switch
            {
                "windows" => OperatingSystemKind.Windows,
                "linux" => OperatingSystemKind.Linux,
                "osx" => OperatingSystemKind.OSX,
                _ => null,
            };
            rule.Os = osc;
        }

        if (o["features"] is JsonObject feats)
        {
            var dict = new Dictionary<string, bool>();
            foreach (var kv in feats)
                if (kv.Value is JsonValue fv)
                    dict[kv.Key] = fv.GetValue<bool>();
            rule.Features = dict;
        }

        return rule;
    }

    private static VersionLibrary ParseLibrary(JsonNode l)
    {
        var o = l.AsObject();
        var lib = new VersionLibrary { Name = (string?)o["name"] ?? string.Empty };

        if (o["rules"] is JsonArray rules)
            lib.Rules = rules.Where(r => r is not null).Select(ParseRule!).ToList();

        if (o["downloads"] is JsonObject dl)
        {
            if (dl["artifact"] is JsonObject art)
                lib.Artifact = ParseArtifact(art);
            if (dl["classifiers"] is JsonObject cls)
            {
                var dict = new Dictionary<string, VersionArtifact>();
                foreach (var kv in cls)
                    if (kv.Value is JsonObject ca)
                        dict[kv.Key] = ParseArtifact(ca);
                lib.Classifiers = dict;
            }
        }

        if (o["natives"] is JsonObject nat)
        {
            var dict = new Dictionary<OperatingSystemKind, string>();
            foreach (var kv in nat)
            {
                var kind = kv.Key switch
                {
                    "windows" => (OperatingSystemKind?)OperatingSystemKind.Windows,
                    "linux" => OperatingSystemKind.Linux,
                    "osx" => OperatingSystemKind.OSX,
                    _ => null,
                };
                if (kind is { } k && kv.Value is JsonValue cv)
                    dict[k] = cv.GetValue<string>();
            }
            lib.Natives = dict;
        }

        if (o["extract"] is JsonObject ex && ex["exclude"] is JsonArray exc)
            lib.Extract = new VersionLibraryExtract
            {
                Exclude = exc.Where(x => x is not null).Select(x => x!.GetValue<string>()).ToList(),
            };

        return lib;
    }

    private static VersionArtifact ParseArtifact(JsonObject o) => new()
    {
        Path = (string?)o["path"] ?? string.Empty,
        Sha1 = (string?)o["sha1"] ?? string.Empty,
        Size = (long?)o["size"] ?? 0,
        Url = (string?)o["url"] ?? string.Empty,
    };
}
