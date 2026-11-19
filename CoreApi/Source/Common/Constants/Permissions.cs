namespace LittleEcommerce.Source.Common.Constants;

public record PermissionDefinition(Guid Id, string Code, string Group, string Description);

public static class Permissions
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";
    public const string AuditRead = "audit.read";
    public const string OutboxRead = "outbox.read";
    public const string OutboxManage = "outbox.manage";

    public static readonly IReadOnlyList<PermissionDefinition> Catalog =
    [
        new(Guid.Parse("019a9c00-0000-7000-8001-000000000001"), UsersRead, "Usuários", "Listar e consultar usuários"),
        new(Guid.Parse("019a9c00-0000-7000-8001-000000000002"), UsersManage, "Usuários", "Trocar papel, bloquear, desbloquear e encerrar sessões de usuários"),
        new(Guid.Parse("019a9c00-0000-7000-8001-000000000003"), RolesRead, "Papéis", "Listar papéis e permissões"),
        new(Guid.Parse("019a9c00-0000-7000-8001-000000000004"), RolesManage, "Papéis", "Criar, editar permissões e remover papéis"),
        new(Guid.Parse("019a9c00-0000-7000-8001-000000000005"), AuditRead, "Auditoria", "Consultar o log de auditoria"),
        new(Guid.Parse("019a9c00-0000-7000-8001-000000000006"), OutboxRead, "Mensageria", "Consultar mensagens do outbox (e-mails e eventos)"),
        new(Guid.Parse("019a9c00-0000-7000-8001-000000000007"), OutboxManage, "Mensageria", "Reenviar mensagens do outbox")
    ];

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultsByRole = new Dictionary<string, IReadOnlyList<string>>
    {
        [Roles.Manager] = [UsersRead]
    };

    public static PermissionDefinition Get(string code) => Catalog.Single(p => p.Code == code);
}
