using System.Text.RegularExpressions;
using Ordevo.Web.Api;

namespace Ordevo.Web.Models;

public sealed record MetricCard(string Label, string Value, string Detail, string Accent);

public static class UiFormat
{
    public static string Money(decimal value) => value.ToString("N2");

    public static string ShortId(string? value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value.Length <= 8 ? value : value[..8];

    public static string Error<T>(ApiResult<T> result)
        => FriendlyError.Message(result.Error, result.StatusCode);
}

public static partial class FriendlyError
{
    private static readonly IReadOnlyDictionary<string, string> CodeMessages =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["branch.required"] = "Bu işlem için aktif bir şube seçili olmalı.",
            ["identity.invalid_credentials"] = "Giriş bilgileri uyuşmuyor. Lütfen tekrar deneyin.",
            ["auth.invalid_credentials"] = "Giriş bilgileri uyuşmuyor. Lütfen tekrar deneyin.",
            ["auth.unauthorized"] = "Bu işlem için yeniden giriş yapmanız gerekiyor.",
            ["auth.forbidden"] = "Bu işlem için yetkiniz yok.",
            ["user.name_required"] = "Personel adını kontrol edip tekrar deneyin.",
            ["user.pin_invalid"] = "PIN 6 haneli rakamlardan oluşmalı.",
            ["user.no_roles"] = "Personel rolü seçilemedi. Lütfen ayarları kontrol edin.",
            ["user.invalid_roles"] = "Seçilen personel rolü kullanılamıyor.",
            ["user.not_found"] = "Personel kaydı bulunamadı.",
            ["order.not_found"] = "Adisyon bulunamadı veya artık açık değil.",
            ["order.item_not_found"] = "Seçilen adisyon kalemi bulunamadı.",
            ["order.table_busy"] = "Seçilen masada açık bir adisyon var.",
            ["order.invalid_item"] = "Bu ürün şu anda siparişe eklenemiyor.",
            ["order.invalid_qty"] = "Adet bilgisi geçerli değil.",
            ["order.split_empty"] = "Ayırmak için en az bir kalem seçin.",
            ["fiscal.terminal.required"] = "Kartlı tahsilat için POS cihazı seçin.",
            ["fiscal.terminal.not_found"] = "Seçilen POS cihazı bulunamadı veya pasif.",
            ["fiscal.terminal.failed"] = "POS cihazı işlemi tamamlayamadı. Ödeme kaydedilmedi.",
            ["fiscal.terminal.unreachable"] = "POS cihazından yanıt alınamadı. Ödeme kaydedilmedi.",
            ["einvoice.provider"] = "e-Belge sağlayıcısından yanıt alınamadı. Belge daha sonra tekrar gönderilebilir.",
            ["kds.order_closed"] = "Bu adisyon kapalı olduğu için mutfak durumu değiştirilemez.",
            ["validation.failed"] = "Bilgileri kontrol edip tekrar deneyin.",
            ["not_found"] = "Kayıt bulunamadı.",
            ["conflict"] = "Bu işlem mevcut durum nedeniyle tamamlanamadı."
        };

    public static string Message(string? raw, int statusCode = 0)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return ByStatus(statusCode);

        var value = raw.Trim();
        var firstPart = value.Split(" - ", 2, StringSplitOptions.TrimEntries)[0];
        if (CodeMessages.TryGetValue(firstPart, out var mapped))
            return mapped;

        foreach (var (code, message) in CodeMessages)
        {
            if (value.Contains(code, StringComparison.OrdinalIgnoreCase))
                return message;
        }

        if (LooksTechnical(value))
            return ByStatus(statusCode);

        return value.Length > 180 ? ByStatus(statusCode) : value;
    }

    public static string FromProblem(string? title, string? detail, int statusCode)
    {
        if (!string.IsNullOrWhiteSpace(title) && CodeMessages.TryGetValue(title.Trim(), out var mapped))
            return mapped;

        if (!string.IsNullOrWhiteSpace(detail) && !LooksTechnical(detail))
            return Message(detail, statusCode);

        if (!string.IsNullOrWhiteSpace(title) && !LooksTechnical(title))
            return Message(title, statusCode);

        return ByStatus(statusCode);
    }

    public static string ByStatus(int statusCode) => statusCode switch
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

    private static bool LooksTechnical(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return TechnicalPattern().IsMatch(value)
            || value.Contains("Exception", StringComparison.OrdinalIgnoreCase)
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
            || value.Equals("Internal Server Error", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"(^[a-z0-9_.-]+$)|(\bat\s+\w+\.)|(\bline\s+\d+\b)", RegexOptions.IgnoreCase)]
    private static partial Regex TechnicalPattern();
}
