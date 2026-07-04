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
/// 原子写（temp + rename）。共享式 .minecraft：实例只是「版本 + 设置」的引用，
/// 游戏文件写入共享游戏根（<c>&lt;GameDirectory&gt;</c>）——<c>versions/&lt;id&gt;/</c> 按版本分文件夹，
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
        _instances = Load();
    }

    public IReadOnlyList<GameInstance> GetAll() => _instances;

    public Task<GameInstance> CreateAsync(string name, MinecraftVersion version, LoaderType loader, CancellationToken ct = default)
    {
        var id = MakeId(name);

        // 共享式：实例只是 (版本 + 设置) 的引用，不创建独立目录。
        // 游戏文件（versions/<id>/ 与共享的 libraries/、assets/）由安装器写入 _rootDir。
        var instance = new GameInstance
        {
            Id = id,
            Name = name,
            MinecraftVersion = version.Id,
            Loader = loader,
            Path = _rootDir,
        };
        _instances.Add(instance);
        Save();
        return Task.FromResult(instance);
    }

    public Task SaveAsync(GameInstance instance, CancellationToken ct = default)
    {
        Save();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(GameInstance instance, CancellationToken ct = default)
    {
        _instances.RemoveAll(x => x.Id == instance.Id);

        // 只删该版本的核心文件（versions/<version>/）；libraries/、assets/ 跨版本共享，不动。
        var versionDir = Path.Combine(instance.Path, "versions", instance.MinecraftVersion);
        if (Directory.Exists(versionDir))
            Directory.Delete(versionDir, recursive: true);

        Save();
        return Task.CompletedTask;
    }

    private List<GameInstance> Load()
    {
        try
        {
            if (!File.Exists(_file)) return new List<GameInstance>();
            var json = File.ReadAllText(_file);
            return JsonSerializer.Deserialize<List<GameInstance>>(json, JsonOptions) ?? new List<GameInstance>();
        }
        catch (Exception ex)
        {
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
    }

    private static string MakeId(string name)
    {
        var slug = string.Concat(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-'));
        slug = string.Join('-', slug.Split('-', StringSplitOptions.RemoveEmptyEntries));
        return (string.IsNullOrEmpty(slug) ? "instance" : slug) + "-" + Guid.NewGuid().ToString("N")[..8];
    }
}
