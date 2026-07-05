using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AraonMC.Core.Config;
using Tomlyn;

namespace AraonMC.Config;

/// <summary>
/// TOML-backed <see cref="IConfigStore"/>. Two files at the OS config root:
/// <list type="bullet">
/// <item><c>config.toml</c> — global keys (tables per section).</item>
/// <item><c>instances.toml</c> — per-instance overrides, keyed by absolute instance path:
/// <c>[instances."C:/.../foo"]</c>.</item>
/// </list>
/// Reads are served from an in-memory model loaded once at construction; every write updates
/// memory and writes the affected file through atomically (temp-file + rename). Loading is
/// lenient: a missing/wrong-typed key falls back to its default; an unreadable file is backed
/// up and reset to defaults, with <paramref name="onWarning"/> surfacing the problem.
/// </summary>
public sealed class TomlConfigStore : IConfigStore
{
    private readonly string _globalFile;
    private readonly string _instancesFile;
    private readonly Action<string>? _onWarning;
    private readonly object _lock = new();

    private Dictionary<string, object?> _global = new();
    private Dictionary<string, object?> _instances = new();

    public TomlConfigStore(string globalFile, string instancesFile, Action<string>? onWarning = null)
    {
        _globalFile = globalFile;
        _instancesFile = instancesFile;
        _onWarning = onWarning;
        Load();
    }

    // ---- Loading ---------------------------------------------------------------

    private void Load()
    {
        _global = LoadFile(_globalFile);
        _instances = LoadFile(_instancesFile);
    }

    private Dictionary<string, object?> LoadFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return new Dictionary<string, object?>();
            var doc = Toml.Parse(File.ReadAllText(path));
            return ToPlain(doc.ToModel());
        }
        catch (Exception ex)
        {
            // Back up the corrupt file and start fresh from defaults.
            BackupCorrupt(path);
            _onWarning?.Invoke($"Config file {Path.GetFileName(path)} was corrupt and was reset to defaults: {ex.Message}");
            return new Dictionary<string, object?>();
        }
    }

    private static void BackupCorrupt(string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            for (var i = 1; ; i++)
            {
                var backup = Path.Combine(dir ?? "", $"{name}.corrupt-{i}{ext}");
                if (!File.Exists(backup))
                {
                    File.Move(path, backup, overwrite: false);
                    return;
                }
            }
        }
        catch { /* best-effort backup; never fail the app on it */ }
    }

    // ---- IConfigStore ----------------------------------------------------------

    public T Get<T>(ConfigScope scope, string path, T defaultValue, string? instancePath = null)
    {
        lock (_lock)
        {
            object? raw = scope == ConfigScope.Instance
                ? GetInstance(path, instancePath)
                : GetByPath(_global, path);
            return ConvertValue(raw, defaultValue);
        }
    }

    public void Set<T>(ConfigScope scope, string path, T value, string? instancePath = null)
    {
        lock (_lock)
        {
            var normalized = Normalize(value);
            if (scope == ConfigScope.Instance)
            {
                if (instancePath is null) return;
                SetInstance(path, instancePath, normalized);
                Save(_instancesFile, _instances);
            }
            else
            {
                SetByPath(_global, path, normalized);
                Save(_globalFile, _global);
            }
        }
    }

    // ---- Path navigation -------------------------------------------------------

    private static object? GetByPath(Dictionary<string, object?> root, string dottedPath)
    {
        var cur = root;
        foreach (var seg in dottedPath.Split('.'))
        {
            if (cur.TryGetValue(seg, out var v) && v is Dictionary<string, object?> d)
                cur = d;
            else if (cur.TryGetValue(seg, out var scalar))
                return scalar;
            else
                return null;
        }
        return null;
    }

    private static void SetByPath(Dictionary<string, object?> root, string dottedPath, object? value)
    {
        var segs = dottedPath.Split('.');
        var cur = root;
        for (var i = 0; i < segs.Length - 1; i++)
        {
            if (!cur.TryGetValue(segs[i], out var next) || next is not Dictionary<string, object?> nd)
            {
                nd = new Dictionary<string, object?>();
                cur[segs[i]] = nd;
                next = nd;
            }
            cur = (Dictionary<string, object?>)next!;
        }
        var lastKey = segs[^1];
        if (value is null) cur.Remove(lastKey);
        else cur[lastKey] = value;
    }

    private object? GetInstance(string path, string? instancePath)
    {
        if (instancePath is null) return null;
        if (!_instances.TryGetValue("instances", out var instObj) || instObj is not Dictionary<string, object?> inst)
            return null;
        if (!inst.TryGetValue(instancePath, out var tbl) || tbl is not Dictionary<string, object?> keys)
            return null;
        // path is "<sectionPath>.<key...>"; drop the leading section segment.
        var keyPath = string.Join('.', path.Split('.').Skip(1));
        return GetByPath(keys, keyPath);
    }

    private void SetInstance(string path, string instancePath, object? value)
    {
        if (!_instances.TryGetValue("instances", out var instObj) || instObj is not Dictionary<string, object?> inst)
        {
            inst = new Dictionary<string, object?>();
            _instances["instances"] = inst;
        }
        if (!inst.TryGetValue(instancePath, out var tbl) || tbl is not Dictionary<string, object?> keys)
        {
            keys = new Dictionary<string, object?>();
            inst[instancePath] = keys;
        }
        var keyPath = string.Join('.', path.Split('.').Skip(1));
        SetByPath(keys, keyPath, value);
    }

    // ---- (De)serialization helpers ---------------------------------------------

    private static void Save(string path, Dictionary<string, object?> model)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var text = Toml.FromModel(model, new TomlModelOptions());
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, text);
        File.Move(tmp, path, overwrite: true);
    }

    private static Dictionary<string, object?> ToPlain(object? src)
    {
        var dst = new Dictionary<string, object?>();
        if (src is IDictionary<string, object?> d)
        {
            foreach (var kv in d)
                dst[kv.Key] = kv.Value is IDictionary<string, object?> nested ? ToPlain(nested) : kv.Value;
        }
        return dst;
    }

    // ---- Value conversion ------------------------------------------------------

    private static object? Normalize<T>(T value)
    {
        if (value is null) return null;
        var t = typeof(T);
        var underlying = Nullable.GetUnderlyingType(t);
        if ((underlying ?? t).IsEnum)
            return Enum.GetName(underlying ?? t, value!) ?? value!.ToString(); // store as name
        return value;
    }

    private static T ConvertValue<T>(object? raw, T defaultValue)
    {
        if (raw is null) return defaultValue;
        var type = typeof(T);
        var underlying = Nullable.GetUnderlyingType(type);
        var target = underlying ?? type;
        try
        {
            if (target.IsEnum)
            {
                if (raw is string s && Enum.TryParse(target, s, ignoreCase: false, out var e)) return (T)e;
                if (raw.GetType().IsPrimitive) return (T)Enum.ToObject(target, Convert.ToInt64(raw, CultureInfo.InvariantCulture));
                return defaultValue;
            }
            if (target == typeof(string)) return (T)(object)Convert.ToString(raw, CultureInfo.InvariantCulture)!;
            if (target == typeof(bool)) return (T)(object)Convert.ToBoolean(raw, CultureInfo.InvariantCulture);
            if (target == typeof(int)) return (T)(object)Convert.ToInt32(raw, CultureInfo.InvariantCulture);
            if (target == typeof(long)) return (T)(object)Convert.ToInt64(raw, CultureInfo.InvariantCulture);
            if (target == typeof(double)) return (T)(object)Convert.ToDouble(raw, CultureInfo.InvariantCulture);
            if (target == typeof(float)) return (T)(object)Convert.ToSingle(raw, CultureInfo.InvariantCulture);
            return (T)Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
        }
        catch
        {
            return defaultValue;
        }
    }
}
