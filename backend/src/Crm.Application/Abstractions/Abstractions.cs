using Crm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Crm.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerContact> CustomerContacts { get; }
    DbSet<ServiceLine> ServiceLines { get; }
    DbSet<Stage> Stages { get; }
    DbSet<Status> Statuses { get; }
    DbSet<StatusTransition> StatusTransitions { get; }
    DbSet<WorkflowGate> WorkflowGates { get; }
    DbSet<Opportunity> Opportunities { get; }
    DbSet<ScopeOfWork> ScopesOfWork { get; }
    DbSet<ScopeItem> ScopeItems { get; }
    DbSet<GateInstance> GateInstances { get; }
    DbSet<Note> Notes { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AppNotification> Notifications { get; }
    DbSet<EstimatedCost> EstimatedCosts { get; }
    DbSet<ProposalPricing> ProposalPricings { get; }
    DbSet<BidBond> BidBonds { get; }
    DbSet<QualificationMeeting> QualificationMeetings { get; }
    DbSet<QualificationMeetingAttendee> QualificationMeetingAttendees { get; }
    DbSet<SlResponse> SlResponses { get; }
    DbSet<Submission> Submissions { get; }
    DbSet<OpportunityOutcome> OpportunityOutcomes { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<ContractNegotiationRound> ContractNegotiationRounds { get; }
    DbSet<ContractMilestone> ContractMilestones { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<GeneratedEmail> GeneratedEmails { get; }
    DbSet<OpportunityNumberSequence> OpportunityNumberSequences { get; }
    DbSet<ContractNumberSequence> ContractNumberSequences { get; }
    DbSet<Team> Teams { get; }
    DbSet<TeamMember> TeamMembers { get; }
    DbSet<UserServiceLine> UserServiceLines { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<SavedView> SavedViews { get; }
    DbSet<ExportTemplate> ExportTemplates { get; }
    DbSet<Activity> Activities { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    IReadOnlyList<string> Roles { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
    bool HasRole(string roleCode);
    bool IsAdmin { get; }
}

public interface IDateTime
{
    DateTime UtcNow { get; }
}

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken ct = default);
    Task DeleteAsync(string storedFileName, CancellationToken ct = default);
}

public sealed record StoredFile(string StoredFileName, string OriginalFileName, string ContentType, long SizeBytes);

public interface IEmailComposer
{
    Task<GeneratedEmail> ComposeAsync(string templateCode, Opportunity opportunity, Guid generatedByUserId, IDictionary<string, string>? extraTokens = null, CancellationToken ct = default);
}

public interface IEmailSender
{
    Task SendAsync(GeneratedEmail email, CancellationToken ct = default);
}

/// <summary>
/// Localizable notification payload. <see cref="Key"/> maps to i18n entries notif.{Key}.title / notif.{Key}.body
/// on the client; <see cref="TitleEn"/>/<see cref="BodyEn"/> are the server-side English fallback.
/// </summary>
public sealed record NotificationContent(string Key, string TitleEn, string BodyEn, IReadOnlyDictionary<string, string> Params);

public static class NotificationContents
{
    private static IReadOnlyDictionary<string, string> P(params (string K, string V)[] pairs) =>
        pairs.ToDictionary(p => p.K, p => p.V);

    public static NotificationContent GateAssigned(string gateNameEn, string oppNumber, string oppName) =>
        new("gateAssigned", $"{gateNameEn} pending", $"{oppNumber} — {oppName}",
            P(("gate", gateNameEn), ("number", oppNumber), ("name", oppName)));

    public static NotificationContent BuilderAssigned(string oppNumber, string oppName) =>
        new("builderAssigned", "You have been assigned as builder", $"{oppNumber} — {oppName}",
            P(("number", oppNumber), ("name", oppName)));

    public static NotificationContent EmailDraft(string subject, string oppNumber) =>
        new("emailDraft", subject, "A draft email was generated and is ready to copy or send.",
            P(("subject", subject), ("number", oppNumber)));

    public static NotificationContent StatusChanged(string oppNumber, string oppName, string statusEn) =>
        new("statusChanged", $"{oppNumber} is now {statusEn}", oppName,
            P(("number", oppNumber), ("name", oppName), ("status", statusEn)));

    public static NotificationContent GateDecided(string gateNameEn, string decision, string oppNumber, string oppName) =>
        new("gateDecided", $"{gateNameEn}: {decision}", $"{oppNumber} — {oppName}",
            P(("gate", gateNameEn), ("decision", decision), ("number", oppNumber), ("name", oppName)));
}

public interface INotificationPublisher
{
    Task PublishAsync(Guid userId, Domain.Enums.NotificationType type, NotificationContent content, string? linkUrl, Guid? opportunityId, CancellationToken ct = default);
    /// <summary>Publishes to every active user holding any of the given role codes (comma or pipe separated).</summary>
    Task PublishToRolesAsync(string roleCodes, Domain.Enums.NotificationType type, NotificationContent content, string? linkUrl, Guid? opportunityId, CancellationToken ct = default);
}

public interface IJwtTokenService
{
    (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(User user, IEnumerable<string> roles);
    string CreateRefreshToken();
    string HashToken(string token);
}

public interface IWorkflowEngine
{
    Task<GateInstance> OpenGateAsync(Opportunity opportunity, string gateCode, Guid? assignedUserId = null, int? round = null, CancellationToken ct = default);

    Task ApplyGateDecisionAsync(
        GateInstance instance,
        string decision,
        string? reason,
        Guid actorUserId,
        IReadOnlyList<string> actorRoles,
        CancellationToken ct = default);

    Task<IReadOnlyList<Status>> GetAvailableTransitionsAsync(
        Opportunity opportunity,
        IReadOnlyList<string> roleCodes,
        CancellationToken ct = default);

    Task ChangeStatusAsync(
        Opportunity opportunity,
        Guid toStatusId,
        string? reason,
        IReadOnlyList<string> roleCodes,
        CancellationToken ct = default);

    Task HoldAsync(Opportunity opportunity, string reason, CancellationToken ct = default);
    Task ResumeAsync(Opportunity opportunity, CancellationToken ct = default);
    Task CancelAsync(Opportunity opportunity, string reason, IReadOnlyList<string> roleCodes, CancellationToken ct = default);

    Task AssignBuilderAsync(Opportunity opportunity, Domain.Enums.BuilderType builderType, Guid builderUserId, CancellationToken ct = default);
}

public interface INumberGenerator
{
    Task<string> NextOpportunityNumberAsync(CancellationToken ct = default);
    Task<string> NextContractNumberAsync(CancellationToken ct = default);
}

public interface IAuditWriter
{
    void Add(
        string entityType,
        Guid entityId,
        Domain.Enums.AuditAction action,
        string description,
        Guid? opportunityId = null,
        string? fromValue = null,
        string? toValue = null,
        string? metadataJson = null);
}

public interface IExcelExporter
{
    byte[] Export<T>(string sheetName, IReadOnlyList<(string Header, Func<T, object?> Value)> columns, IEnumerable<T> rows);
}
