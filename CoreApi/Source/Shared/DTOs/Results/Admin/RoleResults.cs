namespace LittleEcommerce.Source.Shared.DTOs.Results.Admin;

public record RoleResult(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<string> Permissions,
    int Users
);

public record PermissionResult(
    string Code,
    string Group,
    string Description
);
