using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DjMixOrganizer.Data;

// `dotnet ef` (migrations, database update, etc.) needs to construct a
// DjMixDbContext at design time, but there's no running MAUI app around to
// hand it one through DI. Implementing this interface — found by naming
// convention, not registered anywhere — tells the tool how to build one
// itself. Only used by the CLI tooling; the real app never touches this.
public class DjMixDbContextFactory : IDesignTimeDbContextFactory<DjMixDbContext>
{
    public DjMixDbContext CreateDbContext(string[] args)
    {
        var connectionString = DjMixConnectionString.FromEnvironment();

        var optionsBuilder = new DbContextOptionsBuilder<DjMixDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 46)));

        return new DjMixDbContext(optionsBuilder.Options);
    }
}
