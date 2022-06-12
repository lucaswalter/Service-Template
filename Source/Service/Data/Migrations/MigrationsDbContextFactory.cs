using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Service.Data.Persistence;

namespace Service.Data.Migrations;

public class MigrationsDbContextFactory : IDesignTimeDbContextFactory<ServiceDbContext>
{
    public ServiceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ServiceDbContext>();

        options.UseNpgsql("localhost", npgsql =>
        {
            npgsql.SetPostgresVersion(14, 3);
            npgsql.UseNodaTime();
        });

        return new ServiceDbContext(options.Options, SystemClock.Instance);
    }
}
