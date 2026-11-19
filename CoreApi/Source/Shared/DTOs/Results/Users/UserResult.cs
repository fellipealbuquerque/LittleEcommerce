using LittleEcommerce.Source.Common.Enums;

namespace LittleEcommerce.Source.Shared.DTOs.Results.Users;

public record UserResult(
    Guid Id,
    string Name,
    string Email,
    string Role,
    UserStatus Status,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
