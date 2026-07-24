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

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Config;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;

namespace AraonMC.Instances;

/// <summary>
/// JSON 持久化的 <see cref="IInstanceRepository"/>：<c>instances.json</c> 于 config 根，
/// 原子写（temp + rename）。共享式 .minecraft：实例名绑定到 <c>versions/&lt;name&gt;/</c> 目录；
/// <c>libraries/</c>、<c>assets/</c> 跨版本共享；不创建隔离目录。
/// </summary>
public sealed class JsonInstanceRepository : IInstanceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _file;
    private readonly INotificationService _notifications;
    private readonly string _rootDir;
    private List<GameInstance> _instances;

    public JsonInstanceRepository(INotificationService notifications)
    {
        _notifications = notifications;
        _file = Path.Combine(ConfigPaths.GlobalRoot(), "instances.json");
        _rootDir = ConfigPaths.GameDirectory();
        DebugLog.Info($"Instances: repository constructed — file='{_file}', shared gameDir='{_rootDir}'.");
        _instances = Load();
    }

    public IReadOnlyList<GameInstance> GetAll() => _instances;

    public Task<GameInstance> CreateAsync(string name, MinecraftVersion version, LoaderType loader, CancellationToken ct = default)
    {
        var instanceName = ValidateInstanceName(name);
        EnsureNameAvailable(instanceName, except: null);

        var instance = new GameInstance
        {
            Id = MakeId(instanceName),
            // MinecraftVersion is intentionally the versions/<name>/ folder, not necessarily Mojang's id.
            MinecraftVersion = instanceName,
            BaseMinecraftVersion = version.Id,
            Loader = loader,
            Path = _rootDir,
        };
        _instances.Add(instance);
        Save();
        DebugLog.Info($"Instances: created instance '{instance.Name}' (id={instance.Id}, version='{instance.MinecraftVersion}', base='{instance.BaseMinecraftVersion}', loader={instance.Loader}); {_instances.Count} total.");
        return Task.FromResult(instance);
    }

    public Task SaveAsync(GameInstance instance, CancellationToken ct = default)
    {
        DebugLog.Info($"Instances: explicit save requested for instance '{instance.Name}'.");
        Save();
        return Task.CompletedTask;
    }

    public Task RenameAsync(GameInstance instance, string newName, CancellationToken ct = default)
    {
        var targetName = ValidateInstanceName(newName);
        if (string.Equals(instance.MinecraftVersion, targetName, StringComparison.Ordinal))
        {
            DebugLog.Info($"Instances: rename no-op — '{instance.MinecraftVersion}' already matches the requested name.");
            return Task.CompletedTask;
        }
        EnsureNameAvailable(targetName, except: instance);
        DebugLog.Info($"Instances: renaming '{instance.MinecraftVersion}' → '{targetName}' (instance '{instance.Name}').");

        var oldName = instance.MinecraftVersion;
        var versionsDir = Path.Combine(instance.Path, "versions");
        var oldDir = Path.Combine(versionsDir, oldName);
        var newDir = Path.Combine(versionsDir, targetName);

        if (Directory.Exists(newDir))
            throw new IOException($"Version folder already exists: {newDir}");

        if (Directory.Exists(oldDir))
        {
            // Rename files inside first, while paths are simple and still under the old directory.
            RenameIfExists(Path.Combine(oldDir, oldName + ".jar"), Path.Combine(oldDir, targetName + ".jar"));
            RenameIfExists(Path.Combine(oldDir, oldName + ".json"), Path.Combine(oldDir, targetName + ".json"));
            Directory.Move(oldDir, newDir);
            DebugLog.Info($"Instances: moved version folder '{oldDir}' → '{newDir}'.");
        }
        else
        {
            DebugLog.Warn($"Instances: version folder missing during rename: {oldDir}; updating metadata only.");
        }

        instance.MinecraftVersion = targetName;
        Save();
        DebugLog.Info($"Instances: rename complete; instance '{instance.Name}' now maps to versions/'{targetName}'.");
        return Task.CompletedTask;
    }

    public Task DeleteAsync(GameInstance instance, CancellationToken ct = default)
    {
        DebugLog.Info($"Instances: deleting instance '{instance.Name}' (id={instance.Id}, version='{instance.MinecraftVersion}').");
        _instances.RemoveAll(x => x.Id == instance.Id);

        // 实例名绑定到版本目录名；移除实例即移除 versions/<name>/。
        var versionDir = Path.Combine(instance.Path, "versions", instance.MinecraftVersion);
        if (Directory.Exists(versionDir))
        {
            Directory.Delete(versionDir, recursive: true);
            DebugLog.Info($"Instances: deleted version directory '{versionDir}' (recursive).");
        }
        else
        {
            DebugLog.Info($"Instances: version directory '{versionDir}' already absent; removed metadata only.");
        }

        Save();
        DebugLog.Info($"Instances: delete complete; {_instances.Count} instance(s) remain.");
        return Task.CompletedTask;
    }

    private List<GameInstance> Load()
    {
        try
        {
            if (!File.Exists(_file))
            {
                DebugLog.Info("Instances: instances.json not found; starting with no instances.");
                return new List<GameInstance>();
            }
            var json = File.ReadAllText(_file);
            DebugLog.Info($"Instances: read instances.json ({json.Length} char(s)); deserializing...");
            var loaded = JsonSerializer.Deserialize<List<GameInstance>>(json, JsonOptions) ?? new List<GameInstance>();
            var migrated = 0;
            foreach (var i in loaded)
            {
                // Migration for older instances: before this change MinecraftVersion was Mojang's id and no
                // BaseMinecraftVersion existed, so treat the old value as both folder/name and base version.
                if (string.IsNullOrWhiteSpace(i.BaseMinecraftVersion)) { i.BaseMinecraftVersion = i.MinecraftVersion; migrated++; }
            }
            DebugLog.Info($"Instances: loaded {loaded.Count} instance(s)"
                + (migrated > 0 ? $" ({migrated} migrated to the BaseMinecraftVersion schema)." : "."));
            return loaded;
        }
        catch (Exception ex)
        {
            DebugLog.Warn($"Instances: instances.json unreadable ({ex.GetType().Name}: {ex.Message}); starting with no instances.");
            _ = _notifications.ShowAsync(NotificationRequest.Toast(
                "Instances file unreadable",
                $"Could not read instances.json; starting with no instances: {ex.Message}",
                NotificationLevel.Warning));
            return new List<GameInstance>();
        }
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(_file);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_instances, JsonOptions);
        var tmp = _file + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, _file, overwrite: true);
        DebugLog.Info($"Instances: persisted {_instances.Count} instance(s) to instances.json ({json.Length} char(s)) via temp+rename.");
    }

    private static string ValidateInstanceName(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("Instance name must not be empty.", nameof(name));
        if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Instance name contains characters that can't be used in a folder name.", nameof(name));
        if (trimmed is "." or "..")
            throw new ArgumentException("Instance name can't be '.' or '..'.", nameof(name));
        return trimmed;
    }

    private void EnsureNameAvailable(string name, GameInstance? except)
    {
        if (_instances.Any(i => !ReferenceEquals(i, except)
                               && string.Equals(i.MinecraftVersion, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"An instance named '{name}' already exists.");
    }

    private static void RenameIfExists(string oldPath, string newPath)
    {
        if (!File.Exists(oldPath)) return;
        if (File.Exists(newPath)) throw new IOException($"Target file already exists: {newPath}");
        File.Move(oldPath, newPath);
    }

    private static string MakeId(string name)
    {
        var slug = string.Concat(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-'));
        slug = string.Join('-', slug.Split('-', StringSplitOptions.RemoveEmptyEntries));
        return (string.IsNullOrEmpty(slug) ? "instance" : slug) + "-" + Guid.NewGuid().ToString("N")[..8];
    }
}
