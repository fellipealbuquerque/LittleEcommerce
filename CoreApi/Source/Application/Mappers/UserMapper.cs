using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Shared.DTOs.Results.Users;

namespace LittleEcommerce.Source.Application.Mappers;

public static class UserMapper
{
    public static UserResult ToResult(this User user) =>
        new(user.Id, user.Name, user.Email, user.Role.Name, user.Status, user.CreatedAt, user.LastLoginAt);
}
