using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ordevo.Desktop.Wpf.Models;

namespace Ordevo.Desktop.Wpf.Services;

public sealed class OrdevoApiClient(HttpClient http, DesktopSession session, OfflineStore offline)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Uri BaseAddress => http.BaseAddress!;

    public Task<ApiResult<AuthResult>> LoginAsync(LoginRequest request, CancellationToken ct = default)
        => SendAsync<AuthResult>(HttpMethod.Post, "/api/identity/auth/login", request, cacheKey: null, anonymous: true, allowRefresh: false, ct);

    public Task<ApiResult<AuthResult>> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
        => SendAsync<AuthResult>(HttpMethod.Post, "/api/identity/auth/refresh", request, cacheKey: null, anonymous: true, allowRefresh: false, ct);

    public Task<ApiResult<NoContent>> LogoutAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(session.RefreshToken))
            return Task.FromResult(new ApiResult<NoContent>(new NoContent(), 204, null));

        return SendAsync<NoContent>(HttpMethod.Post, "/api/identity/auth/logout", new LogoutRequest(session.RefreshToken), cacheKey: null, anonymous: true, allowRefresh: false, ct);
    }

    public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Get, path, body: null, cacheKey: path, anonymous: false, allowRefresh: true, ct);

    public Task<ApiResult<T>> PostAsync<T>(string path, object body, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Post, path, body, cacheKey: null, anonymous: false, allowRefresh: true, ct);

    public Task<ApiResult<T>> PutAsync<T>(string path, object body, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Put, path, body, cacheKey: null, anonymous: false, allowRefresh: true, ct);

    public Task<ApiResult<T>> DeleteAsync<T>(string path, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Delete, path, body: null, cacheKey: null, anonymous: false, allowRefresh: true, ct);

    public async Task<ApiResult<string>> DownloadStringAsync(string path, CancellationToken ct = default)
    {
        var result = await SendStringOnceAsync(path, ct);
        if (result.StatusCode == (int)HttpStatusCode.Unauthorized && await TryRefreshAsync(ct))
            result = await SendStringOnceAsync(path, ct);

        return result;
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        string? cacheKey,
        bool anonymous,
        bool allowRefresh,
        CancellationToken ct)
    {
        var result = await SendOnceAsync<T>(method, path, body, cacheKey, anonymous, ct);
        if (!anonymous && allowRefresh && result.StatusCode == (int)HttpStatusCode.Unauthorized && await TryRefreshAsync(ct))
            result = await SendOnceAsync<T>(method, path, body, cacheKey, anonymous, ct);

        return result;
    }

    private async Task<ApiResult<T>> SendOnceAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        string? cacheKey,
        bool anonymous,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!anonymous && !string.IsNullOrWhiteSpace(session.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        try
        {
            using var response = await http.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return await FromCacheOrError<T>(cacheKey, (int)response.StatusCode, ExtractError(content, response.ReasonPhrase, (int)response.StatusCode), ct);

            if (!string.IsNullOrWhiteSpace(cacheKey))
                await offline.SaveAsync(cacheKey, content, ct);

            if (typeof(T) == typeof(NoContent))
                return new ApiResult<T>((T)(object)new NoContent(), (int)response.StatusCode, null);

            if (typeof(T) == typeof(string))
                return new ApiResult<T>((T)(object)content, (int)response.StatusCode, null);

            if (string.IsNullOrWhiteSpace(content))
                return new ApiResult<T>(default, (int)response.StatusCode, null);

            var value = JsonSerializer.Deserialize<T>(content, JsonOptions);
            return new ApiResult<T>(value, (int)response.StatusCode, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return await FromCacheOrError<T>(cacheKey, 0, "Sistemle bağlantı kurulamadı; varsa lokal cache kullanılıyor.", ct);
        }
    }

    public async Task<ApiResult<byte[]>> DownloadBytesAsync(string path, CancellationToken ct = default)
    {
        var result = await SendBytesOnceAsync(path, ct);
        if (result.StatusCode == (int)HttpStatusCode.Unauthorized && await TryRefreshAsync(ct))
            result = await SendBytesOnceAsync(path, ct);

        return result;
    }

    private async Task<ApiResult<byte[]>> SendBytesOnceAsync(string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        try
        {
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                return new ApiResult<byte[]>(null, (int)response.StatusCode, ExtractError(err, response.ReasonPhrase, (int)response.StatusCode));
            }
            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            return new ApiResult<byte[]>(bytes, (int)response.StatusCode, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ApiResult<byte[]>(null, 0, FriendlyError(null, 0));
        }
    }

    private async Task<ApiResult<string>> SendStringOnceAsync(string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        try
        {
            using var response = await http.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return response.IsSuccessStatusCode
                ? new ApiResult<string>(content, (int)response.StatusCode, null)
                : new ApiResult<string>(null, (int)response.StatusCode, ExtractError(content, response.ReasonPhrase, (int)response.StatusCode));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ApiResult<string>(null, 0, FriendlyError(null, 0));
        }
    }

    private async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(session.RefreshToken))
            return false;

        var result = await RefreshAsync(new RefreshRequest(session.RefreshToken), ct);
        if (!result.IsSuccess || result.Value is null)
            return false;

        session.SignIn(result.Value);
        return true;
    }

    private async Task<ApiResult<T>> FromCacheOrError<T>(string? cacheKey, int statusCode, string error, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(cacheKey))
        {
            var cached = await offline.LoadAsync(cacheKey, ct);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                var cachedValue = JsonSerializer.Deserialize<T>(cached, JsonOptions);
                return new ApiResult<T>(cachedValue, statusCode, null, FromCache: true);
            }
        }

        return new ApiResult<T>(default, statusCode, error);
    }

    private static string ExtractError(string content, string? fallback, int statusCode)
    {
        if (string.IsNullOrWhiteSpace(content))
            return StatusMessage(statusCode);

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            var detail = root.TryGetProperty("detail", out var d) ? d.GetString() : null;
            var message = root.TryGetProperty("message", out var m) ? m.GetString() : null;
            var code = root.TryGetProperty("code", out var c) ? c.GetString() : null;
            if (root.TryGetProperty("errors", out _))
                return StatusMessage(statusCode);
            return FriendlyError(code ?? title ?? detail ?? message, statusCode);
        }
        catch (JsonException)
        {
            return FriendlyError(content, statusCode);
        }
    }

    private static string FriendlyError(string? raw, int statusCode)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return StatusMessage(statusCode);

        var value = raw.Trim();
        var firstPart = value.Split(" - ", 2, StringSplitOptions.TrimEntries)[0];
        if (CodeMessages.TryGetValue(firstPart, out var direct))
            return direct;

        foreach (var (code, message) in CodeMessages)
        {
            if (value.Contains(code, StringComparison.OrdinalIgnoreCase))
                return message;
        }

        return LooksTechnical(value) ? StatusMessage(statusCode) : value.Length > 180 ? StatusMessage(statusCode) : value;
    }

    private static readonly IReadOnlyDictionary<string, string> CodeMessages =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["branch.required"] = "Bu işlem için aktif bir şube seçili olmalı.",
            ["identity.invalid_credentials"] = "Giriş bilgileri uyuşmuyor. Lütfen tekrar deneyin.",
            ["auth.invalid_credentials"] = "Giriş bilgileri uyuşmuyor. Lütfen tekrar deneyin.",
            ["auth.unauthorized"] = "Bu işlem için yeniden giriş yapmanız gerekiyor.",
            ["auth.forbidden"] = "Bu işlem için yetkiniz yok.",
            ["order.not_found"] = "Adisyon bulunamadı veya artık açık değil.",
            ["order.item_not_found"] = "Seçilen adisyon kalemi bulunamadı.",
            ["order.table_busy"] = "Seçilen masada açık bir adisyon var.",
            ["order.invalid_item"] = "Bu ürün şu anda siparişe eklenemiyor.",
            ["order.invalid_qty"] = "Adet bilgisi geçerli değil.",
            ["order.split_empty"] = "Ayırmak için en az bir kalem seçin.",
            ["kds.order_closed"] = "Bu adisyon kapalı olduğu için mutfak durumu değiştirilemez.",
            ["validation.failed"] = "Bilgileri kontrol edip tekrar deneyin.",
            ["not_found"] = "Kayıt bulunamadı.",
            ["conflict"] = "Bu işlem mevcut durum nedeniyle tamamlanamadı."
        };

    private static bool LooksTechnical(string value)
        => value.Contains("Exception", StringComparison.OrdinalIgnoreCase)
            || value.Contains("stack", StringComparison.OrdinalIgnoreCase)
            || value.Contains("trace", StringComparison.OrdinalIgnoreCase)
            || value.Contains("ORA-", StringComparison.OrdinalIgnoreCase)
            || value.Contains("SQL", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Dapper", StringComparison.OrdinalIgnoreCase)
            || value.Contains("System.", StringComparison.Ordinal)
            || value.Contains("/api/", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Bad Request", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Unauthorized", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Forbidden", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Not Found", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Conflict", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Internal Server Error", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("at ", StringComparison.OrdinalIgnoreCase)
            || value.Contains(" line ", StringComparison.OrdinalIgnoreCase);

    private static string StatusMessage(int statusCode) => statusCode switch
    {
        0 => "Sistemle bağlantı kurulamadı. Lütfen bağlantınızı kontrol edin.",
        400 => "Bilgileri kontrol edip tekrar deneyin.",
        401 => "Oturumunuz sona ermiş olabilir. Lütfen tekrar giriş yapın.",
        403 => "Bu işlem için yetkiniz yok.",
        404 => "Aradığınız kayıt bulunamadı.",
        409 => "Bu işlem mevcut durum nedeniyle tamamlanamadı.",
        422 => "Bilgileri kontrol edip tekrar deneyin.",
        >= 500 => "İşlem şu anda tamamlanamadı. Lütfen kısa süre sonra tekrar deneyin.",
        _ => "İşlem tamamlanamadı. Lütfen tekrar deneyin."
    };
}
