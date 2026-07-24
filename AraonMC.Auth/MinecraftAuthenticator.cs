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

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace AraonMC.Auth;

/// <summary>
///     Minecraft 正版（微软账户）登录器，把启动器主项目里手写的六步登录链路（设备码 / XBL / XSTS /
///     MC Token / 所有权 / 档案）封装为零依赖的纯后端实现。
///     <para>
///         对应原文件 <c>Plain Craft Launcher 2/Modules/Minecraft/ModLaunch.cs</c> 第 683–1374 行
///         （<c>McLoginMsStart</c> 与 <c>MsLoginStep1–6</c>），以及
///         <c>Pages/PageLaunch/MyMsgLogin.xaml.cs</c> 第 71–133 行的设备码轮询。
///     </para>
/// </summary>
public sealed class MinecraftAuthenticator
{
    private readonly MinecraftAuthOptions _options;
    private readonly HttpClient _http;
    private readonly IDeviceCodeUI _ui;

    // —— access token 内存缓存，对齐原版 mcLoginMsRefreshTime 的 10 分钟复用逻辑 ——
    private MinecraftAuthResult? _cached;
    private long _cacheTick;

    public MinecraftAuthenticator(MinecraftAuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.ClientId))
            throw new ArgumentException("options.ClientId must not be empty.", nameof(options));
        ArgumentNullException.ThrowIfNull(options.DeviceCodeUI);

        _options = options;
        _http = options.HttpClient ?? new HttpClient { Timeout = options.RequestTimeout };
        _ui = options.DeviceCodeUI;
    }

    #region 公共入口

    /// <summary>
    ///     添加档案：完整的设备码六步流程（Step 1–6）。
    /// </summary>
    public async Task<MinecraftAuthResult> LoginNewAsync(CancellationToken ct = default)
    {
        Log("Login (new): starting Microsoft device-code flow.");
        var info = await RequestDeviceCodeAsync(ct).ConfigureAwait(false);

        // UI 展示与后台轮询并行：轮询拿到 token（或出错）后取消 UI 令牌，让 DisplayAsync 退出。
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var uiTask = _ui.DisplayAsync(info, cts.Token);
        try
        {
            Log("Login (new): showing device code to user, polling for authorization...");
            var (accessToken, refreshToken) =
                await PollDeviceCodeAsync(info.DeviceCode, info.Interval, ct).ConfigureAwait(false);
            return await RunStepsAsync(accessToken, refreshToken, ct).ConfigureAwait(false);
        }
        finally
        {
            cts.Cancel();
            try { await uiTask.ConfigureAwait(false); } catch { /* UI 自行处理取消 */ }
        }
    }

    /// <summary>
    ///     启动游戏：用 refresh token 刷新 OAuth 后再走 Step 2–6。
    ///     refresh token 失效时，按 <paramref name="allowNewLogin" /> 决定是否回退设备码流程。
    ///     对齐原版 <c>GetOAuthTokens</c>（ModLaunch.cs:836-867）的回退逻辑。
    /// </summary>
    public async Task<MinecraftAuthResult> LoginWithRefreshTokenAsync(string refreshToken,
        bool allowNewLogin = true, CancellationToken ct = default)
    {
        Log($"Login (refresh): refreshing OAuth (refresh_token len={refreshToken.Length}, allowNewLogin={allowNewLogin}).");
        try
        {
            var (accessToken, newRefreshToken) =
                await RefreshOAuthAsync(refreshToken, ct).ConfigureAwait(false);
            return await RunStepsAsync(accessToken, newRefreshToken, ct).ConfigureAwait(false);
        }
        catch (MinecraftAuthException ex) when (ex.Kind == AuthErrorKind.ReloginRequired && allowNewLogin)
        {
            Log("Login (refresh): refresh token expired, falling back to device-code flow...");
            return await LoginNewAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     一步到位：命中缓存直接返回；否则有 refresh 走刷新、无 refresh 走设备码。
    /// </summary>
    public async Task<MinecraftAuthResult> AuthenticateAsync(string? refreshToken = null,
        bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh && TryGetCached(out var cached))
        {
            Log("Auth: access token cache hit, skipping network requests.");
            return cached;
        }

        if (!string.IsNullOrEmpty(refreshToken))
            return await LoginWithRefreshTokenAsync(refreshToken, allowNewLogin: true, ct)
                .ConfigureAwait(false);

        return await LoginNewAsync(ct).ConfigureAwait(false);
    }

    #endregion

    #region Step 2–6：OAuth 拿到 token 后的公共链路

    private async Task<MinecraftAuthResult> RunStepsAsync(string oauthAccessToken,
        string oauthRefreshToken, CancellationToken ct)
    {
        var xblToken = await GetXblTokenAsync(oauthAccessToken, ct).ConfigureAwait(false);
        var (xsts, uhs) = await GetXstsTokenAsync(xblToken, ct).ConfigureAwait(false);
        var mcToken = await GetMinecraftTokenAsync(xsts, uhs, ct).ConfigureAwait(false);
        await CheckEntitlementsAsync(mcToken, ct).ConfigureAwait(false);
        var (uuid, username, profileJson) = await GetProfileAsync(mcToken, ct).ConfigureAwait(false);

        var result = new MinecraftAuthResult
        {
            Uuid = uuid,
            Username = username,
            AccessToken = mcToken,
            RefreshToken = oauthRefreshToken,
            ProfileJson = profileJson
        };
        UpdateCache(result);
        Log($"Auth complete: uuid={uuid}, username={username}.");
        return result;
    }

    #endregion

    #region Step 1：设备码 / 刷新

    /// <summary>申请设备码（对应 <c>MsLoginStep1New</c> 前半段，ModLaunch.cs:882-907）。</summary>
    private async Task<DeviceCodeInfo> RequestDeviceCodeAsync(CancellationToken ct)
    {
        Log("Step 1/6: requesting device code...");
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["tenant"] = Endpoints.Tenant,
            ["scope"] = Endpoints.Scope
        };
        var body = await PostFormAsync(Endpoints.DeviceCode, form, ct).ConfigureAwait(false);
        var json = JsonNode.Parse(body) ?? throw NewUnknown("Failed to parse device-code response: " + body);
        var info = new DeviceCodeInfo
        {
            UserCode = (string)json["user_code"]!,
            DeviceCode = (string)json["device_code"]!,
            VerificationUrl = (string)json["verification_uri"]!,
            DirectVerificationUrl = (string?)json["verification_uri_complete"],
            ExpiresIn = (int?)json["expires_in"] ?? 0,
            Interval = (int?)json["interval"] ?? 5
        };
        Log($"Step 1/6: device code received (user_code={info.UserCode}, " +
            $"verification_uri={info.VerificationUrl}, expires_in={info.ExpiresIn}s, interval={info.Interval}s).");
        return info;
    }

    /// <summary>轮询设备码授权状态（对应 <c>MyMsgLogin.WorkThreadAsync</c>，MyMsgLogin.xaml.cs:71-133）。</summary>
    private async Task<(string accessToken, string refreshToken)> PollDeviceCodeAsync(
        string deviceCode, int interval, CancellationToken ct)
    {
        Log("Step 1/6: polling token endpoint for authorization...");
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
            ["client_id"] = _options.ClientId,
            ["device_code"] = deviceCode,
            ["scope"] = Endpoints.Scope
        };

        var failureCount = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            using var resp = await _http.PostAsync(Endpoints.OAuthToken,
                new FormUrlEncodedContent(form), ct).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                var err = JsonNode.Parse(body)?["error"]?.ToString();
                switch (err)
                {
                    case "authorization_pending":
                        Log($"Step 1/6: authorization pending, retrying in {Math.Max(1, interval - 1)}s...");
                        await Task.Delay(Math.Max(1, interval - 1) * 1000, ct).ConfigureAwait(false);
                        continue;
                    case "slow_down":
                        Log($"Step 1/6: slow_down requested, backing off for {interval + 5}s...");
                        await Task.Delay((interval + 5) * 1000, ct).ConfigureAwait(false);
                        continue;
                    case "expired_token":
                        throw new MinecraftAuthException(AuthErrorKind.DeviceCodeExpired,
                            "The device code has expired. Please start the login again.");
                    case "canceled":
                        throw new MinecraftAuthException(AuthErrorKind.DeviceCodePollingFailed,
                            "The authorization request was canceled.");
                    default:
                        // 未知错误重试 3 次后失败（对齐 MyMsgLogin.xaml.cs:119-130）
                        if (failureCount < 3)
                        {
                            failureCount++;
                            Log($"Step 1/6: polling attempt {failureCount} failed: {err ?? body}");
                            await Task.Delay(2000, ct).ConfigureAwait(false);
                            continue;
                        }
                        throw new MinecraftAuthException(AuthErrorKind.DeviceCodePollingFailed,
                            "Device-code polling failed: " + (err ?? body));
                }
            }

            var json = JsonNode.Parse(body) ?? throw NewUnknown("Failed to parse token response: " + body);
            var accessToken = (string)json["access_token"]!;
            var refreshToken = (string)json["refresh_token"]!;
            Log($"Step 1/6: OAuth tokens acquired (expires_in={json["expires_in"]}s, " +
                $"access_token len={accessToken.Length}, refresh_token len={refreshToken.Length}).");
            return (accessToken, refreshToken);
        }
    }

    /// <summary>使用 refresh token 刷新 OAuth token（对应 <c>MsLoginStep1Refresh</c>，ModLaunch.cs:938-998）。</summary>
    private async Task<(string accessToken, string refreshToken)> RefreshOAuthAsync(
        string refreshToken, CancellationToken ct)
    {
        Log("Step 1/6: refreshing OAuth token...");
        if (string.IsNullOrEmpty(refreshToken))
            throw new ArgumentException("Refresh token is empty.", nameof(refreshToken));

        var form = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
            ["scope"] = Endpoints.Scope
        };
        using var resp = await _http.PostAsync(Endpoints.RefreshToken,
            new FormUrlEncodedContent(form), ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            // 失效判定对齐 ModLaunch.cs:973-976（原版用忽略大小写的 ContainsF）
            var low = body.ToLowerInvariant();
            if (low.Contains("invalid_grant") || low.Contains("must sign in again") ||
                low.Contains("must first sign in") || low.Contains("password expired") ||
                (low.Contains("refresh_token") && low.Contains("is not valid")))
            {
                Log("Step 1/6: refresh token is no longer valid (relogin required).");
                throw new MinecraftAuthException(AuthErrorKind.ReloginRequired,
                    "The refresh token is no longer valid; re-login is required.");
            }
            throw NewUnknown($"Failed to refresh token: {(int)resp.StatusCode} {resp.StatusCode}\n{body}");
        }

        var json = JsonNode.Parse(body) ?? throw NewUnknown("Failed to parse refresh response: " + body);
        var access = (string)json["access_token"]!;
        var refresh = (string)json["refresh_token"]!;
        Log($"Step 1/6: OAuth token refreshed (access_token len={access.Length}, refresh_token len={refresh.Length}).");
        return (access, refresh);
    }

    #endregion

    #region Step 2：XBL Token

    /// <summary>OAuth access token → XBL Token（对应 <c>MsLoginStep2</c>，ModLaunch.cs:1020-1069）。</summary>
    private async Task<string> GetXblTokenAsync(string oauthAccessToken, CancellationToken ct)
    {
        Log("Step 2/6: exchanging OAuth token for Xbox Live (XBL) token...");
        var body = new JsonObject
        {
            ["Properties"] = new JsonObject
            {
                ["AuthMethod"] = "RPS",
                ["SiteName"] = "user.auth.xboxlive.com",
                ["RpsTicket"] = $"d={oauthAccessToken}"
            },
            ["RelyingParty"] = "http://auth.xboxlive.com",
            ["TokenType"] = "JWT"
        };
        var (status, respBody) = await PostJsonAsync(Endpoints.XboxLiveAuthenticate,
            body.ToJsonString(), ct).ConfigureAwait(false);
        if (!IsSuccess(status))
            throw NewUnknown($"Step 2 failed to obtain XBL token: {(int)status} {status}\n{respBody}");
        var json = JsonNode.Parse(respBody) ?? throw NewUnknown("Failed to parse XBL response.");
        var xbl = json["Token"]?.ToString() ?? throw NewUnknown("XBL response is missing the Token field.");
        Log($"Step 2/6: XBL token acquired (len={xbl.Length}).");
        return xbl;
    }

    #endregion

    #region Step 3：XSTS Token + UHS

    /// <summary>XBL Token → {XSTS Token, UHS}（对应 <c>MsLoginStep3</c>，ModLaunch.cs:1089-1183）。</summary>
    private async Task<(string xsts, string uhs)> GetXstsTokenAsync(string xblToken, CancellationToken ct)
    {
        Log("Step 3/6: exchanging XBL token for XSTS token...");
        var body = new JsonObject
        {
            ["Properties"] = new JsonObject
            {
                ["SandboxId"] = "RETAIL",
                ["UserTokens"] = new JsonArray { xblToken }
            },
            ["RelyingParty"] = "rp://api.minecraftservices.com/",
            ["TokenType"] = "JWT"
        };
        var (status, respBody) = await PostJsonAsync(Endpoints.XstsAuthorize,
            body.ToJsonString(), ct).ConfigureAwait(false);
        if (!IsSuccess(status))
        {
            // Xbox 错误码对齐 ModLaunch.cs:1117-1156（参考 PrismarineJS/prismarine-auth）
            if (respBody.Contains("2148916227"))
                throw new MinecraftAuthException(AuthErrorKind.XboxBanned,
                    "This Microsoft account is banned by Xbox Live and cannot sign in.");
            if (respBody.Contains("2148916233"))
                throw new MinecraftAuthException(AuthErrorKind.XboxNotRegistered,
                    "This Microsoft account has no Xbox profile yet and must create one first.",
                    "https://signup.live.com/signup");
            if (respBody.Contains("2148916235"))
                throw new MinecraftAuthException(AuthErrorKind.RegionBlocked,
                    "This Microsoft account is in a restricted region and cannot sign in.");
            if (respBody.Contains("2148916238"))
                throw new MinecraftAuthException(AuthErrorKind.Underage,
                    "This Microsoft account is underage and needs an adult to complete sign-in.",
                    "https://account.live.com/editprof.aspx");
            throw NewUnknown($"Step 3 failed to obtain XSTS token: {(int)status} {status}\n{respBody}");
        }

        var json = JsonNode.Parse(respBody) ?? throw NewUnknown("Failed to parse XSTS response.");
        var xsts = json["Token"]?.ToString() ?? throw NewUnknown("XSTS response is missing the Token field.");
        var uhs = json["DisplayClaims"]?["xui"]?[0]?["uhs"]?.ToString()
                  ?? throw NewUnknown("XSTS response is missing the uhs field.");
        Log($"Step 3/6: XSTS token acquired (len={xsts.Length}, uhs={uhs}).");
        return (xsts, uhs);
    }

    #endregion

    #region Step 4：Minecraft access token

    /// <summary>{XSTS, UHS} → Minecraft access token（对应 <c>MsLoginStep4</c>，ModLaunch.cs:1190-1250）。</summary>
    private async Task<string> GetMinecraftTokenAsync(string xsts, string uhs, CancellationToken ct)
    {
        Log("Step 4/6: exchanging XSTS token for Minecraft access token...");
        var body = new JsonObject { ["identityToken"] = $"XBL3.0 x={uhs};{xsts}" };
        var (status, respBody) = await PostJsonAsync(Endpoints.MinecraftLoginWithXbox,
            body.ToJsonString(), ct).ConfigureAwait(false);

        if (status == System.Net.HttpStatusCode.TooManyRequests)
            throw new MinecraftAuthException(AuthErrorKind.TooManyRequests,
                "Too many requests; please try again shortly (429).");
        if (status == System.Net.HttpStatusCode.Forbidden)
            throw new MinecraftAuthException(AuthErrorKind.AbnormalIp,
                "Login was rejected (403). This may be an IP / anti-fraud issue — check your network or proxy.");
        if (!IsSuccess(status))
            throw NewUnknown($"Step 4 failed to obtain Minecraft access token: {(int)status} {status}\n{respBody}");

        var json = JsonNode.Parse(respBody) ?? throw NewUnknown("Failed to parse Minecraft token response.");
        var accessToken = json["access_token"]?.ToString();
        if (string.IsNullOrWhiteSpace(accessToken))
            throw NewUnknown("The Minecraft access token came back empty; the login flow is abnormal.");
        Log($"Step 4/6: Minecraft access token acquired (len={accessToken.Length}).");
        return accessToken;
    }

    #endregion

    #region Step 5：所有权校验

    /// <summary>校验账户是否持有 Minecraft（对应 <c>MsLoginStep5</c>，ModLaunch.cs:1256-1298）。</summary>
    private async Task CheckEntitlementsAsync(string mcAccessToken, CancellationToken ct)
    {
        Log("Step 5/6: checking Minecraft ownership (entitlements)...");
        var (status, respBody) = await GetAsync(Endpoints.Entitlements, mcAccessToken, ct)
            .ConfigureAwait(false);
        if (!IsSuccess(status))
            throw NewUnknown($"Step 5 ownership check failed: {(int)status} {status}\n{respBody}");

        var items = JsonNode.Parse(respBody)?["items"]?.AsArray();
        var owns = items?.Any(x =>
            x?["name"]?.ToString() is "product_minecraft" or "game_minecraft") ?? false;
        if (!owns)
            throw new MinecraftAuthException(AuthErrorKind.NotPurchased,
                "This Microsoft account does not own Minecraft and cannot sign in.",
                "https://www.xbox.com/zh-cn/games/store/minecraft-java-bedrock-edition-for-pc/9nxp44l49shj");
        Log("Step 5/6: ownership confirmed (account owns Minecraft).");
    }

    #endregion

    #region Step 6：玩家档案

    /// <summary>Minecraft access token → {UUID, UserName, ProfileJson}（对应 <c>MsLoginStep6</c>，ModLaunch.cs:1305-1374）。</summary>
    private async Task<(string uuid, string username, string profileJson)> GetProfileAsync(
        string mcAccessToken, CancellationToken ct)
    {
        Log("Step 6/6: fetching Minecraft player profile...");
        var (status, respBody) = await GetAsync(Endpoints.Profile, mcAccessToken, ct)
            .ConfigureAwait(false);

        if (status == System.Net.HttpStatusCode.TooManyRequests)
            throw new MinecraftAuthException(AuthErrorKind.TooManyRequests,
                "Too many requests; please try again shortly (429).");
        if (status == System.Net.HttpStatusCode.NotFound)
            throw new MinecraftAuthException(AuthErrorKind.ProfileNotCreated,
                "No Minecraft player profile exists yet; create one on the official site first.",
                "https://www.minecraft.net/zh-hans/msaprofile/mygames/editprofile");
        if (!IsSuccess(status))
            throw NewUnknown($"Step 6 failed to fetch profile: {(int)status} {status}\n{respBody}");

        var json = JsonNode.Parse(respBody) ?? throw NewUnknown("Failed to parse profile response.");
        var uuid = json["id"]?.ToString() ?? throw NewUnknown("Profile response is missing the id field.");
        var username = json["name"]?.ToString() ?? throw NewUnknown("Profile response is missing the name field.");
        Log($"Step 6/6: profile fetched (uuid={uuid}, username={username}).");
        return (uuid, username, respBody);
    }

    #endregion

    #region 缓存

    private bool TryGetCached([NotNullWhen(true)] out MinecraftAuthResult? result)
    {
        result = _cached;
        if (result is null) return false;
        var ttl = _options.AccessTokenCacheTtl;
        if (ttl is null) return false;
        return Environment.TickCount64 - _cacheTick < ttl.Value.TotalMilliseconds;
    }

    private void UpdateCache(MinecraftAuthResult result)
    {
        _cached = result;
        _cacheTick = Environment.TickCount64;
    }

    #endregion

    #region HTTP / 日志辅助

    private void Log(string msg) => _options.Logger?.Invoke(msg);

    private static MinecraftAuthException NewUnknown(string message) =>
        new(AuthErrorKind.Unknown, message);

    private static bool IsSuccess(System.Net.HttpStatusCode code) =>
        (int)code >= 200 && (int)code < 300;

    private async Task<string> PostFormAsync(string url, IDictionary<string, string> form,
        CancellationToken ct)
    {
        using var resp = await _http.PostAsync(url, new FormUrlEncodedContent(form), ct)
            .ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw NewUnknown($"Request to {url} failed: {(int)resp.StatusCode} {resp.StatusCode}\n{body}");
        return body;
    }

    private async Task<(System.Net.HttpStatusCode status, string body)> PostJsonAsync(
        string url, string jsonBody, CancellationToken ct)
    {
        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync(url, content, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return (resp.StatusCode, body);
    }

    private async Task<(System.Net.HttpStatusCode status, string body)> GetAsync(
        string url, string bearer, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return (resp.StatusCode, body);
    }

    #endregion
}
