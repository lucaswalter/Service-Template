using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Service.Data.Domain.Core;

namespace Service.Data.Persistence;

public sealed partial class ServiceDbContext : DbContext
{
    private readonly IClock _clock;

    public ServiceDbContext(DbContextOptions<ServiceDbContext> options, IClock clock) : base(options)
    {
        _clock = clock;
        ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
        ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        var currentInstant = _clock.GetCurrentInstant();

        HandleAdded(currentInstant);
        HandleModified(currentInstant);
        HandleVersioned();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new NotSupportedException("Use async version instead!");
    }

    private void HandleAdded(Instant currentInstant)
    {
        var added = ChangeTracker.Entries<IAuditableModel>().Where(e => e.State == EntityState.Added);

        foreach (var entry in added)
        {
            entry.Property(o => o.CreatedAt).CurrentValue = currentInstant;
            entry.Property(o => o.CreatedAt).IsModified = true;

            entry.Property(o => o.UpdatedAt).CurrentValue = currentInstant;
            entry.Property(o => o.UpdatedAt).IsModified = true;
        }
    }

    private void HandleModified(Instant currentInstant)
    {
        var modified = ChangeTracker.Entries<IAuditableModel>().Where(e => e.State == EntityState.Modified);

        foreach (var entry in modified)
        {
            entry.Property(o => o.UpdatedAt).CurrentValue = currentInstant;
            entry.Property(o => o.UpdatedAt).IsModified = true;

            entry.Property(o => o.CreatedAt).CurrentValue = entry.Property(x => x.UpdatedAt).OriginalValue;
            entry.Property(x => x.CreatedAt).IsModified = false;
        }
    }

    private void HandleVersioned()
    {
        foreach (var versionedModel in ChangeTracker.Entries<IVersionedEntity>())
        {
            var versionProp = versionedModel.Property(o => o.Version);

            switch (versionedModel.State)
            {
                case EntityState.Added:
                    versionProp.CurrentValue = 1;
                    break;

                case EntityState.Modified:
                    versionProp.CurrentValue = versionProp.OriginalValue + 1;
                    versionProp.IsModified = true;
                    break;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return base.SaveChangesAsync(ct);
    }
}
