using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Options;

namespace LittleEcommerce.Source.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int MinimumWorkFactor = 4;

    private readonly int _workFactor;
    private readonly Lazy<string> _dummyHash;

    public BCryptPasswordHasher(IOptions<PasswordHashingOptions> options)
    {
        _workFactor = Math.Max(MinimumWorkFactor, options.Value.WorkFactor);
        _dummyHash = new Lazy<string>(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), _workFactor));
    }

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, _workFactor);

    public bool Verify(string password, string? hash)
    {
        if (string.IsNullOrEmpty(hash))
        {
            BCrypt.Net.BCrypt.Verify(password, _dummyHash.Value);
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
