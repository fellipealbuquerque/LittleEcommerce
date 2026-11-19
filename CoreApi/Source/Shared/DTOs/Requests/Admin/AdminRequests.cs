using System.ComponentModel.DataAnnotations;

namespace LittleEcommerce.Source.Shared.DTOs.Requests.Admin;

public record ChangeUserRoleRequest(
    [Required(ErrorMessage = "Informe o papel")]
    string Role
);

public record CreateRoleRequest(
    [Required(ErrorMessage = "Informe o nome")]
    [RegularExpression("^[A-Za-z][A-Za-z0-9_-]{1,49}$", ErrorMessage = "Use de 2 a 50 caracteres: letras, números, _ ou -, começando com letra")]
    string Name,
    [StringLength(255, ErrorMessage = "Descrição muito longa")]
    string? Description,
    [Required(ErrorMessage = "Informe as permissões")]
    IReadOnlyList<string> Permissions
);

public record UpdateRolePermissionsRequest(
    [Required(ErrorMessage = "Informe as permissões")]
    IReadOnlyList<string> Permissions
);
