using Crm.Domain.Common;
using Crm.Domain.Enums;

namespace Crm.Domain.Entities;

public class EstimatedCost : BaseEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public Guid SubmittedByUserId { get; set; }
    public User SubmittedByUser { get; set; } = null!;
    public DateTime SubmittedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public class ProposalPricing : BaseEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public decimal PriceSar { get; set; }
    public decimal CostSar { get; set; }
    public decimal MarginPercent { get; set; }
    public int Version { get; set; }
    public bool IsCurrent { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }

    public static decimal ComputeMargin(decimal priceSar, decimal costSar)
        => priceSar == 0 ? 0 : Math.Round((priceSar - costSar) / priceSar * 100m, 2);
}

public class BidBond : BaseEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public bool Required { get; set; }
    public decimal? AmountSar { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? IssuingBank { get; set; }
    public BidBondStatus Status { get; set; } = BidBondStatus.NotRequired;
    public DateTime? RequestedAtUtc { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public string? RejectReason { get; set; }
}

public class QualificationMeeting : BaseEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public DateTime ScheduledAtUtc { get; set; }
    public string? Location { get; set; }
    public string? MeetingLink { get; set; }
    public string? Agenda { get; set; }
    public string? MinutesOfMeeting { get; set; }
    public MeetingOutcome Outcome { get; set; } = MeetingOutcome.Pending;
    public DateTime? HeldAtUtc { get; set; }
    public ICollection<QualificationMeetingAttendee> Attendees { get; set; } = new List<QualificationMeetingAttendee>();
}

public class QualificationMeetingAttendee : BaseEntity
{
    public Guid MeetingId { get; set; }
    public QualificationMeeting Meeting { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? ServiceLineId { get; set; }
    public ServiceLine? ServiceLine { get; set; }
    public AttendeeResponse Response { get; set; } = AttendeeResponse.Invited;
}

public class SlResponse : AuditableEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public Guid ServiceLineId { get; set; }
    public ServiceLine ServiceLine { get; set; } = null!;
    public Guid? ScopeItemId { get; set; }
    public ScopeItem? ScopeItem { get; set; }
    public Guid? TechnicalProposalAttachmentId { get; set; }
    public Attachment? TechnicalProposalAttachment { get; set; }
    public Guid? CostingAttachmentId { get; set; }
    public Attachment? CostingAttachment { get; set; }
    public decimal? CostSar { get; set; }
    public SlResponseStatus Status { get; set; } = SlResponseStatus.Pending;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public string? ReturnReason { get; set; }
}

public class Submission : BaseEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public DateTime SubmittedAtUtc { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public User SubmittedByUser { get; set; } = null!;
    public SubmissionChannel Channel { get; set; }
    public string? Reference { get; set; }
}

public class OpportunityOutcome : BaseEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public OutcomeResult Result { get; set; }
    public DateTime AnnouncedAtUtc { get; set; }
    public decimal? AwardedValueSar { get; set; }
    public string? CompetitorName { get; set; }
    public string? LossReason { get; set; }
    public string? Notes { get; set; }
}
