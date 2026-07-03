using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using AraonMC.Auth;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
// Alias the generated facade under a non-clashing name: the app's `Config/` folder exposes a
// child namespace AraonMC.Config, which would otherwise win over the bare name `Config`.
using CoreConfig = AraonMC.Core.Config.Config;

namespace AraonMC.Accounts;

/// <summary>
/// Presentation-layer <see cref="IAccountService"/>: wraps <c>AraonMC.Auth</c> device-code login
/// and offline profile generation, persisting via <see cref="IAccountStore"/>. Secrets stay here;
/// the UI binds only the projection in <see cref="Accounts"/>.
/// </summary>
public sealed class AccountService : IAccountService
{
    private readonly MinecraftAuthenticator _authenticator;
    private readonly AvaloniaDeviceCodeUI _deviceCodeUi;
    private readonly IAccountStore _store;
    private readonly List<StoredAccount> _stored;
    private readonly ObservableCollection<MinecraftAccount> _accounts = new();

    public AccountService(
        MinecraftAuthenticator authenticator,
        AvaloniaDeviceCodeUI deviceCodeUi,
        IAccountStore store)
    {
        _authenticator = authenticator;
        _deviceCodeUi = deviceCodeUi;
        _store = store;
        _stored = new List<StoredAccount>(store.Load());
        RebuildAccounts();
        DebugLog.Info($"Accounts: loaded {_stored.Count} account(s) from store.");
    }

    public ObservableCollection<MinecraftAccount> Accounts => _accounts;

    public MinecraftAccount? GetActive()
    {
        var activeId = CoreConfig.Account.ActiveAccountId;
        return _accounts.FirstOrDefault(a => a.Uuid == activeId) ?? _accounts.FirstOrDefault();
    }

    public async Task<MinecraftAccount> LoginMicrosoftAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Secrets.MsOAuthClientId))
            throw new InvalidOperationException(
                "Microsoft client id is not configured. Set the MS_CLIENT_ID environment variable and rebuild.");

        DebugLog.Info("Accounts: starting Microsoft device-code login.");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _deviceCodeUi.CancelSource = cts;

        var result = await _authenticator.LoginNewAsync(cts.Token);
        DebugLog.Info($"Accounts: Microsoft login succeeded for {result.Username} (uuid={result.Uuid}).");

        var stored = new StoredAccount
        {
            Uuid = result.Uuid,
            AccountType = AccountType.Microsoft,
            Username = result.Username,
            RefreshToken = result.RefreshToken,
            ProfileJson = result.ProfileJson
        };
        UpsertStored(stored);
        _store.Save(_stored);
        DebugLog.Info($"Accounts: persisted account store (total={_stored.Count}).");

        var account = Project(stored);
        _accounts.Add(account);
        await SetActiveAsync(account, ct);
        return account;
    }

    public Task<MinecraftAccount> AddOfflineAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        var trimmed = username.Trim();
        DebugLog.Info($"Accounts: adding offline account '{trimmed}'.");
        var stored = new StoredAccount
        {
            Uuid = OfflineUuid(trimmed),
            AccountType = AccountType.Offline,
            Username = trimmed
        };
        UpsertStored(stored);
        _store.Save(_stored);

        var account = Project(stored);
        _accounts.Add(account);
        EnsureActive();
        DebugLog.Info($"Accounts: offline account created (uuid={stored.Uuid}, total={_stored.Count}).");
        return Task.FromResult(account);
    }

    public Task SetActiveAsync(MinecraftAccount account, CancellationToken ct = default)
    {
        foreach (var a in _accounts) a.IsActive = a.Uuid == account.Uuid;
        CoreConfig.Account.ActiveAccountId = account.Uuid;
        DebugLog.Info($"Accounts: active account set to {account.Username} (uuid={account.Uuid}).");
        return Task.CompletedTask;
    }

    public Task RemoveAsync(MinecraftAccount account, CancellationToken ct = default)
    {
        DebugLog.Info($"Accounts: removing account {account.Username} (uuid={account.Uuid}).");
        _stored.RemoveAll(s => s.Uuid == account.Uuid);
        _store.Save(_stored);

        var match = _accounts.FirstOrDefault(a => a.Uuid == account.Uuid);
        if (match is not null) _accounts.Remove(match);

        // If we removed the active account, fall back to the first remaining (or clear).
        if (CoreConfig.Account.ActiveAccountId == account.Uuid)
        {
            var next = _accounts.FirstOrDefault();
            if (next is not null)
            {
                next.IsActive = true;
                CoreConfig.Account.ActiveAccountId = next.Uuid;
            }
            else
            {
                CoreConfig.Account.ActiveAccountId = "";
            }
        }
        DebugLog.Info($"Accounts: account removed (remaining={_accounts.Count}).");
        return Task.CompletedTask;
    }

    public async Task<string?> GetAccessTokenAsync(MinecraftAccount account, CancellationToken ct = default)
    {
        var stored = _stored.FirstOrDefault(s => s.Uuid == account.Uuid);
        if (stored is null)
        {
            DebugLog.Info($"Accounts: no stored record for {account.Username}; cannot get token.");
            return null;
        }
        if (stored.AccountType != AccountType.Microsoft || string.IsNullOrEmpty(stored.RefreshToken))
        {
            DebugLog.Info($"Accounts: {account.Username} has no refresh token (offline / third-party).");
            return null;
        }

        DebugLog.Info($"Accounts: refreshing access token for {account.Username} (uuid={account.Uuid}).");
        try
        {
            var result = await _authenticator.LoginWithRefreshTokenAsync(
                stored.RefreshToken, allowNewLogin: false, ct);
            // Refresh tokens can rotate; persist the latest.
            if (!string.Equals(stored.RefreshToken, result.RefreshToken, StringComparison.Ordinal))
            {
                stored.RefreshToken = result.RefreshToken;
                _store.Save(_stored);
                DebugLog.Info("Accounts: refresh token rotated; store updated.");
            }
            DebugLog.Info("Accounts: access token refreshed successfully.");
            return result.AccessToken;
        }
        catch (MinecraftAuthException ex) when (ex.Kind == AuthErrorKind.ReloginRequired)
        {
            DebugLog.Info($"Accounts: refresh failed for {account.Username} — re-login required.");
            return null;
        }
    }

    // ---- helpers ----

    private void RebuildAccounts()
    {
        var activeId = CoreConfig.Account.ActiveAccountId;
        if (string.IsNullOrEmpty(activeId) && _stored.Count > 0)
        {
            activeId = _stored[0].Uuid;
            CoreConfig.Account.ActiveAccountId = activeId;
        }

        _accounts.Clear();
        foreach (var s in _stored)
        {
            var a = Project(s);
            a.IsActive = a.Uuid == activeId;
            _accounts.Add(a);
        }
    }

    private void EnsureActive()
    {
        if (!string.IsNullOrEmpty(CoreConfig.Account.ActiveAccountId)) return;
        var first = _accounts.FirstOrDefault();
        if (first is null) return;
        first.IsActive = true;
        CoreConfig.Account.ActiveAccountId = first.Uuid;
    }

    private void UpsertStored(StoredAccount account)
    {
        var idx = _stored.FindIndex(s => s.Uuid == account.Uuid);
        if (idx >= 0) _stored[idx] = account;
        else _stored.Add(account);
    }

    private static MinecraftAccount Project(StoredAccount s) => new()
    {
        Uuid = s.Uuid,
        Username = s.Username,
        AccountType = s.AccountType,
        IsOnline = s.AccountType != AccountType.Offline,
        AvatarKey = AvatarKeyFor(s.Username),
        ServerUrl = s.ServerUrl ?? ""
    };

    private static string AvatarKeyFor(string username)
    {
        if (string.IsNullOrEmpty(username)) return "?";
        return username.Length >= 2
            ? username[..2].ToUpperInvariant()
            : username.ToUpperInvariant();
    }

    /// <summary>
    /// Deterministic offline UUID: MD5 of <c>"OfflinePlayer:" + username</c> with RFC 4122 v3
    /// version/variant bits set, matching the scheme used by Minecraft/Mojang and Java's
    /// <c>UUID.nameUUIDFromBytes</c>. Returned as no-dash lowercase to match
    /// <see cref="MinecraftAuthResult.Uuid"/>.
    /// </summary>
    private static string OfflineUuid(string username)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes("OfflinePlayer:" + username));
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x30); // version 3
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // IETF variant
        return Convert.ToHexStringLower(bytes);
    }
}
