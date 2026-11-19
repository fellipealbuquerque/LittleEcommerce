using LittleEcommerce.Source.Application.Abstractions;
using LittleEcommerce.Source.Data.Entities;

namespace LittleEcommerce.Source.Application.Interfaces.Security;

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);

    IssuedRefreshToken CreateRefreshToken();

    string HashRefreshToken(string value);
}
