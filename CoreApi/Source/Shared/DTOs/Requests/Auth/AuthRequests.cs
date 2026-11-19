using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Shared.Validators;

namespace LittleEcommerce.Source.Shared.DTOs.Requests.Auth;

public record RegisterRequest(
    [Required(ErrorMessage = "Informe o nome")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "O nome precisa ter de 2 a 150 caracteres")]
    string Name,
    [Required(ErrorMessage = "Informe o e-mail")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [StringLength(254, ErrorMessage = "E-mail muito longo")]
    string Email,
    [Required(ErrorMessage = "Informe a senha")]
    [StrongPassword]
    string Password
);

public record LoginRequest(
    [Required(ErrorMessage = "Informe o e-mail")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    string Email,
    [Required(ErrorMessage = "Informe a senha")]
    string Password
);

public record RefreshTokenRequest(
    [Required(ErrorMessage = "Informe o refresh token")]
    string RefreshToken
);

public record LogoutRequest(
    string? RefreshToken
);
