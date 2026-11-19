using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Infrastructure.Auditing;
using LittleEcommerce.UnitTests.Support;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LittleEcommerce.UnitTests.Auditing;

public class EntityChangeCollectorTests
{
    private static readonly AuditActor Actor = new() { Type = AuditActorTypes.User, UserId = "admin-1" };
    private static readonly DateTime Now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    private static ApplicationDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseLittleEcommerceDatabase(NpgsqlDataSource.Create("Host=localhost;Database=unit"));
        return new ApplicationDbContext(builder.Options);
    }

    private static User Attached(ApplicationDbContext context)
    {
        var user = TestUsers.Create();
        user.Role = null!;
        context.Users.Attach(user);
        return user;
    }

    [Fact]
    public void Collect_ShouldRecordCreationWithMaskedSecrets()
    {
        using var context = CreateContext();
        var user = TestUsers.Create();
        user.Role = null!;
        context.Users.Add(user);

        var entry = EntityChangeCollector.Collect(context, Actor, "corr-1", Now).Single();

        Assert.Equal(AuditKinds.EntityChange, entry.Kind);
        Assert.Equal(EntityChangeCollector.Created, entry.Action);
        Assert.Equal(nameof(User), entry.EntityType);
        Assert.Equal(user.Id.ToString(), entry.EntityId);
        Assert.Equal("ana@loja.com", entry.Changes!["email"]["new"].AsString);
        Assert.Equal("***", entry.Changes["passwordHash"]["new"].AsString);
        Assert.False(entry.Changes.Contains("createdAt"));
        Assert.Equal("corr-1", entry.CorrelationId);
        Assert.Same(Actor, entry.Actor);
    }

    [Fact]
    public void Collect_ShouldRecordOnlyChangedFieldsWithOldAndNew()
    {
        using var context = CreateContext();
        var user = Attached(context);

        user.Status = UserStatus.Blocked;
        user.UpdatedAt = Now;
        context.ChangeTracker.DetectChanges();

        var entry = EntityChangeCollector.Collect(context, Actor, null, Now).Single();

        Assert.Equal(EntityChangeCollector.Updated, entry.Action);
        Assert.Equal(1, entry.Changes!.ElementCount);
        Assert.Equal("Active", entry.Changes["status"]["old"].AsString);
        Assert.Equal("Blocked", entry.Changes["status"]["new"].AsString);
    }

    [Fact]
    public void Collect_ShouldSkipEntries_WhenOnlyTimestampsChanged()
    {
        using var context = CreateContext();
        var user = Attached(context);

        user.UpdatedAt = Now;
        context.ChangeTracker.DetectChanges();

        Assert.Empty(EntityChangeCollector.Collect(context, Actor, null, Now));
    }

    [Fact]
    public void Collect_ShouldReportSoftDeleteAsDeleted()
    {
        using var context = CreateContext();
        var user = Attached(context);

        user.DeletedAt = Now;
        context.ChangeTracker.DetectChanges();

        var entry = EntityChangeCollector.Collect(context, Actor, null, Now).Single();

        Assert.Equal(EntityChangeCollector.Deleted, entry.Action);
    }

    [Fact]
    public void Collect_ShouldIgnoreEntitiesMarkedAsNotAudited()
    {
        using var context = CreateContext();
        context.RefreshTokens.Add(new RefreshToken { UserId = Guid.NewGuid(), TokenHash = "hash", FamilyId = Guid.NewGuid() });

        Assert.Empty(EntityChangeCollector.Collect(context, Actor, null, Now));
    }

    [Fact]
    public void Collect_ShouldUseCompositeKeyForJoinEntities()
    {
        using var context = CreateContext();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });

        var entry = EntityChangeCollector.Collect(context, Actor, null, Now).Single();

        Assert.Equal($"{roleId}:{permissionId}", entry.EntityId);
    }
}
