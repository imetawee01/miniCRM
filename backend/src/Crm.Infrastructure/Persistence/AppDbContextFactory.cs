using Crm.Infrastructure.Identity;
using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Crm.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = "Server=(localdb)\\mssqllocaldb;Database=CrmDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs).Options;
        var clock = new SystemDateTime();
        var current = new DesignTimeCurrentUser();
        var interceptor = new AuditSaveChangesInterceptor(current, clock);
        return new AppDbContext(options, interceptor, current, clock);
    }
}

file sealed class DesignTimeCurrentUser : Crm.Application.Abstractions.ICurrentUser
{
    public Guid? UserId => null;
    public string? Email => null;
    public string? DisplayName => null;
    public IReadOnlyList<string> Roles => [];
    public string? IpAddress => null;
    public string? UserAgent => null;
    public bool IsAuthenticated => false;
    public bool IsAdmin => false;
    public bool HasRole(string roleCode) => false;
}
