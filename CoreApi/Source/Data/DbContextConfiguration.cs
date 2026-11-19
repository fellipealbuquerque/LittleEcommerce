using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LittleEcommerce.Source.Data;

public static class DbContextConfiguration
{
    public static DbContextOptionsBuilder UseLittleEcommerceDatabase(this DbContextOptionsBuilder builder, NpgsqlDataSource dataSource) =>
        builder
            .UseNpgsql(dataSource, npgsql => npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null))
            .UseSnakeCaseNamingConvention();
}
