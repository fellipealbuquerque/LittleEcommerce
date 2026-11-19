namespace LittleEcommerce.Source.Data.Seeders;

public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
