using System.Text.Json;

namespace Ordevo.Modules.Identity.Application;

public sealed class SettingsService(ISettingsRepository settings)
{
    private const string DeveloperTogglesKey = "developer.toggles";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly DeveloperToggleDefinition[] ModuleDefinitions =
    [
        new("dineInOrders", "Restoran içi sipariş", "Masa/adisyon akışı ve mutfak takip ekranı.", "/tables"),
        new("menuCms", "Menü CMS", "Kategori, ürün ve seçenek yönetimi.", "/menu"),
        new("tableCms", "Masa CMS", "Bölüm ve masa yerleşimi yönetimi.", "/tables/manage"),
        new("crm", "CRM", "Müşteri, rezervasyon ve sadakat yönetimi.", "/crm"),
        new("inventory", "Stok", "Stok kalemleri, tedarikçiler ve sayım işlemleri.", "/inventory"),
        new("shift", "Personel - Cihaz", "Garson mobil erişimi, PIN ve cihaz yetkilendirme akışı.", "/personel-cihaz"),
        new("salesAnalysis", "Satış analizi", "Ciro, kategori, saatlik trend ve ödeme analizleri.", "/sales-analysis"),
        new("finance", "Finans", "Gelir/gider, hesap ve cari hareketler.", "/finance"),
        new("print", "Yazıcı", "Fiş ve mutfak yazdırma kuyruğu.", "/print")
    ];

    private static readonly DeveloperToggleDefinition[] IntegrationDefinitions =
    [
        new("externalIntegrations", "Dış entegrasyonlar", "Connector, webhook ve terminal komutları.", "/ayarlar?tab=integrations"),
        new("offlineSync", "Offline sync", "Entity kataloğu, mutation ve conflict izleme.", "/ayarlar?tab=sync"),
        new("paymentTerminal", "Ödeme terminali", "POS/terminal komut kuyruğu.", "/ayarlar?tab=integrations"),
        new("einvoice", "e-Fatura / e-Arşiv", "Belge sağlayıcı entegrasyonları.", "/settings?tab=integrations")
    ];

    public async Task<DeveloperSettingsDto> GetDeveloperSettingsAsync(string tenantId, string? branchId, CancellationToken ct = default)
    {
        var stored = await settings.GetValueAsync(tenantId, branchId, DeveloperTogglesKey, ct);
        var values = Deserialize(stored);
        return Build(values);
    }

    public async Task<DeveloperSettingsDto> UpdateDeveloperSettingsAsync(
        string tenantId,
        string? branchId,
        UpdateDeveloperSettingsRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var normalized = new DeveloperToggleState(
            Normalize(request.Modules, ModuleDefinitions),
            Normalize(request.Integrations, IntegrationDefinitions));

        await settings.UpsertValueAsync(
            tenantId,
            branchId,
            DeveloperTogglesKey,
            JsonSerializer.Serialize(normalized, JsonOptions),
            userId,
            ct);

        return Build(normalized);
    }

    private static DeveloperSettingsDto Build(DeveloperToggleState values) =>
        new(
            [.. ModuleDefinitions.Select(def => def.ToDto(values.Modules.GetValueOrDefault(def.Code, true)))],
            [.. IntegrationDefinitions.Select(def => def.ToDto(values.Integrations.GetValueOrDefault(def.Code, true)))]);

    private static Dictionary<string, bool> Normalize(
        IReadOnlyDictionary<string, bool>? submitted,
        IReadOnlyList<DeveloperToggleDefinition> definitions)
    {
        var allowed = definitions.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = definitions.ToDictionary(x => x.Code, _ => true, StringComparer.OrdinalIgnoreCase);

        if (submitted is null)
            return result;

        foreach (var (code, enabled) in submitted)
        {
            if (allowed.Contains(code))
                result[code] = enabled;
        }

        return result;
    }

    private static DeveloperToggleState Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return DeveloperToggleState.Default;

        try
        {
            return JsonSerializer.Deserialize<DeveloperToggleState>(json, JsonOptions)
                ?? DeveloperToggleState.Default;
        }
        catch (JsonException)
        {
            return DeveloperToggleState.Default;
        }
    }

    private sealed record DeveloperToggleDefinition(string Code, string Name, string Description, string Route)
    {
        public DeveloperToggleDto ToDto(bool enabled) => new(Code, Name, Description, Route, enabled);
    }

    private sealed record DeveloperToggleState(
        Dictionary<string, bool> Modules,
        Dictionary<string, bool> Integrations)
    {
        public static DeveloperToggleState Default => new(
            ModuleDefinitions.ToDictionary(x => x.Code, _ => true, StringComparer.OrdinalIgnoreCase),
            IntegrationDefinitions.ToDictionary(x => x.Code, _ => true, StringComparer.OrdinalIgnoreCase));
    }
}
