using Crm.Application.Abstractions;
using Crm.Infrastructure.Email;
using Crm.Infrastructure.Excel;
using Crm.Infrastructure.Files;
using Crm.Infrastructure.Identity;
using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Persistence.Interceptors;
using Crm.Infrastructure.Persistence.Seed;
using Crm.Infrastructure.Security;
using Crm.Infrastructure.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Crm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<IAuditWriter>(sp => sp.GetRequiredService<AuditSaveChangesInterceptor>());
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IDateTime, SystemDateTime>();

        var cs = config.GetConnectionString("DefaultConnection")
                 ?? "Server=(localdb)\\mssqllocaldb;Database=CrmDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true";

        // Dev fallback when SQL Server / LocalDB / Docker is unavailable.
        var useInMemory = cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase)
                          || cs.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase)
                          || config.GetValue("Database:UseInMemory", false);
        if (useInMemory)
            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("CrmDb"));
        else
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(cs));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IFileStorage, LocalDiskFileStorage>();
        services.AddScoped<IEmailComposer, RazorEmailComposer>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();
        services.AddScoped<INumberGenerator, NumberGenerator>();
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        services.AddScoped<IOpportunityVisibility, OpportunityVisibility>();
        services.AddSingleton<IPermissionStore, PermissionStore>();
        services.AddSingleton<IExcelExporter, Crm.Infrastructure.Excel.ExcelExporter>();

        var sending = config.GetValue("Email:SendingEnabled", false);
        if (sending)
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, NullEmailSender>();

        return services;
    }
}
