using Crm.Domain.Common;
using Crm.Domain.Enums;

namespace Crm.Domain.Entities;

public class Opportunity : AuditableEntity, ISoftDelete
{
    public string OpportunityNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
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
    public Stage Stage { get; set; } = null!;
    public Guid StatusId { get; set; }
    public Status Status { get; set; } = null!;
    public Guid SubmittedByUserId { get; set; }
    public User SubmittedByUser { get; set; } = null!;
    public Guid? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }
    public BuilderType? BuilderType { get; set; }
    public Guid? BuilderUserId { get; set; }
    public User? BuilderUser { get; set; }
    public bool? RequiresQualificationMeeting { get; set; }
    public bool RequiresBidBond { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }

    /// <summary>Status to restore on resume after Hold.</summary>
    public Guid? HoldPriorStatusId { get; set; }
    public Guid? HoldPriorStageId { get; set; }
    public string? HoldReason { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public OpportunityDeadlines Deadlines { get; set; } = new();
    public ScopeOfWork? ScopeOfWork { get; set; }
    public BidBond? BidBond { get; set; }
    public EstimatedCost? EstimatedCost { get; set; }
    public Submission? Submission { get; set; }
    public OpportunityOutcome? Outcome { get; set; }
    public Contract? Contract { get; set; }

    public ICollection<GateInstance> GateInstances { get; set; } = new List<GateInstance>();
    public ICollection<ProposalPricing> PricingVersions { get; set; } = new List<ProposalPricing>();
    public ICollection<SlResponse> SlResponses { get; set; } = new List<SlResponse>();
    public ICollection<QualificationMeeting> QualificationMeetings { get; set; } = new List<QualificationMeeting>();
    public ICollection<GeneratedEmail> GeneratedEmails { get; set; } = new List<GeneratedEmail>();
}

public class OpportunityDeadlines
{
    public DateTime? QualificationDeadline { get; set; }
    public DateTime? InquiriesDeadline { get; set; }
    public DateTime? EstimatedCostDeadline { get; set; }
    public DateTime? InternalDeadline { get; set; }
    public DateTime? SubmissionDeadline { get; set; }
}

public class ScopeOfWork : BaseEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public string Brief { get; set; } = string.Empty;
    public ICollection<ScopeItem> Items { get; set; } = new List<ScopeItem>();
}

public class ScopeItem : BaseEntity
{
    public Guid ScopeOfWorkId { get; set; }
    public ScopeOfWork ScopeOfWork { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ServiceLineId { get; set; }
    public ServiceLine ServiceLine { get; set; } = null!;
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
    public string? Comment { get; set; }
    public int SortOrder { get; set; }
}

public class GateInstance : AuditableEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public Guid GateId { get; set; }
    public WorkflowGate Gate { get; set; } = null!;
    public GateState State { get; set; } = GateState.Pending;
    public string AssignedRoleCode { get; set; } = string.Empty;
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }
    public string? Decision { get; set; }
    public string? Reason { get; set; }
    public int Round { get; set; } = 1;
}
