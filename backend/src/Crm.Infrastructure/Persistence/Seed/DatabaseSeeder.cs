using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Persistence.Seed;

public sealed class DatabaseSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(AppDbContext db, ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (_db.Database.IsRelational())
            await _db.Database.MigrateAsync(ct);
        else
            await _db.Database.EnsureCreatedAsync(ct);

        LookupSeeder.Seed(null, _db);
        DemoSeeder.SeedUsersAndLookups(_db);
        await _db.SaveChangesAsync(ct);
        DemoSeeder.SeedOpportunities(_db);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Database seeded (idempotent).");
    }
}
