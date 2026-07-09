using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ordevo.Modules.EInvoice.Application;

namespace Ordevo.Modules.EInvoice.Infrastructure;

public sealed class SpecialIntegratorEInvoiceProvider(
    IHttpClientFactory clients,
    IConfiguration configuration,
    ILogger<SpecialIntegratorEInvoiceProvider> logger) : IEInvoiceProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public string Name => configuration["EInvoice:SpecialIntegrator:ProviderCode"] ?? "special-integrator";

    public async Task<EInvoiceIssueResult> IssueAsync(EInvoiceDocumentModel document, CancellationToken ct = default)
    {
        var fallback = new EInvoiceIssueResult(false, "error", null, null, null, null, null,
            "e-Belge sağlayıcısından yanıt alınamadı.");
        return await SendAsync(
            configuration["EInvoice:SpecialIntegrator:IssuePath"] ?? "/documents/issue",
            document,
            fallback,
            ct);
    }

    public async Task<EInvoiceStatusResult> GetStatusAsync(string externalId, CancellationToken ct = default)
    {
        var path = (configuration["EInvoice:SpecialIntegrator:StatusPath"] ?? "/documents/{externalId}/status")
            .Replace("{externalId}", Uri.EscapeDataString(externalId), StringComparison.OrdinalIgnoreCase);
        return await SendAsync(
            path,
            new { externalId },
            new EInvoiceStatusResult(false, "error", null, "e-Belge durumu alınamadı."),
            ct);
    }

    public async Task<EInvoiceCancelResult> CancelAsync(string externalId, string reason, CancellationToken ct = default)
    {
        var path = (configuration["EInvoice:SpecialIntegrator:CancelPath"] ?? "/documents/{externalId}/cancel")
            .Replace("{externalId}", Uri.EscapeDataString(externalId), StringComparison.OrdinalIgnoreCase);
        return await SendAsync(
            path,
            new { externalId, reason },
            new EInvoiceCancelResult(false, "error", null, "e-Belge iptal isteği gönderilemedi."),
            ct);
    }

    private async Task<TResult> SendAsync<TBody, TResult>(
        string path,
        TBody body,
        TResult fallback,
        CancellationToken ct)
    {
        var baseUrl = configuration["EInvoice:SpecialIntegrator:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return WithError(fallback, "e-Belge sağlayıcı bağlantısı yapılandırılmamış.");

        using var client = clients.CreateClient("SpecialIntegratorEInvoice");
        client.Timeout = TimeSpan.FromSeconds(configuration.GetValue("EInvoice:SpecialIntegrator:TimeoutSeconds", 45));
        using var request = new HttpRequestMessage(HttpMethod.Post, Combine(baseUrl, path));
        ApplyAuth(request);
        request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return WithRaw(fallback, raw, "e-Belge sağlayıcısı isteği kabul etmedi.");

            var parsed = JsonSerializer.Deserialize<TResult>(raw, Json);
            return parsed is null ? WithRaw(fallback, raw, "e-Belge sağlayıcısından geçerli yanıt alınamadı.") : parsed;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Special integrator e-document request timed out.");
            return WithError(fallback, "e-Belge sağlayıcısından zamanında yanıt alınamadı.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Special integrator e-document request failed.");
            return WithError(fallback, "e-Belge sağlayıcısına ulaşılamadı.");
        }
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        var bearer = configuration["EInvoice:SpecialIntegrator:BearerToken"];
        if (!string.IsNullOrWhiteSpace(bearer))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return;
        }

        var apiKey = configuration["EInvoice:SpecialIntegrator:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var header = configuration["EInvoice:SpecialIntegrator:ApiKeyHeader"] ?? "X-Api-Key";
            request.Headers.TryAddWithoutValidation(header, apiKey);
        }
    }

    private static Uri Combine(string baseUrl, string path)
        => new(new Uri(baseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));

    private static TResult WithError<TResult>(TResult fallback, string message)
        => fallback switch
        {
            EInvoiceIssueResult x => (TResult)(object)(x with { Error = message }),
            EInvoiceStatusResult x => (TResult)(object)(x with { Error = message }),
            EInvoiceCancelResult x => (TResult)(object)(x with { Error = message }),
            _ => fallback
        };

    private static TResult WithRaw<TResult>(TResult fallback, string? raw, string message)
        => fallback switch
        {
            EInvoiceIssueResult x => (TResult)(object)(x with { RawResponseJson = raw, Error = message }),
            EInvoiceStatusResult x => (TResult)(object)(x with { RawResponseJson = raw, Error = message }),
            EInvoiceCancelResult x => (TResult)(object)(x with { RawResponseJson = raw, Error = message }),
            _ => fallback
        };
}
