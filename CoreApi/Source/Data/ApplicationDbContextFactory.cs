using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace LittleEcommerce.Source.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string LocalConnection = "Host=localhost;Port=5432;Database=little_ecommerce;Username=postgres;Password=postgres";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL") ?? LocalConnection;
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseLittleEcommerceDatabase(NpgsqlDataSource.Create(connection));

        return new ApplicationDbContext(builder.Options);
    }
}
