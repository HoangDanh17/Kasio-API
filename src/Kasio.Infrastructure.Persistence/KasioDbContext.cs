using Kasio.Domain.Permissions;
using Kasio.Domain.Roles;
using Kasio.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Kasio.Infrastructure.Persistence;

public class KasioDbContext(DbContextOptions<KasioDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KasioDbContext).Assembly);
    }
}
