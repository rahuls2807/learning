using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class MigrationDiscoveryTests
{
    [Fact]
    public void Platform_settings_migration_is_discovered_by_ef_core()
    {
        var options = new DbContextOptionsBuilder<WorkerBookingContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MigrationDiscoveryTest;Trusted_Connection=True")
            .Options;

        using var context = new WorkerBookingContext(options);

        Assert.Contains("20260606120000_AddPlatformSettings", context.Database.GetMigrations());
    }
}
