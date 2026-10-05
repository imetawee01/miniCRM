using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Application.Query;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Opportunities;

/// <summary>Shared filter surface for the list and the export.</summary>
public record OpportunityFilter : PagedQuery
{
    public Guid? StageId { get; set; }
    public Guid? StatusId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? BuilderUserId { get; set; }
    public SubmissionTheme? SubmissionTheme { get; set; }
    public SourceChannel? SourceChannel { get; set; }
    public decimal? ValueFrom { get; set; }
    public decimal? ValueTo { get; set; }
    public DateTime? SubmissionDateFrom { get; set; }
    public DateTime? SubmissionDateTo { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public bool? IsClosed { get; set; }
    public bool MyItemsOnly { get; set; }
    public bool PendingOnMe { get; set; }
    /// <summary>Odoo-style domain JSON array. Combined with other typed filters via AND.</summary>
    public string? Filter { get; set; }
}

public record OpportunityListQuery : OpportunityFilter, IRequest<PagedResult<OpportunityListItemDto>>;

public record ExportOpportunitiesQuery : OpportunityFilter, IRequest<IReadOnlyList<OpportunityListItemDto>>
{
    public const int MaxRows = 50_000;
}

public record GetOpportunityQuery(Guid Id) : IRequest<OpportunityDetailDto>;
public record GetOpportunitySummaryQuery(Guid Id) : IRequest<OpportunitySummaryDto>;
public record GetTimelineQuery(Guid Id) : IRequest<IReadOnlyList<TimelineItemDto>>;
public record GetAvailableTransitionsQuery(Guid Id) : IRequest<IReadOnlyList<AvailableTransitionDto>>;
public record CreateOpportunityCommand : IRequest<OpportunityDetailDto>
{
    public string Name { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public SourceChannel SourceChannel { get; set; }
    public string? SourceChannelOther { get; set; }
    public SubmissionTheme SubmissionTheme { get; set; }
    public EngagementType EngagementType { get; set; }
    public OpportunityType OpportunityType { get; set; }
    public decimal ExpectedValueSar { get; set; }
    public int RelationWithClientScore { get; set; }
    public int WinProbabilityScore { get; set; }
    public int? DurationMonths { get; set; }
    public ProposalLanguage ProposalLanguage { get; set; }
    public bool RequiresBidBond { get; set; }
    public string? ScopeBrief { get; set; }
    public List<CreateScopeItemDto>? ScopeItems { get; set; }
}

public record CreateScopeItemDto(string Title, Guid ServiceLineId, string? Description, string? Comment);

public record UpdateOpportunityCommand : IRequest<OpportunityDetailDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public SourceChannel SourceChannel { get; set; }
    public string? SourceChannelOther { get; set; }
    public SubmissionTheme SubmissionTheme { get; set; }
    public EngagementType EngagementType { get; set; }
    public OpportunityType OpportunityType { get; set; }
    public decimal ExpectedValueSar { get; set; }
    public int RelationWithClientScore { get; set; }
    public int WinProbabilityScore { get; set; }
    public int? DurationMonths { get; set; }
    public ProposalLanguage ProposalLanguage { get; set; }
    public bool RequiresBidBond { get; set; }
    public byte[]? RowVersion { get; set; }
}

public record DeleteOpportunityCommand(Guid Id) : IRequest<Unit>;
public record SetDeadlinesCommand(Guid Id, DateTime? QualificationDeadline, DateTime? InquiriesDeadline, DateTime? EstimatedCostDeadline, DateTime? InternalDeadline, DateTime? SubmissionDeadline) : IRequest<Unit>;
public record SetOwnerCommand(Guid Id, Guid OwnerUserId) : IRequest<Unit>;
public record ChangeStatusCommand(Guid Id, Guid ToStatusId, string? Reason) : IRequest<Unit>;
public record HoldCommand(Guid Id, string Reason) : IRequest<Unit>;
public record ResumeCommand(Guid Id) : IRequest<Unit>;
public record CancelOpportunityCommand(Guid Id, string Reason) : IRequest<Unit>;

public record OpportunityListItemDto
{
    public Guid Id { get; init; }
    public string OpportunityNumber { get; init; } = "";
    public string Name { get; init; } = "";
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public string CustomerNameAr { get; init; } = "";
    public SourceChannel SourceChannel { get; init; }
    public SubmissionTheme SubmissionTheme { get; init; }
    public EngagementType EngagementType { get; init; }
    public OpportunityType OpportunityType { get; init; }
    public decimal ExpectedValueSar { get; init; }
    public Guid StageId { get; init; }
    public string StageCode { get; init; } = "";
    public string StageNameEn { get; init; } = "";
    public string StageNameAr { get; init; } = "";
    public Guid StatusId { get; init; }
    public string StatusCode { get; init; } = "";
    public string StatusNameEn { get; init; } = "";
    public string StatusNameAr { get; init; } = "";
    public bool IsTerminal { get; init; }
    public Guid? OwnerUserId { get; init; }
    public string? OwnerDisplayName { get; init; }
    public Guid? BuilderUserId { get; init; }
    public string? BuilderDisplayName { get; init; }
    public Guid SubmittedByUserId { get; init; }
    public string? SubmittedByDisplayName { get; init; }
    public bool IsClosed { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? SubmissionDeadline { get; init; }
    public DateTime? InternalDeadline { get; init; }
    public string? PendingGateCode { get; init; }
    public string? PendingGateNameEn { get; init; }
    public string? PendingGateNameAr { get; init; }
    public string? PendingRoleCode { get; init; }
    public string? PendingUserName { get; init; }
    public DateTime? PendingSinceUtc { get; init; }
}

public record OpportunitySummaryDto(
    Guid Id, string OpportunityNumber, string Name, string CustomerName, string CustomerNameAr,
    string StageCode, string StageNameEn, string StageNameAr,
    string StatusCode, string StatusNameEn, string StatusNameAr,
    decimal ExpectedValueSar, DateTime? SubmissionDeadline, bool IsClosed);

public record TimelineItemDto(DateTime OccurredAtUtc, string Kind, string Title, string? Detail, string? Actor);

public record StatusDto(Guid Id, string Code, string NameEn, string NameAr, Guid StageId, bool IsTerminal);

/// <summary>Legal next status for the current user, including whether a reason is required.</summary>
public record AvailableTransitionDto(
    Guid ToStatusId,
    string ToStatusCode,
    string ToStatusNameEn,
    string ToStatusNameAr,
    Guid StageId,
    string StageCode,
    bool IsTerminal,
    bool RequiresReason,
    string RequiredRoleCode);

public record PendingOnDto
{
    public Guid GateInstanceId { get; set; }
    public string GateCode { get; set; } = string.Empty;
    public string GateNameEn { get; set; } = string.Empty;
    public string GateNameAr { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string RoleNameEn { get; set; } = string.Empty;
    public string RoleNameAr { get; set; } = string.Empty;
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    /// <summary>Human-readable owner of the next action (user name or role name).</summary>
    public string Label { get; set; } = string.Empty;
    public DateTime OpenedAtUtc { get; set; }
}

public record OpportunityDeadlinesDto
{
    public DateTime? QualificationDeadline { get; set; }
    public DateTime? InquiriesDeadline { get; set; }
    public DateTime? EstimatedCostDeadline { get; set; }
    public DateTime? InternalDeadline { get; set; }
    public DateTime? SubmissionDeadline { get; set; }
}

public record OpportunityDetailDto
{
    public Guid Id { get; set; }
    public string OpportunityNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerNameAr { get; set; } = string.Empty;
    public SourceChannel SourceChannel { get; set; }
    public string? SourceChannelOther { get; set; }
    public SubmissionTheme SubmissionTheme { get; set; }
    public EngagementType EngagementType { get; set; }
    public OpportunityType OpportunityType { get; set; }
    public decimal ExpectedValueSar { get; set; }
    public int RelationWithClientScore { get; set; }
    public int WinProbabilityScore { get; set; }
    public int? DurationMonths { get; set; }
    public ProposalLanguage ProposalLanguage { get; set; }
    public Guid StageId { get; set; }
    public string StageCode { get; set; } = string.Empty;
    public string StageNameEn { get; set; } = string.Empty;
    public string StageNameAr { get; set; } = string.Empty;
    public Guid StatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusNameEn { get; set; } = string.Empty;
    public string StatusNameAr { get; set; } = string.Empty;
    public bool IsTerminal { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public string SubmittedByName { get; set; } = string.Empty;
    public Guid? OwnerUserId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public BuilderType? BuilderType { get; set; }
    public Guid? BuilderUserId { get; set; }
    public string? BuilderDisplayName { get; set; }
    public bool? RequiresQualificationMeeting { get; set; }
    public bool RequiresBidBond { get; set; }
    public bool IsClosed { get; set; }
    public bool IsOnHold { get; set; }
    public string? HoldReason { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public OpportunityDeadlinesDto Deadlines { get; set; } = new();
    public string? ScopeBrief { get; set; }
    public PendingOnDto? PendingOn { get; set; }
}

public sealed class CreateOpportunityValidator : AbstractValidator<CreateOpportunityCommand>
{
    public CreateOpportunityValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ExpectedValueSar).GreaterThan(0);
        RuleFor(x => x.RelationWithClientScore).InclusiveBetween(1, 5);
        RuleFor(x => x.WinProbabilityScore).InclusiveBetween(1, 5);
        RuleFor(x => x.SourceChannelOther).NotEmpty().When(x => x.SourceChannel == SourceChannel.Other);
        RuleFor(x => x.ScopeBrief).NotEmpty().MinimumLength(20).MaximumLength(8000);
        RuleFor(x => x.ScopeItems).NotEmpty().WithMessage("At least one scope item is required.");
        RuleForEach(x => x.ScopeItems).ChildRules(item =>
        {
            item.RuleFor(i => i.Title).NotEmpty().MaximumLength(300);
            item.RuleFor(i => i.ServiceLineId).NotEmpty();
        });
    }
}

public sealed class SetDeadlinesValidator : AbstractValidator<SetDeadlinesCommand>
{
    public SetDeadlinesValidator()
    {
        RuleFor(x => x).Custom((cmd, ctx) =>
        {
            var dates = new DateTime?[] { cmd.QualificationDeadline, cmd.InquiriesDeadline, cmd.EstimatedCostDeadline, cmd.InternalDeadline, cmd.SubmissionDeadline }
                .Where(d => d.HasValue).Select(d => d!.Value).ToList();
            for (var i = 1; i < dates.Count; i++)
            {
                if (dates[i] < dates[i - 1])
                    ctx.AddFailure("Deadlines must be in order: Qualification ≤ Inquiries ≤ Estimated Cost ≤ Internal ≤ Submission.");
            }
        });
    }
}

public sealed class OpportunityHandlers :
    IRequestHandler<OpportunityListQuery, PagedResult<OpportunityListItemDto>>,
    IRequestHandler<ExportOpportunitiesQuery, IReadOnlyList<OpportunityListItemDto>>,
    IRequestHandler<GetOpportunityQuery, OpportunityDetailDto>,
    IRequestHandler<GetOpportunitySummaryQuery, OpportunitySummaryDto>,
    IRequestHandler<CreateOpportunityCommand, OpportunityDetailDto>,
    IRequestHandler<UpdateOpportunityCommand, OpportunityDetailDto>,
    IRequestHandler<DeleteOpportunityCommand, Unit>,
    IRequestHandler<SetDeadlinesCommand, Unit>,
    IRequestHandler<SetOwnerCommand, Unit>,
    IRequestHandler<GetTimelineQuery, IReadOnlyList<TimelineItemDto>>,
    IRequestHandler<GetAvailableTransitionsQuery, IReadOnlyList<AvailableTransitionDto>>,
    IRequestHandler<ChangeStatusCommand, Unit>,
    IRequestHandler<HoldCommand, Unit>,
    IRequestHandler<ResumeCommand, Unit>,
    IRequestHandler<CancelOpportunityCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly INumberGenerator _numbers;
    private readonly IWorkflowEngine _workflow;
    private readonly IEmailComposer _email;
    private readonly IDateTime _clock;
    private readonly IOpportunityVisibility _visibility;

    public OpportunityHandlers(
        IApplicationDbContext db, ICurrentUser user, INumberGenerator numbers,
        IWorkflowEngine workflow, IEmailComposer email, IDateTime clock,
        IOpportunityVisibility visibility)
    {
        _db = db; _user = user; _numbers = numbers; _workflow = workflow; _email = email; _clock = clock;
        _visibility = visibility;
    }

    public async Task<PagedResult<OpportunityListItemDto>> Handle(OpportunityListQuery q, CancellationToken ct)
    {
        var query = BuildListQuery(q);
        var total = await query.CountAsync(ct);
        query = ApplySort(query, q.SortBy, q.SortDir);
        var items = await query.Skip(q.Skip).Take(q.Take).Select(ListProjection).ToListAsync(ct);
        return PagedResult<OpportunityListItemDto>.Create(items, q.Page, q.Take, total);
    }

    /// <summary>Export: every filtered row (hard ceiling to protect the server), same projection as the list.</summary>
    public async Task<IReadOnlyList<OpportunityListItemDto>> Handle(ExportOpportunitiesQuery q, CancellationToken ct)
    {
        var query = ApplySort(BuildListQuery(q), q.SortBy, q.SortDir);
        return await query.Take(ExportOpportunitiesQuery.MaxRows).Select(ListProjection).ToListAsync(ct);
    }

    private IQueryable<Opportunity> BuildListQuery(OpportunityFilter q)
    {
        var query = _visibility.Apply(_db.Opportunities.AsNoTracking());

        if (q.StageId is Guid st) query = query.Where(o => o.StageId == st);
        if (q.StatusId is Guid statusId) query = query.Where(o => o.StatusId == statusId);
        if (q.CustomerId is Guid c) query = query.Where(o => o.CustomerId == c);
        if (q.OwnerUserId is Guid ow) query = query.Where(o => o.OwnerUserId == ow);
        if (q.BuilderUserId is Guid b) query = query.Where(o => o.BuilderUserId == b);
        if (q.SubmissionTheme is SubmissionTheme th) query = query.Where(o => o.SubmissionTheme == th);
        if (q.SourceChannel is SourceChannel sc) query = query.Where(o => o.SourceChannel == sc);
        if (q.ValueFrom is decimal vf) query = query.Where(o => o.ExpectedValueSar >= vf);
        if (q.ValueTo is decimal vt) query = query.Where(o => o.ExpectedValueSar <= vt);
        if (q.SubmissionDateFrom is DateTime df) query = query.Where(o => o.Deadlines.SubmissionDeadline >= df);
        if (q.SubmissionDateTo is DateTime dt) query = query.Where(o => o.Deadlines.SubmissionDeadline <= dt);
        if (q.CreatedFrom is DateTime cf) query = query.Where(o => o.CreatedAtUtc >= cf);
        if (q.CreatedTo is DateTime ctd) query = query.Where(o => o.CreatedAtUtc <= ctd);
        if (q.IsClosed is bool cl) query = query.Where(o => o.IsClosed == cl);
        if (q.MyItemsOnly && _user.UserId is Guid uid)
            query = query.Where(o => o.OwnerUserId == uid || o.BuilderUserId == uid || o.SubmittedByUserId == uid);
        if (q.PendingOnMe && _user.UserId is Guid me)
        {
            // Subquery avoids roles.Any(...Contains...) which InMemory cannot translate
            var pendingForMe = _db.GateInstances.AsNoTracking()
                .Where(g => g.State == GateState.Pending)
                .WhereAssignedTo(me, _user.Roles);
            query = query.Where(o => pendingForMe.Any(g => g.OpportunityId == o.Id));
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(o => o.Name.Contains(s) || o.OpportunityNumber.Contains(s)
                || o.Customer.NameEn.Contains(s) || o.Customer.NameAr.Contains(s)
                || (o.OwnerUser != null && o.OwnerUser.DisplayName.Contains(s))
                || (o.BuilderUser != null && o.BuilderUser.DisplayName.Contains(s))
                || o.Status.NameEn.Contains(s) || o.Status.NameAr.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(q.Filter))
        {
            try
            {
                var nodes = DomainFilterParser.Parse(q.Filter);
                var predicate = new DomainFilterBuilder<Opportunity>(OpportunityMeta.Maps).Build(nodes);
                if (predicate is not null)
                    query = query.Where(predicate);
            }
            catch (ArgumentException ex)
            {
                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    ["filter"] = [ex.Message]
                });
            }
        }

        return query;
    }

    /// <summary>Server-side projection shared by the list and the export.</summary>
    internal static readonly System.Linq.Expressions.Expression<Func<Opportunity, OpportunityListItemDto>> ListProjection = o => new OpportunityListItemDto
    {
        Id = o.Id, OpportunityNumber = o.OpportunityNumber, Name = o.Name,
        CustomerId = o.CustomerId, CustomerName = o.Customer.NameEn, CustomerNameAr = o.Customer.NameAr,
        SourceChannel = o.SourceChannel, SubmissionTheme = o.SubmissionTheme, EngagementType = o.EngagementType, OpportunityType = o.OpportunityType,
        ExpectedValueSar = o.ExpectedValueSar,
        StageId = o.StageId, StageCode = o.Stage.Code, StageNameEn = o.Stage.NameEn, StageNameAr = o.Stage.NameAr,
        StatusId = o.StatusId, StatusCode = o.Status.Code, StatusNameEn = o.Status.NameEn, StatusNameAr = o.Status.NameAr, IsTerminal = o.Status.IsTerminal,
        OwnerUserId = o.OwnerUserId, OwnerDisplayName = o.OwnerUser != null ? o.OwnerUser.DisplayName : null,
        BuilderUserId = o.BuilderUserId, BuilderDisplayName = o.BuilderUser != null ? o.BuilderUser.DisplayName : null,
        SubmittedByUserId = o.SubmittedByUserId, SubmittedByDisplayName = o.SubmittedByUser.DisplayName,
        IsClosed = o.IsClosed, CreatedAtUtc = o.CreatedAtUtc,
        SubmissionDeadline = o.Deadlines.SubmissionDeadline, InternalDeadline = o.Deadlines.InternalDeadline,
        PendingGateCode = o.GateInstances.Where(g => g.State == GateState.Pending).OrderByDescending(g => g.OpenedAtUtc).Select(g => g.Gate.Code).FirstOrDefault(),
        PendingGateNameEn = o.GateInstances.Where(g => g.State == GateState.Pending).OrderByDescending(g => g.OpenedAtUtc).Select(g => g.Gate.NameEn).FirstOrDefault(),
        PendingGateNameAr = o.GateInstances.Where(g => g.State == GateState.Pending).OrderByDescending(g => g.OpenedAtUtc).Select(g => g.Gate.NameAr).FirstOrDefault(),
        PendingRoleCode = o.GateInstances.Where(g => g.State == GateState.Pending).OrderByDescending(g => g.OpenedAtUtc).Select(g => g.AssignedRoleCode).FirstOrDefault(),
        PendingUserName = o.GateInstances.Where(g => g.State == GateState.Pending && g.AssignedUser != null).OrderByDescending(g => g.OpenedAtUtc).Select(g => g.AssignedUser!.DisplayName).FirstOrDefault(),
        PendingSinceUtc = o.GateInstances.Where(g => g.State == GateState.Pending).OrderByDescending(g => g.OpenedAtUtc).Select(g => (DateTime?)g.OpenedAtUtc).FirstOrDefault()
    };

    internal static IQueryable<Opportunity> ApplySort(IQueryable<Opportunity> query, string? sortBy, string? sortDir)
    {
        var desc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        IOrderedQueryable<Opportunity> Order<TKey>(System.Linq.Expressions.Expression<Func<Opportunity, TKey>> key) =>
            desc ? query.OrderByDescending(key) : query.OrderBy(key);

        return (sortBy ?? "").ToLowerInvariant() switch
        {
            "opportunitynumber" or "number" => Order(o => o.OpportunityNumber),
            "name" => Order(o => o.Name),
            "customername" or "customer" => Order(o => o.Customer.NameEn),
            "expectedvaluesar" or "value" => Order(o => o.ExpectedValueSar),
            "stage" or "stagecode" or "stagenameen" => desc ? query.OrderByDescending(o => o.Stage.SortOrder).ThenByDescending(o => o.Status.SortOrder)
                                                          : query.OrderBy(o => o.Stage.SortOrder).ThenBy(o => o.Status.SortOrder),
            "status" or "statuscode" or "statusnameen" => desc ? query.OrderByDescending(o => o.Stage.SortOrder).ThenByDescending(o => o.Status.SortOrder)
                                                             : query.OrderBy(o => o.Stage.SortOrder).ThenBy(o => o.Status.SortOrder),
            "owner" or "ownerdisplayname" => Order(o => o.OwnerUser != null ? o.OwnerUser.DisplayName : ""),
            "builder" or "builderdisplayname" => Order(o => o.BuilderUser != null ? o.BuilderUser.DisplayName : ""),
            "submissiondeadline" => Order(o => o.Deadlines.SubmissionDeadline),
            "internaldeadline" => Order(o => o.Deadlines.InternalDeadline),
            "submissiontheme" or "theme" => Order(o => o.SubmissionTheme),
            "sourcechannel" or "source" => Order(o => o.SourceChannel),
            _ => Order(o => o.CreatedAtUtc)
        };
    }

    public async Task<OpportunityDetailDto> Handle(GetOpportunityQuery request, CancellationToken ct)
        => await MapDetail(request.Id, ct);

    public async Task<OpportunitySummaryDto> Handle(GetOpportunitySummaryQuery request, CancellationToken ct)
    {
        var o = await Load(request.Id, ct);
        return new OpportunitySummaryDto(o.Id, o.OpportunityNumber, o.Name, o.Customer.NameEn, o.Customer.NameAr,
            o.Stage.Code, o.Stage.NameEn, o.Stage.NameAr, o.Status.Code, o.Status.NameEn, o.Status.NameAr,
            o.ExpectedValueSar, o.Deadlines.SubmissionDeadline, o.IsClosed);
    }

    public async Task<OpportunityDetailDto> Handle(CreateOpportunityCommand request, CancellationToken ct)
    {
        if (!_user.HasRole(RoleCodes.AM) && !_user.HasRole(RoleCodes.BidsPresales) && !_user.IsAdmin)
            throw new ForbiddenException();

        var status = await _db.Statuses.Include(s => s.Stage).FirstAsync(s => s.Code == StatusCodes.AwaitingAssessment && s.Stage.Code == StageCodes.Qualification, ct);

        var opp = new Opportunity
        {
            Id = Guid.NewGuid(),
            OpportunityNumber = await _numbers.NextOpportunityNumberAsync(ct),
            Name = request.Name,
            CustomerId = request.CustomerId,
            SourceChannel = request.SourceChannel,
            SourceChannelOther = request.SourceChannelOther,
            SubmissionTheme = request.SubmissionTheme,
            EngagementType = request.EngagementType,
            OpportunityType = request.OpportunityType,
            ExpectedValueSar = request.ExpectedValueSar,
            RelationWithClientScore = request.RelationWithClientScore,
            WinProbabilityScore = request.WinProbabilityScore,
            DurationMonths = request.DurationMonths,
            ProposalLanguage = request.ProposalLanguage,
            RequiresBidBond = request.RequiresBidBond,
            StageId = status.StageId,
            StatusId = status.Id,
            SubmittedByUserId = _user.UserId!.Value,
            OwnerUserId = _user.UserId
        };
        _db.Opportunities.Add(opp);
        if (!string.IsNullOrWhiteSpace(request.ScopeBrief) || request.ScopeItems is { Count: > 0 })
        {
            var scope = new ScopeOfWork { OpportunityId = opp.Id, Brief = request.ScopeBrief?.Trim() ?? string.Empty };
            var order = 1;
            foreach (var item in request.ScopeItems ?? [])
            {
                if (string.IsNullOrWhiteSpace(item.Title)) continue;
                scope.Items.Add(new ScopeItem
                {
                    Title = item.Title.Trim(), ServiceLineId = item.ServiceLineId,
                    Description = item.Description, Comment = item.Comment, SortOrder = order++
                });
            }
            _db.ScopesOfWork.Add(scope);
        }

        await _workflow.OpenGateAsync(opp, GateCodes.Gw1Review, ct: ct);
        await _email.ComposeAsync(EmailTemplateCodes.OppReceived, opp, _user.UserId.Value, ct: ct);
        await _db.SaveChangesAsync(ct);
        return await MapDetail(opp.Id, ct);
    }

    public async Task<OpportunityDetailDto> Handle(UpdateOpportunityCommand request, CancellationToken ct)
    {
        var o = await Load(request.Id, ct);
        o.Name = request.Name;
        o.CustomerId = request.CustomerId;
        o.SourceChannel = request.SourceChannel;
        o.SourceChannelOther = request.SourceChannelOther;
        o.SubmissionTheme = request.SubmissionTheme;
        o.EngagementType = request.EngagementType;
        o.OpportunityType = request.OpportunityType;
        o.ExpectedValueSar = request.ExpectedValueSar;
        o.RelationWithClientScore = request.RelationWithClientScore;
        o.WinProbabilityScore = request.WinProbabilityScore;
        o.DurationMonths = request.DurationMonths;
        o.ProposalLanguage = request.ProposalLanguage;
        o.RequiresBidBond = request.RequiresBidBond;
        await _db.SaveChangesAsync(ct);
        return await MapDetail(o.Id, ct);
    }

    public async Task<Unit> Handle(DeleteOpportunityCommand request, CancellationToken ct)
    {
        if (!_user.IsAdmin) throw new ForbiddenException();
        var o = await Load(request.Id, ct);
        o.IsDeleted = true;
        o.DeletedAtUtc = _clock.UtcNow;
        o.DeletedByUserId = _user.UserId;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SetDeadlinesCommand request, CancellationToken ct)
    {
        var o = await Load(request.Id, ct);
        o.Deadlines.QualificationDeadline = request.QualificationDeadline;
        o.Deadlines.InquiriesDeadline = request.InquiriesDeadline;
        o.Deadlines.EstimatedCostDeadline = request.EstimatedCostDeadline;
        o.Deadlines.InternalDeadline = request.InternalDeadline;
        o.Deadlines.SubmissionDeadline = request.SubmissionDeadline;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SetOwnerCommand request, CancellationToken ct)
    {
        var o = await Load(request.Id, ct);
        o.OwnerUserId = request.OwnerUserId;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<IReadOnlyList<TimelineItemDto>> Handle(GetTimelineQuery request, CancellationToken ct)
    {
        var audits = await _db.AuditLogs.AsNoTracking().Where(a => a.OpportunityId == request.Id)
            .OrderBy(a => a.OccurredAtUtc).ToListAsync(ct);
        return audits.Select(a => new TimelineItemDto(a.OccurredAtUtc, a.Action.ToString(), a.Description, a.FromValue + " → " + a.ToValue, a.ActorRoleCode)).ToList();
    }

    public async Task<IReadOnlyList<AvailableTransitionDto>> Handle(GetAvailableTransitionsQuery request, CancellationToken ct)
    {
        var o = await Load(request.Id, ct);
        var list = await _workflow.GetAvailableTransitionsAsync(o, _user.Roles, ct);
        var meta = await _db.StatusTransitions.AsNoTracking()
            .Where(t => t.FromStatusId == o.StatusId)
            .Select(t => new { t.ToStatusId, t.RequiresReason, t.RequiredRoleCode })
            .ToListAsync(ct);
        return list.Where(s => !EngineOwnedStatuses.Contains(s.Code))
            .Select(s =>
            {
                var t = meta.FirstOrDefault(m => m.ToStatusId == s.Id);
                return new AvailableTransitionDto(
                    s.Id, s.Code, s.NameEn, s.NameAr,
                    s.StageId, s.Stage?.Code ?? string.Empty, s.IsTerminal,
                    t?.RequiresReason ?? false, t?.RequiredRoleCode ?? string.Empty);
            }).ToList();
    }

    /// <summary>
    /// Statuses that carry side effects (records, gates, e-mails) and therefore may only be reached
    /// through their dedicated command, never via the generic status endpoint.
    /// </summary>
    private static readonly string[] EngineOwnedStatuses =
    [
        StatusCodes.Submitted, StatusCodes.Won, StatusCodes.Lost, StatusCodes.ContractSigned,
        StatusCodes.NotQualified, StatusCodes.NotApproved, StatusCodes.Hold, StatusCodes.Canceled, StatusCodes.InternalReview
    ];

    public async Task<Unit> Handle(ChangeStatusCommand request, CancellationToken ct)
    {
        var o = await Load(request.Id, ct);
        var target = await _db.Statuses.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ToStatusId, ct)
            ?? throw new NotFoundException(nameof(Status), request.ToStatusId);
        if (EngineOwnedStatuses.Contains(target.Code))
            throw new BusinessRuleException(
                $"'{target.NameEn}' cannot be set directly. Use the dedicated action (submit, record outcome, hold, cancel, gate decision) so the workflow side-effects are applied.",
                new Dictionary<string, string[]> { ["toStatusId"] = ["Engine-owned status."] });
        await _workflow.ChangeStatusAsync(o, request.ToStatusId, request.Reason, _user.Roles, ct);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(HoldCommand request, CancellationToken ct)
    {
        var o = await Load(request.Id, ct);
        await _workflow.HoldAsync(o, request.Reason, ct);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(ResumeCommand request, CancellationToken ct)
    {
        var o = await Load(request.Id, ct);
        await _workflow.ResumeAsync(o, ct);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(CancelOpportunityCommand request, CancellationToken ct)
    {
        var o = await Load(request.Id, ct);
        await _workflow.CancelAsync(o, request.Reason, _user.Roles, ct);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private async Task<Opportunity> Load(Guid id, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(id, ct);
        return await _db.Opportunities
            .Include(o => o.Customer).Include(o => o.Stage).Include(o => o.Status)
            .Include(o => o.SubmittedByUser).Include(o => o.OwnerUser).Include(o => o.BuilderUser)
            .FirstOrDefaultAsync(o => o.Id == id, ct) ?? throw new NotFoundException(nameof(Opportunity), id);
    }

    private async Task<OpportunityDetailDto> MapDetail(Guid id, CancellationToken ct)
    {
        var o = await Load(id, ct);
        var dto = Map(o);
        dto.PendingOn = await LoadPendingOn(id, ct);
        dto.ScopeBrief = await _db.ScopesOfWork.AsNoTracking().Where(s => s.OpportunityId == id).Select(s => s.Brief).FirstOrDefaultAsync(ct);
        return dto;
    }

    private async Task<PendingOnDto?> LoadPendingOn(Guid opportunityId, CancellationToken ct)
    {
        var pending = await _db.GateInstances.AsNoTracking()
            .Include(g => g.Gate)
            .Include(g => g.AssignedUser)
            .Where(g => g.OpportunityId == opportunityId && g.State == GateState.Pending)
            .OrderByDescending(g => g.OpenedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (pending is null)
            return null;

        var (roleNameEn, roleNameAr) = await ResolveRoleNames(pending.AssignedRoleCode, ct);
        var label = pending.AssignedUser?.DisplayName ?? roleNameEn;

        return new PendingOnDto
        {
            GateInstanceId = pending.Id,
            GateCode = pending.Gate.Code,
            GateNameEn = pending.Gate.NameEn,
            GateNameAr = pending.Gate.NameAr,
            RoleCode = pending.AssignedRoleCode,
            RoleNameEn = roleNameEn,
            RoleNameAr = roleNameAr,
            AssignedUserId = pending.AssignedUserId,
            AssignedUserName = pending.AssignedUser?.DisplayName,
            Label = label,
            OpenedAtUtc = pending.OpenedAtUtc
        };
    }

    private async Task<(string En, string Ar)> ResolveRoleNames(string roleCodes, CancellationToken ct)
    {
        var codes = roleCodes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (codes.Length == 0)
            return (roleCodes, roleCodes);

        var roles = await _db.Roles.AsNoTracking()
            .Where(r => codes.Contains(r.Code))
            .ToListAsync(ct);

        var en = string.Join(" / ", codes.Select(c => roles.FirstOrDefault(r => r.Code == c)?.NameEn ?? c));
        var ar = string.Join(" / ", codes.Select(c => roles.FirstOrDefault(r => r.Code == c)?.NameAr ?? c));
        return (en, ar);
    }

    private static OpportunityDetailDto Map(Opportunity o) => new()
    {
        Id = o.Id,
        OpportunityNumber = o.OpportunityNumber,
        Name = o.Name,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer?.NameEn ?? "",
        CustomerNameAr = o.Customer?.NameAr ?? "",
        SourceChannel = o.SourceChannel,
        SourceChannelOther = o.SourceChannelOther,
        SubmissionTheme = o.SubmissionTheme,
        EngagementType = o.EngagementType,
        OpportunityType = o.OpportunityType,
        ExpectedValueSar = o.ExpectedValueSar,
        RelationWithClientScore = o.RelationWithClientScore,
        WinProbabilityScore = o.WinProbabilityScore,
        DurationMonths = o.DurationMonths,
        ProposalLanguage = o.ProposalLanguage,
        StageId = o.StageId,
        StageCode = o.Stage?.Code ?? "",
        StageNameEn = o.Stage?.NameEn ?? "",
        StageNameAr = o.Stage?.NameAr ?? "",
        StatusId = o.StatusId,
        StatusCode = o.Status?.Code ?? "",
        StatusNameEn = o.Status?.NameEn ?? "",
        StatusNameAr = o.Status?.NameAr ?? "",
        IsTerminal = o.Status?.IsTerminal ?? false,
        SubmittedByUserId = o.SubmittedByUserId,
        SubmittedByName = o.SubmittedByUser?.DisplayName ?? "",
        OwnerUserId = o.OwnerUserId,
        OwnerDisplayName = o.OwnerUser?.DisplayName,
        BuilderType = o.BuilderType,
        BuilderUserId = o.BuilderUserId,
        BuilderDisplayName = o.BuilderUser?.DisplayName,
        RequiresQualificationMeeting = o.RequiresQualificationMeeting,
        RequiresBidBond = o.RequiresBidBond,
        IsClosed = o.IsClosed,
        IsOnHold = o.Status?.Code == StatusCodes.Hold,
        HoldReason = o.HoldReason,
        ClosedAtUtc = o.ClosedAtUtc,
        CreatedAtUtc = o.CreatedAtUtc,
        RowVersion = o.RowVersion,
        Deadlines = new OpportunityDeadlinesDto
        {
            QualificationDeadline = o.Deadlines.QualificationDeadline,
            InquiriesDeadline = o.Deadlines.InquiriesDeadline,
            EstimatedCostDeadline = o.Deadlines.EstimatedCostDeadline,
            InternalDeadline = o.Deadlines.InternalDeadline,
            SubmissionDeadline = o.Deadlines.SubmissionDeadline
        }
    };
}
