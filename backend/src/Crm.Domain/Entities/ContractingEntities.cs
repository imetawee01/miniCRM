using Crm.Domain.Common;
using Crm.Domain.Enums;

namespace Crm.Domain.Entities;

public class Contract : AuditableEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public string ContractNumber { get; set; } = string.Empty;
    public ContractStatus ContractStatus { get; set; } = ContractStatus.Won;
    public decimal ContractValueSar { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationMonths { get; set; }
    public DateTime? SignedAtUtc { get; set; }
    public string? SignedByCustomerRepresentative { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<ContractNegotiationRound> NegotiationRounds { get; set; } = new List<ContractNegotiationRound>();
    public ICollection<ContractMilestone> Milestones { get; set; } = new List<ContractMilestone>();
}

public class ContractNegotiationRound : BaseEntity
{
    public Guid ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public int RoundNumber { get; set; }
    public string RequestedChanges { get; set; } = string.Empty;
    public string? OurPosition { get; set; }
    public string? CustomerPosition { get; set; }
    public NegotiationRoundStatus Status { get; set; } = NegotiationRoundStatus.Open;
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public Guid OpenedByUserId { get; set; }
    public User OpenedByUser { get; set; } = null!;
}

public class ContractMilestone : BaseEntity
{
    public Guid ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal? AmountSar { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class EmailTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplateHtml { get; set; } = string.Empty;
    public string DefaultTo { get; set; } = string.Empty;
    public string DefaultCc { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class GeneratedEmail : BaseEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public string TemplateCode { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Cc { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public GeneratedEmailStatus Status { get; set; } = GeneratedEmailStatus.Draft;
    public DateTime GeneratedAtUtc { get; set; }
    public Guid GeneratedByUserId { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public string AttachmentIdsJson { get; set; } = "[]";
}
