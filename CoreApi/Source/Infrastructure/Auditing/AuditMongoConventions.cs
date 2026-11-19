using MongoDB.Bson.Serialization.Conventions;

namespace LittleEcommerce.Source.Infrastructure.Auditing;

public static class AuditMongoConventions
{
    private static int _registered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
            return;

        ConventionRegistry.Register(
            "little-ecommerce-audit",
            new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreIfNullConvention(true) },
            type => type.Namespace == typeof(AuditEntry).Namespace);
    }
}
