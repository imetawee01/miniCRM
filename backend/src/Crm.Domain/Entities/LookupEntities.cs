using Crm.Domain.Common;

namespace Crm.Domain.Entities;

public class Customer : AuditableEntity, ISoftDelete
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public bool IsGovernment { get; set; }
    public string? Website { get; set; }
    /// <summary>True when created via opportunity quick-create (vs Admin).</summary>
    public bool IsQuickCreated { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ICollection<CustomerContact> Contacts { get; set; } = new List<CustomerContact>();
    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
}

public class CustomerContact : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Title { get; set; }
    public bool IsPrimary { get; set; }
}

public class ServiceLine : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public Guid? LeadUserId { get; set; }
    public User? LeadUser { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string? ColourHex { get; set; }
}

public class Stage : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Status> Statuses { get; set; } = new List<Status>();
}

public class Status : BaseEntity
{
    public Guid StageId { get; set; }
    public Stage Stage { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsTerminal { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StatusTransition : BaseEntity
{
    public Guid FromStatusId { get; set; }
    public Status FromStatus { get; set; } = null!;
    public Guid ToStatusId { get; set; }
    public Status ToStatus { get; set; } = null!;
    public string RequiredRoleCode { get; set; } = string.Empty;
    public bool RequiresReason { get; set; }
}

public class WorkflowGate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string ResponsibleRoleCode { get; set; } = string.Empty;
    /// <summary>CSV of allowed decisions, e.g. "Approve,Reject".</summary>
    public string AllowedDecisions { get; set; } = string.Empty;
    public bool RequiresReasonOnReject { get; set; }
    public bool RequiresAttachmentOnApprove { get; set; }
    public bool IsActive { get; set; } = true;
}

public class OpportunityNumberSequence
{
    public int Year { get; set; }
    public int LastValue { get; set; }
}

public class ContractNumberSequence
{
    public int Year { get; set; }
    public int LastValue { get; set; }
}
