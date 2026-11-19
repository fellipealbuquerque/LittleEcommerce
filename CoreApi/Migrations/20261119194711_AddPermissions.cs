using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LittleEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    group = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "code", "created_at", "description", "group", "updated_at" },
                values: new object[,]
                {
                    { new Guid("019a9c00-0000-7000-8001-000000000001"), "users.read", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Listar e consultar usuários", "Usuários", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("019a9c00-0000-7000-8001-000000000002"), "users.manage", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Trocar papel, bloquear, desbloquear e encerrar sessões de usuários", "Usuários", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("019a9c00-0000-7000-8001-000000000003"), "roles.read", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Listar papéis e permissões", "Papéis", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("019a9c00-0000-7000-8001-000000000004"), "roles.manage", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Criar, editar permissões e remover papéis", "Papéis", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("019a9c00-0000-7000-8001-000000000005"), "audit.read", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Consultar o log de auditoria", "Auditoria", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("019a9c00-0000-7000-8000-000000000001"),
                column: "is_system",
                value: true);

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("019a9c00-0000-7000-8000-000000000002"),
                column: "is_system",
                value: true);

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("019a9c00-0000-7000-8000-000000000003"),
                column: "is_system",
                value: true);

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "permission_id", "role_id" },
                values: new object[] { new Guid("019a9c00-0000-7000-8001-000000000001"), new Guid("019a9c00-0000-7000-8000-000000000002") });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropColumn(
                name: "is_system",
                table: "roles");
        }
    }
}
