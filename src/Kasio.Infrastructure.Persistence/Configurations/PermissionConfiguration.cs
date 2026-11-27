using Kasio.Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kasio.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("PermissionID")
            .HasConversion(id => id.Value, value => new PermissionId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.Name)
            .HasColumnName("PermissionName")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Group)
            .HasColumnName("PermissionGroup")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.IsSensitive)
            .HasColumnName("IsSensitive")
            .IsRequired();

        builder.HasIndex(p => p.Name).IsUnique();
        builder.HasIndex(p => p.Group);

        builder.Ignore(p => p.DomainEvents);
    }
}
