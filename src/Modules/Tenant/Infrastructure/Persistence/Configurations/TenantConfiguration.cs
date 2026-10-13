using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.Tenant.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<TenantEntity>
{
    public const string CodeIndex = "ux_tenants_code";

    public void Configure(EntityTypeBuilder<TenantEntity> builder)
    {
        builder.ToTable("tenants", "tenant", table =>
        {
            table.HasCheckConstraint("ck_tenants_code", "code ~ '^[a-z][a-z0-9-]{0,62}[a-z0-9]$'");
            table.HasCheckConstraint("ck_tenants_name", "length(btrim(name)) BETWEEN 1 AND 200");
            table.HasCheckConstraint("ck_tenants_status", "status IN (0, 1, 2)");
            table.HasCheckConstraint("ck_tenants_reason",
                "(status = 2 AND suspension_reason IS NOT NULL AND length(btrim(suspension_reason)) BETWEEN 1 AND 500) OR (status <> 2 AND suspension_reason IS NULL)");
            table.HasCheckConstraint("ck_tenants_version", "version > 0");
            table.HasCheckConstraint("ck_tenants_time", "updated_at >= created_at");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName(CodeIndex);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.SuspensionReason).HasColumnName("suspension_reason").HasMaxLength(500);
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => new { x.CreatedAt, x.Id }).HasDatabaseName("ix_tenants_created_id");
    }
}
