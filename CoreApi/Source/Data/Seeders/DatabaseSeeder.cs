using LittleEcommerce.Source.Application.Interfaces;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.Helpers;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Options;

namespace LittleEcommerce.Source.Data.Seeders;

public class DatabaseSeeder(
    IOptions<SeedOptions> options,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    ILogger<DatabaseSeeder> logger) : IDatabaseSeeder
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var seed = options.Value;

        if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            logger.LogInformation("Seed do administrador ignorado: Seed:AdminEmail ou Seed:AdminPassword vazio");
            return;
        }

        var normalizedEmail = EmailNormalizer.Normalize(seed.AdminEmail);

        if (await users.ExistsByEmailAsync(normalizedEmail, ct))
            return;

        users.Add(new User
        {
            Name = seed.AdminName,
            Email = seed.AdminEmail.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = hasher.Hash(seed.AdminPassword),
            RoleId = Roles.AdminId
        });

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Administrador inicial {Email} criado", seed.AdminEmail);
    }
}
