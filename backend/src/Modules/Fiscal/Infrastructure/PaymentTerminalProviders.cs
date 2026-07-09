using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ordevo.Modules.Fiscal.Application;

namespace Ordevo.Modules.Fiscal.Infrastructure;

public sealed class SandboxPaymentTerminalProvider : IPaymentTerminalProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string Name => "sandbox-gmp3";

    public Task<PaymentTerminalResult> SaleAsync(PaymentTerminalSaleRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var prefix = request.IsTest ? "TST" : "GMP";
        var reference = $"{prefix}-{now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        var payload = JsonSerializer.Serialize(new
        {
            mode = "sandbox",
            protocol = "gmp3",
            reference,
            request.Amount,
            request.Currency,
            request.IsTest,
            approvedAt = now
        }, Json);

        return Task.FromResult(new PaymentTerminalResult(
            true,
            "approved",
            reference,
            Random.Shared.Next(100000, 999999).ToString(),
            now.ToString("yyyyMMdd"),
            Random.Shared.Next(100000, 999999).ToString(),
            Random.Shared.NextInt64(100000000000, 999999999999).ToString(),
            request.IsTest ? $"TEST-{Random.Shared.Next(1000, 9999)}" : $"FIS-{Random.Shared.Next(100000, 999999)}",
            $"Z{now:yyyyMMdd}",
            "SANDBOX-GMP3",
            payload,
            null,
            null));
    }

    public Task<PaymentTerminalResult> RefundAsync(PaymentTerminalRefundRequest request, CancellationToken ct = default)
        => Task.FromResult(PaymentTerminalResult.Failed("terminal.unsupported", "İade işlemi için gerçek POS sağlayıcı adaptörü gerekli."));

    public Task<PaymentTerminalResult> VoidAsync(PaymentTerminalVoidRequest request, CancellationToken ct = default)
        => Task.FromResult(PaymentTerminalResult.Failed("terminal.unsupported", "İptal işlemi için gerçek POS sağlayıcı adaptörü gerekli."));

    public Task<PaymentTerminalResult> SettlementAsync(PaymentTerminalSettlementRequest request, CancellationToken ct = default)
        => Task.FromResult(PaymentTerminalResult.Failed("terminal.unsupported", "Günsonu işlemi için gerçek POS sağlayıcı adaptörü gerekli."));

    public Task<PaymentTerminalStatusResult> GetStatusAsync(string terminalId, CancellationToken ct = default)
        => Task.FromResult(new PaymentTerminalStatusResult(true, "online", "SANDBOX-GMP3", DateTimeOffset.UtcNow, null, null, null));
}

public sealed class HttpPaymentTerminalProvider(
    IHttpClientFactory clients,
    IConfiguration configuration,
    ILogger<HttpPaymentTerminalProvider> logger) : IPaymentTerminalProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public string Name => configuration["Fiscal:PaymentTerminal:ProviderCode"] ?? "gmp3-agent";

    public async Task<PaymentTerminalResult> SaleAsync(PaymentTerminalSaleRequest request, CancellationToken ct = default)
        => await SendAsync<PaymentTerminalSaleRequest, PaymentTerminalResult>(
            configuration["Fiscal:PaymentTerminal:SalePath"] ?? "/sale",
            request,
            PaymentTerminalResult.Failed("terminal.unreachable", "POS cihazından yanıt alınamadı. Ödeme kaydedilmedi."),
            ct);

    public async Task<PaymentTerminalResult> RefundAsync(PaymentTerminalRefundRequest request, CancellationToken ct = default)
        => await SendAsync<PaymentTerminalRefundRequest, PaymentTerminalResult>(
            configuration["Fiscal:PaymentTerminal:RefundPath"] ?? "/refund",
            request,
            PaymentTerminalResult.Failed("terminal.unreachable", "POS cihazından yanıt alınamadı. İade kaydedilmedi."),
            ct);

    public async Task<PaymentTerminalResult> VoidAsync(PaymentTerminalVoidRequest request, CancellationToken ct = default)
        => await SendAsync<PaymentTerminalVoidRequest, PaymentTerminalResult>(
            configuration["Fiscal:PaymentTerminal:VoidPath"] ?? "/void",
            request,
            PaymentTerminalResult.Failed("terminal.unreachable", "POS cihazından yanıt alınamadı. İptal kaydedilmedi."),
            ct);

    public async Task<PaymentTerminalResult> SettlementAsync(PaymentTerminalSettlementRequest request, CancellationToken ct = default)
        => await SendAsync<PaymentTerminalSettlementRequest, PaymentTerminalResult>(
            configuration["Fiscal:PaymentTerminal:SettlementPath"] ?? "/settlement",
            request,
            PaymentTerminalResult.Failed("terminal.unreachable", "POS cihazından yanıt alınamadı. Günsonu tamamlanmadı."),
            ct);

    public async Task<PaymentTerminalStatusResult> GetStatusAsync(string terminalId, CancellationToken ct = default)
        => await SendAsync<object, PaymentTerminalStatusResult>(
            configuration["Fiscal:PaymentTerminal:StatusPath"] ?? $"/terminals/{Uri.EscapeDataString(terminalId)}/status",
            new { terminalId },
            new PaymentTerminalStatusResult(false, "offline", null, null, null, "terminal.unreachable", "POS cihazından yanıt alınamadı."),
            ct);

    private async Task<TResult> SendAsync<TBody, TResult>(
        string path,
        TBody body,
        TResult fallback,
        CancellationToken ct)
    {
        var baseUrl = configuration["Fiscal:PaymentTerminal:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Fiscal payment terminal HTTP provider selected without BaseUrl.");
            return WithRaw(fallback, "terminal.not_configured", "POS cihazı bağlantısı yapılandırılmamış.");
        }

        using var client = clients.CreateClient("FiscalPaymentTerminal");
        client.Timeout = TimeSpan.FromSeconds(configuration.GetValue("Fiscal:PaymentTerminal:TimeoutSeconds", 45));
        using var request = new HttpRequestMessage(HttpMethod.Post, Combine(baseUrl, path));
        ApplyAuth(request);
        request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return WithRaw(fallback, "terminal.declined", "POS cihazı işlemi onaylamadı.", raw);

            var parsed = JsonSerializer.Deserialize<TResult>(raw, Json);
            return parsed is null ? WithRaw(fallback, "terminal.bad_response", "POS cihazından geçerli yanıt alınamadı.", raw) : parsed;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Fiscal payment terminal request timed out.");
            return WithRaw(fallback, "terminal.timeout", "POS cihazından zamanında yanıt alınamadı.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Fiscal payment terminal request failed.");
            return WithRaw(fallback, "terminal.unreachable", "POS cihazından yanıt alınamadı.");
        }
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        var bearer = configuration["Fiscal:PaymentTerminal:BearerToken"];
        if (!string.IsNullOrWhiteSpace(bearer))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return;
        }

        var apiKey = configuration["Fiscal:PaymentTerminal:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var header = configuration["Fiscal:PaymentTerminal:ApiKeyHeader"] ?? "X-Api-Key";
            request.Headers.TryAddWithoutValidation(header, apiKey);
        }
    }

    private static Uri Combine(string baseUrl, string path)
        => new(new Uri(baseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));

    private static TResult WithRaw<TResult>(TResult fallback, string code, string message, string? raw = null)
        => fallback switch
        {
            PaymentTerminalResult => (TResult)(object)PaymentTerminalResult.Failed(code, message, raw),
            PaymentTerminalStatusResult => (TResult)(object)new PaymentTerminalStatusResult(false, "offline", null, null, raw, code, message),
            _ => fallback
        };
}
