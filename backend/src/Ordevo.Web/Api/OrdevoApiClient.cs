using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Ordevo.Web.Models;

namespace Ordevo.Web.Api;

public sealed class OrdevoApiClient(HttpClient http, IHttpContextAccessor accessor)
{
    private const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // stash tokens on HttpContext.Items so every client in the same request shares the latest one
    private const string ItemAccess = "__ordevo.access_token";
    private const string ItemRefresh = "__ordevo.refresh_token";
    private const string ItemExpires = "__ordevo.expires_at";
    private const string ItemRefreshed = "__ordevo.refresh_attempted";

    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<AuthResult>> LoginAsync(LoginRequest request, CancellationToken ct = default)
        => SendAsync<AuthResult>(HttpMethod.Post, "/api/identity/auth/login", request, anonymous: true, ct);

    public Task<ApiResult<UserProfile>> MeAsync(CancellationToken ct = default)
        => GetAsync<UserProfile>("/api/identity/auth/me", ct);

    public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Get, path, body: null, anonymous: false, ct);

    public Task<ApiResult<T>> PostAsync<T>(string path, object body, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Post, path, body, anonymous: false, ct);

    public Task<ApiResult<T>> PutAsync<T>(string path, object body, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Put, path, body, anonymous: false, ct);

    public Task<ApiResult<T>> DeleteAsync<T>(string path, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Delete, path, body: null, anonymous: false, ct);

    public async Task<ApiResult<string>> GetRawAsync(string path, CancellationToken ct = default)
    {
        await TryRefreshAsync(force: false, ct); // refresh early if it's about to expire

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        await AttachBearerAsync(request);

        try
        {
            using var response = await http.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshAsync(force: true, ct))
                return await GetRawRetryAsync(path, ct);

            var content = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<string>(null, (int)response.StatusCode, ExtractError(content, response.ReasonPhrase, (int)response.StatusCode));
            return new ApiResult<string>(content, (int)response.StatusCode, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ApiResult<string>(null, 0, FriendlyError.ByStatus(0));
        }
    }

    private async Task<ApiResult<string>> GetRawRetryAsync(string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        await AttachBearerAsync(request);
        try
        {
            using var response = await http.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<string>(null, (int)response.StatusCode, ExtractError(content, response.ReasonPhrase, (int)response.StatusCode));
            return new ApiResult<string>(content, (int)response.StatusCode, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ApiResult<string>(null, 0, FriendlyError.ByStatus(0));
        }
    }

    private Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method, string path, object? body, bool anonymous, CancellationToken ct)
        => SendCoreAsync<T>(method, path, body, anonymous, allowRetry: !anonymous, ct);

    private async Task<ApiResult<T>> SendCoreAsync<T>(
        HttpMethod method, string path, object? body, bool anonymous, bool allowRetry, CancellationToken ct)
    {
        if (!anonymous)
            await TryRefreshAsync(force: false, ct); // refresh early if it's about to expire

        using var request = new HttpRequestMessage(method, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!anonymous)
            await AttachBearerAsync(request);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        try
        {
            using var response = await http.SendAsync(request, ct);

            // Access token rejected mid-session: refresh once and replay the request.
            if (response.StatusCode == HttpStatusCode.Unauthorized && allowRetry
                && await TryRefreshAsync(force: true, ct))
            {
                return await SendCoreAsync<T>(method, path, body, anonymous, allowRetry: false, ct);
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<T>(default, (int)response.StatusCode, ExtractError(content, response.ReasonPhrase, (int)response.StatusCode));

            if (string.IsNullOrWhiteSpace(content))
                return new ApiResult<T>(default, (int)response.StatusCode, null);

            var value = JsonSerializer.Deserialize<T>(content, JsonOptions);
            return new ApiResult<T>(value, (int)response.StatusCode, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ApiResult<T>(default, 0, FriendlyError.ByStatus(0));
        }
        catch (JsonException)
        {
            return new ApiResult<T>(default, 500, FriendlyError.ByStatus(500));
        }
    }

    private async Task AttachBearerAsync(HttpRequestMessage request)
    {
        var httpContext = accessor.HttpContext;
        if (httpContext is null)
            return;

        var token = ReadItem(httpContext, ItemAccess) ?? await httpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // Gets a fresh access token from the refresh token. Only runs once per request: refresh tokens
    // rotate and the server flags reuse, so refreshing twice would kill the whole token family.
    private async Task<bool> TryRefreshAsync(bool force, CancellationToken ct)
    {
        var httpContext = accessor.HttpContext;
        if (httpContext is null)
            return false;

        // One refresh attempt per request — any later caller reuses that outcome.
        if (httpContext.Items.TryGetValue(ItemRefreshed, out var attempted) && attempted is true)
            return ReadItem(httpContext, ItemAccess) is not null;

        if (!force)
        {
            var expiresRaw = ReadItem(httpContext, ItemExpires) ?? await httpContext.GetTokenAsync("expires_at");
            // Only pre-emptively refresh when we can see the token is (nearly) expired.
            if (!DateTimeOffset.TryParse(expiresRaw, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var expiresAt))
                return false;
            if (expiresAt - ExpiryBuffer > DateTimeOffset.UtcNow)
                return false;
        }

        var refreshToken = ReadItem(httpContext, ItemRefresh) ?? await httpContext.GetTokenAsync("refresh_token");
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        httpContext.Items[ItemRefreshed] = true;

        var result = await SendCoreAsync<AuthResult>(
            HttpMethod.Post, "/api/identity/auth/refresh",
            new RefreshBody(refreshToken), anonymous: true, allowRetry: false, ct);

        if (!result.IsSuccess || result.Value is null)
            return false;

        var tokens = result.Value.Tokens;

        // Cache for the remainder of this request.
        httpContext.Items[ItemAccess] = tokens.AccessToken;
        httpContext.Items[ItemRefresh] = tokens.RefreshToken;
        httpContext.Items[ItemExpires] = tokens.AccessTokenExpiresAt.ToString("O");

        // Persist rotated tokens into the auth cookie for subsequent requests.
        try
        {
            if (!httpContext.Response.HasStarted)
            {
                var auth = await httpContext.AuthenticateAsync(Scheme);
                if (auth.Succeeded && auth.Principal is not null && auth.Properties is not null)
                {
                    auth.Properties.UpdateTokenValue("access_token", tokens.AccessToken);
                    auth.Properties.UpdateTokenValue("refresh_token", tokens.RefreshToken);
                    auth.Properties.UpdateTokenValue("expires_at", tokens.AccessTokenExpiresAt.ToString("O"));
                    await httpContext.SignInAsync(Scheme, auth.Principal, auth.Properties);
                }
            }
        }
        catch
        {
            // Cookie couldn't be updated (response already started) — the per-request Items cache
            // still serves the fresh token for this request; next request will refresh again.
        }

        return true;
    }

    private static string? ReadItem(HttpContext httpContext, string key)
        => httpContext.Items.TryGetValue(key, out var value) ? value as string : null;

    private static string ExtractError(string content, string? fallback, int statusCode)
    {
        if (string.IsNullOrWhiteSpace(content))
            return FriendlyError.ByStatus(statusCode);

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            var detail = root.TryGetProperty("detail", out var d) ? d.GetString() : null;
            var message = root.TryGetProperty("message", out var m) ? m.GetString() : null;
            var code = root.TryGetProperty("code", out var c) ? c.GetString() : null;
            if (root.TryGetProperty("errors", out _))
                return FriendlyError.ByStatus(statusCode);

            return FriendlyError.FromProblem(code ?? title, detail ?? message, statusCode);
        }
        catch (JsonException)
        {
            return FriendlyError.Message(content, statusCode);
        }
    }

    private sealed record RefreshBody(string RefreshToken);
}
