using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Shared.Validators;

namespace LittleEcommerce.Source.Shared.DTOs.Requests.Users;

public record UpdateProfileRequest(
    [Required(ErrorMessage = "Informe o nome")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "O nome precisa ter de 2 a 150 caracteres")]
    string Name
);

public record ChangePasswordRequest(
    [Required(ErrorMessage = "Informe a senha atual")]
    string CurrentPassword,
    [Required(ErrorMessage = "Informe a nova senha")]
    [StrongPassword]
    string NewPassword
);
