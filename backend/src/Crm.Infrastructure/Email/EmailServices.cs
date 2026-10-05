using System.Net;
using System.Text.RegularExpressions;
using Crm.Application.Abstractions;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Email;

public sealed class RazorEmailComposer : IEmailComposer
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;
    private readonly IAuditWriter _audit;
    private readonly INotificationPublisher _notifications;

    public RazorEmailComposer(IApplicationDbContext db, IDateTime clock, IAuditWriter audit, INotificationPublisher notifications)
    {
        _db = db;
        _clock = clock;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<GeneratedEmail> ComposeAsync(
        string templateCode,
        Opportunity opportunity,
        Guid generatedByUserId,
        IDictionary<string, string>? extraTokens = null,
        CancellationToken ct = default)
    {
        var template = await _db.EmailTemplates.FirstOrDefaultAsync(t => t.Code == templateCode && t.IsActive, ct)
            ?? throw new Application.Common.NotFoundException(nameof(EmailTemplate), templateCode);

        if (opportunity.Customer is null)
            await _db.Customers.Where(c => c.Id == opportunity.CustomerId).LoadAsync(ct);
        if (opportunity.SubmittedByUser is null)
            await _db.Users.Where(u => u.Id == opportunity.SubmittedByUserId).LoadAsync(ct);
        if (opportunity.BuilderUser is null && opportunity.BuilderUserId is not null)
            await _db.Users.Where(u => u.Id == opportunity.BuilderUserId).LoadAsync(ct);
        if (opportunity.ScopeOfWork is null)
            await _db.ScopesOfWork.Include(s => s.Items).ThenInclude(i => i.ServiceLine)
                .Where(s => s.OpportunityId == opportunity.Id).LoadAsync(ct);

        var customer = opportunity.Customer ?? await _db.Customers.FindAsync([opportunity.CustomerId], ct);
        var am = opportunity.SubmittedByUser ?? await _db.Users.FindAsync([opportunity.SubmittedByUserId], ct);
        var builder = opportunity.BuilderUserId is Guid bid
            ? opportunity.BuilderUser ?? await _db.Users.FindAsync([bid], ct)
            : null;
        var pricing = await _db.ProposalPricings.Where(p => p.OpportunityId == opportunity.Id && p.IsCurrent).FirstOrDefaultAsync(ct);
        var scope = opportunity.ScopeOfWork ?? await _db.ScopesOfWork.Include(s => s.Items).ThenInclude(i => i.ServiceLine)
            .FirstOrDefaultAsync(s => s.OpportunityId == opportunity.Id, ct);

        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["OpportunityName"] = opportunity.Name,
            ["OpportunityNumber"] = opportunity.OpportunityNumber,
            ["CustomerName"] = customer?.NameEn ?? "",
            ["SourceChannel"] = opportunity.SourceChannel.ToString(),
            ["SubmissionDate"] = opportunity.CreatedAtUtc.ToString("u"),
            ["SubmissionTheme"] = opportunity.SubmissionTheme.ToString(),
            ["EngagementType"] = opportunity.EngagementType.ToString(),
            ["ExpectedValueSar"] = opportunity.ExpectedValueSar.ToString("N2"),
            ["OpportunityType"] = opportunity.OpportunityType.ToString(),
            ["RelationWithClientScore"] = opportunity.RelationWithClientScore.ToString(),
            ["WinProbabilityScore"] = opportunity.WinProbabilityScore.ToString(),
            ["DurationMonths"] = opportunity.DurationMonths?.ToString() ?? "—",
            ["ProposalLanguage"] = opportunity.ProposalLanguage.ToString(),
            ["QualificationDeadline"] = Fmt(opportunity.Deadlines.QualificationDeadline),
            ["InquiriesDeadline"] = Fmt(opportunity.Deadlines.InquiriesDeadline),
            ["EstimatedCostDeadline"] = Fmt(opportunity.Deadlines.EstimatedCostDeadline),
            ["InternalDeadline"] = Fmt(opportunity.Deadlines.InternalDeadline),
            ["SubmissionDeadline"] = Fmt(opportunity.Deadlines.SubmissionDeadline),
            ["SubmittingAmName"] = am?.DisplayName ?? "",
            ["SubmittingAmEmail"] = am?.Email ?? "",
            ["BuilderEmail"] = builder?.Email ?? "",
            ["ApproverEmail"] = "mgmt.fahad@crm.local",
            ["SlOrPresalesEmail"] = "sl.noura@crm.local",
            ["SlRepresentatives"] = "sl.noura@crm.local; sl.yousef@crm.local",
            ["ScopeBrief"] = scope?.Brief ?? "",
            ["ScopeOfWorkHtml"] = BuildScopeHtml(scope),
            ["PriceSar"] = pricing?.PriceSar.ToString("N2") ?? "",
            ["MarginPercent"] = pricing?.MarginPercent.ToString("N2") ?? "",
            ["RejectionReasonsHtml"] = ""
        };
        if (extraTokens is not null)
            foreach (var kv in extraTokens) tokens[kv.Key] = kv.Value;

        string Resolve(string input)
        {
            return Regex.Replace(input, @"\{\{(\w+)\}\}", m =>
                tokens.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
        }

        var email = new GeneratedEmail
        {
            Id = Guid.NewGuid(),
            OpportunityId = opportunity.Id,
            TemplateCode = templateCode,
            To = Resolve(template.DefaultTo),
            Cc = Resolve(template.DefaultCc),
            Subject = Resolve(template.SubjectTemplate),
            BodyHtml = Resolve(template.BodyTemplateHtml),
            Status = GeneratedEmailStatus.Draft,
            GeneratedAtUtc = _clock.UtcNow,
            GeneratedByUserId = generatedByUserId
        };
        _db.GeneratedEmails.Add(email);
        _audit.Add("GeneratedEmail", email.Id, AuditAction.EmailGenerated, $"Draft {templateCode}", opportunity.Id);

        if (am is not null)
        {
            await _notifications.PublishAsync(am.Id, NotificationType.EmailGenerated,
                NotificationContents.EmailDraft(email.Subject, opportunity.OpportunityNumber),
                $"/opportunities/{opportunity.Id}/emails", opportunity.Id, ct);
        }

        return email;
    }

    private static string Fmt(DateTime? d) => d?.ToString("u") ?? "—";

    private static string BuildScopeHtml(ScopeOfWork? scope)
    {
        if (scope is null) return "";
        var rows = scope.Items.OrderBy(i => i.SortOrder).Select(i =>
            $"<tr><td>{WebUtility.HtmlEncode(i.Title)}</td><td>{WebUtility.HtmlEncode(i.ServiceLine?.NameEn)}</td><td>{WebUtility.HtmlEncode(i.Comment)}</td></tr>");
        return $"<h3>Scope of Work</h3><p>{WebUtility.HtmlEncode(scope.Brief)}</p><table border='1' cellpadding='6'><tr><th>Scope Item</th><th>Service Line</th><th>Comment</th></tr>{string.Join("", rows)}</table>";
    }
}

public sealed class NullEmailSender : IEmailSender
{
    private readonly ILogger<NullEmailSender> _logger;
    public NullEmailSender(ILogger<NullEmailSender> logger) => _logger = logger;
    public Task SendAsync(GeneratedEmail email, CancellationToken ct = default)
    {
        _logger.LogInformation("Email sending disabled. Draft {Id} to {To} subject {Subject}", email.Id, email.To, email.Subject);
        return Task.CompletedTask;
    }
}

public sealed class SmtpEmailSender : IEmailSender
{
    public Task SendAsync(GeneratedEmail email, CancellationToken ct = default)
        => throw new NotSupportedException("SMTP sending is not wired. Enable Email:SendingEnabled only after configuring SMTP.");
}

public sealed class NotificationPublisher : INotificationPublisher
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public NotificationPublisher(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public Task PublishAsync(Guid userId, NotificationType type, NotificationContent content, string? linkUrl, Guid? opportunityId, CancellationToken ct = default)
    {
        _db.Notifications.Add(new AppNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = content.TitleEn,
            Body = content.BodyEn,
            MessageKey = content.Key,
            ParamsJson = System.Text.Json.JsonSerializer.Serialize(content.Params),
            LinkUrl = linkUrl,
            OpportunityId = opportunityId,
            CreatedAtUtc = _clock.UtcNow
        });
        return Task.CompletedTask;
    }

    public async Task PublishToRolesAsync(string roleCodes, NotificationType type, NotificationContent content, string? linkUrl, Guid? opportunityId, CancellationToken ct = default)
    {
        var codes = roleCodes.Split(['|', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var userIds = await _db.UserRoles.Where(ur => codes.Contains(ur.Role.Code) && ur.User.IsActive)
            .Select(ur => ur.UserId).Distinct().ToListAsync(ct);
        foreach (var id in userIds)
            await PublishAsync(id, type, content, linkUrl, opportunityId, ct);
    }
}
