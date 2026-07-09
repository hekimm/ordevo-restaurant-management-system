using Microsoft.AspNetCore.Http;
using Ordevo.BuildingBlocks.Results;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Ordevo.BuildingBlocks.Http;

public static class ResultHttp
{
    public static IResult ToProblem(this Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return HttpResults.Problem(title: error.Code, detail: SafeDetail(error.Message, status), statusCode: status);
    }

    public static IResult Match<T>(this Result<T> result, Func<T, IResult> onSuccess)
        => result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblem();

    public static IResult Match(this Result result, Func<IResult> onSuccess)
        => result.IsSuccess ? onSuccess() : result.Error.ToProblem();

    private static string SafeDetail(string? message, int status)
    {
        if (string.IsNullOrWhiteSpace(message))
            return DetailByStatus(status);

        var value = message.Trim();
        return LooksTechnical(value) ? DetailByStatus(status) : value.Length > 180 ? DetailByStatus(status) : value;
    }

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

    private static string DetailByStatus(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Bilgileri kontrol edip tekrar deneyin.",
        StatusCodes.Status401Unauthorized => "Oturumunuz sona ermiş olabilir. Lütfen tekrar giriş yapın.",
        StatusCodes.Status403Forbidden => "Bu işlem için yetkiniz yok.",
        StatusCodes.Status404NotFound => "Aradığınız kayıt bulunamadı.",
        StatusCodes.Status409Conflict => "Bu işlem mevcut durum nedeniyle tamamlanamadı.",
        >= StatusCodes.Status500InternalServerError => "İşlem şu anda tamamlanamadı. Lütfen kısa süre sonra tekrar deneyin.",
        _ => "İşlem tamamlanamadı. Lütfen tekrar deneyin."
    };
}
