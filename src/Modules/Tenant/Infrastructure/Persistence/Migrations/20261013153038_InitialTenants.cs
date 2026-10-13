using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BazarKav.Tenant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tenant");

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "tenant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    suspension_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                    table.CheckConstraint("ck_tenants_code", "code ~ '^[a-z][a-z0-9-]{0,62}[a-z0-9]$'");
                    table.CheckConstraint("ck_tenants_name", "length(btrim(name)) BETWEEN 1 AND 200");
                    table.CheckConstraint("ck_tenants_reason", "(status = 2 AND suspension_reason IS NOT NULL AND length(btrim(suspension_reason)) BETWEEN 1 AND 500) OR (status <> 2 AND suspension_reason IS NULL)");
                    table.CheckConstraint("ck_tenants_status", "status IN (0, 1, 2)");
                    table.CheckConstraint("ck_tenants_time", "updated_at >= created_at");
                    table.CheckConstraint("ck_tenants_version", "version > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenants_created_id",
                schema: "tenant",
                table: "tenants",
                columns: new[] { "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_tenants_code",
                schema: "tenant",
                table: "tenants",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenants",
                schema: "tenant");
        }
    }
}
