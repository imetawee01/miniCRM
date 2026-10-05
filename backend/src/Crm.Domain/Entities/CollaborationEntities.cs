using Crm.Domain.Common;
using Crm.Domain.Enums;

namespace Crm.Domain.Entities;

public class Note : BaseEntity, ISoftDelete
{
    public OwnerEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Body { get; set; } = string.Empty;
    public NoteVisibility Visibility { get; set; } = NoteVisibility.Internal;
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
}

public class Comment : BaseEntity, ISoftDelete
{
    public OwnerEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public string MentionedUserIdsJson { get; set; } = "[]";
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? EditedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}

public class Attachment : BaseEntity, ISoftDelete
{
    public OwnerEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Description { get; set; } = string.Empty;
    public AttachmentCategory Category { get; set; }
    public Guid UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;
    public DateTime UploadedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
}

public class AuditLog : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid? OpportunityId { get; set; }
    public AuditAction Action { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? FromValue { get; set; }
    public string? ToValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class AppNotification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public NotificationType Type { get; set; }
    /// <summary>English fallback text. Clients should prefer <see cref="MessageKey"/> + <see cref="ParamsJson"/>.</summary>
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    /// <summary>i18n key (e.g. "gateAssigned"). Frontend resolves notif.{key}.title / .body with params.</summary>
    public string? MessageKey { get; set; }
    public string? ParamsJson { get; set; }
    public string? LinkUrl { get; set; }
    public Guid? OpportunityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}

/// <summary>Scheduled work item on an opportunity (call / meeting / to-do / follow-up).</summary>
public class Activity : BaseEntity
{
    public Guid OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public ActivityType Type { get; set; } = ActivityType.Todo;
    public string Summary { get; set; } = string.Empty;
    public DateTime DueAtUtc { get; set; }
    public Guid AssignedUserId { get; set; }
    public User AssignedUser { get; set; } = null!;
    public DateTime? DoneAtUtc { get; set; }
    public string? Note { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
