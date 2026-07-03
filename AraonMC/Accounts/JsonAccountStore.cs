using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Config;
using AraonMC.Core.Domain.Entities;

namespace AraonMC.Accounts;

/// <summary>
/// JSON-backed <see cref="IAccountStore"/>: <c>accounts.json</c> at the config root, written
/// atomically (temp + rename). Plaintext for now; a future ISecretProtector can wrap the token
/// fields without changing this format.
/// </summary>
public sealed class JsonAccountStore : IAccountStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _file;
    private readonly INotificationService _notifications;

    public JsonAccountStore(INotificationService notifications)
    {
        _file = Path.Combine(ConfigPaths.GlobalRoot(), "accounts.json");
        _notifications = notifications;
    }

    public IReadOnlyList<StoredAccount> Load()
    {
        try
        {
            if (!File.Exists(_file)) return new List<StoredAccount>();
            var json = File.ReadAllText(_file);
            return JsonSerializer.Deserialize<List<StoredAccount>>(json, JsonOptions) ?? new List<StoredAccount>();
        }
        catch (Exception ex)
        {
            // Best-effort warning; never throw out of Load — start empty rather than crash.
            _ = _notifications.ShowAsync(NotificationRequest.Toast(
                "Accounts file unreadable",
                $"Could not read accounts.json; starting with no accounts: {ex.Message}",
                NotificationLevel.Warning));
            return new List<StoredAccount>();
        }
    }

    public void Save(IReadOnlyList<StoredAccount> accounts)
    {
        var dir = Path.GetDirectoryName(_file);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(accounts, JsonOptions);
        var tmp = _file + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, _file, overwrite: true);
    }
}
