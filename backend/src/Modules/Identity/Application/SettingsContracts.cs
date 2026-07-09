namespace Ordevo.Modules.Identity.Application;

public sealed record DeveloperToggleDto(
    string Code,
    string Name,
    string Description,
    string Route,
    bool IsEnabled);

public sealed record DeveloperSettingsDto(
    IReadOnlyList<DeveloperToggleDto> Modules,
    IReadOnlyList<DeveloperToggleDto> Integrations);

public sealed record UpdateDeveloperSettingsRequest(
    IReadOnlyDictionary<string, bool> Modules,
    IReadOnlyDictionary<string, bool> Integrations);

