using Crm.Application.Abstractions;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Identity;
using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Persistence.Interceptors;
using Crm.Infrastructure.Persistence.Seed;
using Crm.Infrastructure.Workflow;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Crm.Application.Tests;

public class WorkflowEngineTests
{
    [Fact]
    public async Task Illegal_transition_is_rejected()
    {
        var (engine, db, opp) = await SetupAsync();
        var act = async () => await engine.ChangeStatusAsync(opp, SeedIds.StContractSigned, null, [RoleCodes.Admin], CancellationToken.None);
        await act.Should().ThrowAsync<Crm.Application.Common.ConflictException>();
    }

    [Fact]
    public async Task Hold_requires_reason_and_moves_to_hold()
    {
        var (engine, db, opp) = await SetupAsync();
        await engine.HoldAsync(opp, "Waiting on customer", CancellationToken.None);
        opp.StatusId.Should().Be(SeedIds.StHold);
        await engine.ResumeAsync(opp, CancellationToken.None);
        opp.StatusId.Should().Be(SeedIds.StAwaitingAssessment);
    }

    private static async Task<(WorkflowEngine engine, AppDbContext db, Opportunity opp)> SetupAsync()
    {
        var clock = new SystemDateTime();
        var user = new TestUser();
        var interceptor = new AuditSaveChangesInterceptor(user, clock);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options, interceptor, user, clock);
        LookupSeeder.Seed(null, db);
        await db.SaveChangesAsync();
        var opp = new Opportunity
        {
            Name = "Test",
            OpportunityNumber = "OPP-2026-00999",
            CustomerId = Guid.NewGuid(),
            StageId = SeedIds.StageQualification,
            StatusId = SeedIds.StAwaitingAssessment,
            SubmittedByUserId = Guid.NewGuid(),
            ExpectedValueSar = 1
        };
        db.Opportunities.Add(opp);
        await db.SaveChangesAsync();
        var engine = new WorkflowEngine(db, clock, user, interceptor, new NoEmail(), new NoNotify());
        return (engine, db, opp);
    }

    private sealed class TestUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public string? Email => "test@crm.local";
        public string? DisplayName => "Test";
        public IReadOnlyList<string> Roles => [RoleCodes.Admin];
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "test";
        public bool IsAuthenticated => true;
        public bool IsAdmin => true;
        public bool HasRole(string roleCode) => true;
    }

    private sealed class NoEmail : IEmailComposer
    {
        public Task<GeneratedEmail> ComposeAsync(string templateCode, Opportunity opportunity, Guid generatedByUserId, IDictionary<string, string>? extraTokens = null, CancellationToken ct = default)
            => Task.FromResult(new GeneratedEmail { TemplateCode = templateCode, OpportunityId = opportunity.Id });
    }

    private sealed class NoNotify : INotificationPublisher
    {
        public Task PublishAsync(Guid userId, NotificationType type, NotificationContent content, string? linkUrl, Guid? opportunityId, CancellationToken ct = default) => Task.CompletedTask;
        public Task PublishToRolesAsync(string roleCodes, NotificationType type, NotificationContent content, string? linkUrl, Guid? opportunityId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
