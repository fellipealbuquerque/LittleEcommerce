using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LittleEcommerce.Source.Infrastructure.Security;

public static class JwtKeys
{
    public static SymmetricSecurityKey SigningKey(string secret) => new(Encoding.UTF8.GetBytes(secret));
}
