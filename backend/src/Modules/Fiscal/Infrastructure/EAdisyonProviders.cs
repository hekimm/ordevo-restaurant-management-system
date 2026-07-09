using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ordevo.Modules.Fiscal.Application;

namespace Ordevo.Modules.Fiscal.Infrastructure;

public sealed class NullEAdisyonProvider : IEAdisyonProvider
{
    public string Name => "disabled";

    public Task<EAdisyonNotifyResult> NotifyPaidAsync(EAdisyonNotifyRequest request, CancellationToken ct = default)
        => Task.FromResult(new EAdisyonNotifyResult(true, "skipped", null, null, null, null));
}

public sealed class HttpEAdisyonProvider(
    IHttpClientFactory clients,
    IConfiguration configuration,
    ILogger<HttpEAdisyonProvider> logger) : IEAdisyonProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public string Name => configuration["Fiscal:EAdisyon:ProviderCode"] ?? "special-integrator";

    public async Task<EAdisyonNotifyResult> NotifyPaidAsync(EAdisyonNotifyRequest request, CancellationToken ct = default)
    {
        var baseUrl = configuration["Fiscal:EAdisyon:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return new EAdisyonNotifyResult(false, "error", null, null, "eadisyon.not_configured", "e-Adisyon bağlantısı yapılandırılmamış.");

        var path = configuration["Fiscal:EAdisyon:NotifyPaidPath"] ?? "/eadisyon/paid";
        using var client = clients.CreateClient("FiscalEAdisyon");
        client.Timeout = TimeSpan.FromSeconds(configuration.GetValue("Fiscal:EAdisyon:TimeoutSeconds", 30));
        using var message = new HttpRequestMessage(HttpMethod.Post, Combine(baseUrl, path));
        ApplyAuth(message);
        message.Content = new StringContent(JsonSerializer.Serialize(request, Json), Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(message, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new EAdisyonNotifyResult(false, "error", null, raw, "eadisyon.rejected", "e-Adisyon bildirimi kabul edilmedi.");

            var parsed = JsonSerializer.Deserialize<EAdisyonNotifyResult>(raw, Json);
            return parsed ?? new EAdisyonNotifyResult(false, "error", null, raw, "eadisyon.bad_response", "e-Adisyon sağlayıcısından geçerli yanıt alınamadı.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "EAdisyon notification failed.");
            return new EAdisyonNotifyResult(false, "error", null, null, "eadisyon.unreachable", "e-Adisyon sağlayıcısına ulaşılamadı.");
        }
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        var bearer = configuration["Fiscal:EAdisyon:BearerToken"];
        if (!string.IsNullOrWhiteSpace(bearer))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return;
        }

        var apiKey = configuration["Fiscal:EAdisyon:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var header = configuration["Fiscal:EAdisyon:ApiKeyHeader"] ?? "X-Api-Key";
            request.Headers.TryAddWithoutValidation(header, apiKey);
        }
    }

    private static Uri Combine(string baseUrl, string path)
        => new(new Uri(baseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));
}
