using Microsoft.AspNetCore.Mvc.RazorPages;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages;

public abstract class AppPageModel : PageModel
{
    protected AppPageModel(OrdevoApiClient api) => Api = api;

    protected OrdevoApiClient Api { get; }

    public List<string> Errors { get; } = [];

    protected async Task<IReadOnlyList<T>> GetListAsync<T>(string path, CancellationToken ct)
    {
        var result = await Api.GetAsync<List<T>>(path, ct);
        if (result.IsSuccess)
            return result.Value ?? [];

        AddApiError(result);
        return [];
    }

    protected async Task<T?> GetOneAsync<T>(string path, CancellationToken ct)
    {
        var result = await Api.GetAsync<T>(path, ct);
        if (result.IsSuccess)
            return result.Value;

        AddApiError(result);
        return default;
    }

    protected void AddUiError(string message)
    {
        var safeMessage = FriendlyError.Message(message);
        Errors.Add(safeMessage);
    }

    protected void AddApiError<T>(ApiResult<T> result)
        => Errors.Add(UiFormat.Error(result));

    protected void NotifySuccess(string message)
        => TempData["ToastSuccess"] = message;

    protected void NotifyError(string message)
        => TempData["ToastError"] = FriendlyError.Message(message);
}
