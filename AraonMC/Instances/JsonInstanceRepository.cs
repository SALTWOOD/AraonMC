using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Config;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;
using CoreConfig = AraonMC.Core.Config.Config;

namespace AraonMC.Instances;

/// <summary>
/// JSON 持久化的 <see cref="IInstanceRepository"/>：<c>instances.json</c> 于 config 根，
/// 原子写（temp + rename）。每个实例一个独立隔离目录 <c>&lt;GameDirectory&gt;/&lt;id&gt;</c>。
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
        _rootDir = ResolveRoot();
        _instances = Load();
    }

    public IReadOnlyList<GameInstance> GetAll() => _instances;

    public Task<GameInstance> CreateAsync(string name, MinecraftVersion version, LoaderType loader, CancellationToken ct = default)
    {
        var id = MakeId(name);
        var path = Path.Combine(_rootDir, id);
        Directory.CreateDirectory(path);

        var instance = new GameInstance
        {
            Id = id,
            Name = name,
            MinecraftVersion = version.Id,
            Loader = loader,
            Path = path,
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
        if (!string.IsNullOrEmpty(instance.Path) && Directory.Exists(instance.Path))
            Directory.Delete(instance.Path, recursive: true);
        Save();
        return Task.CompletedTask;
    }

    private static string ResolveRoot()
    {
        var dir = CoreConfig.Game.GameDirectory;
        return !string.IsNullOrWhiteSpace(dir) ? dir : Path.Combine(ConfigPaths.GlobalRoot(), "instances");
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
