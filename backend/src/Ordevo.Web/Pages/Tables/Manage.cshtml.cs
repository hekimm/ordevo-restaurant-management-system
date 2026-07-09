using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Tables;

public sealed class ManageModel(OrdevoApiClient api) : AppPageModel(api)
{
    public IReadOnlyList<SectionDto> Sections { get; private set; } = [];
    public IReadOnlyList<TableDto> Tables { get; private set; } = [];

    [BindProperty]
    public SectionInput Section { get; set; } = new();

    [BindProperty]
    public TableInput Table { get; set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostSectionAsync(CancellationToken ct)
    {
        var result = string.IsNullOrWhiteSpace(Section.Id)
            ? await Api.PostAsync<SectionDto>("/api/ordering/sections", new { Section.Name, Section.SortOrder }, ct)
            : await Api.PutAsync<SectionDto>($"/api/ordering/sections/{Section.Id}", new { Section.Name, Section.SortOrder }, ct);

        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteSectionAsync(string id, CancellationToken ct)
    {
        var result = await Api.DeleteAsync<string>($"/api/ordering/sections/{id}", ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostTableAsync(CancellationToken ct)
    {
        var body = new
        {
            Table.Name,
            SectionId = string.IsNullOrWhiteSpace(Table.SectionId) ? null : Table.SectionId,
            Table.Capacity,
            Table.SortOrder,
            Table.IsActive
        };

        var result = string.IsNullOrWhiteSpace(Table.Id)
            ? await Api.PostAsync<TableDto>("/api/ordering/tables", body, ct)
            : await Api.PutAsync<TableDto>($"/api/ordering/tables/{Table.Id}", body, ct);

        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteTableAsync(string id, CancellationToken ct)
    {
        var result = await Api.DeleteAsync<string>($"/api/ordering/tables/{id}", ct);
        return await CompleteMutationAsync(result, ct);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Sections = await GetListAsync<SectionDto>("/api/ordering/sections", ct);
        Tables = await GetListAsync<TableDto>("/api/ordering/tables", ct);
        Table.IsActive = true;
        Table.Capacity = Table.Capacity == 0 ? 2 : Table.Capacity;
        Table.SectionId = string.IsNullOrWhiteSpace(Table.SectionId) ? Sections.FirstOrDefault()?.Id : Table.SectionId;
    }

    private async Task<IActionResult> CompleteMutationAsync<T>(ApiResult<T> result, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            NotifySuccess("Masa düzeni güncellendi.");
            return RedirectToPage();
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    public sealed class SectionInput
    {
        public string? Id { get; set; }
        public string Name { get; set; } = "";
        public int SortOrder { get; set; }
    }

    public sealed class TableInput
    {
        public string? Id { get; set; }
        public string? SectionId { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; } = 2;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
