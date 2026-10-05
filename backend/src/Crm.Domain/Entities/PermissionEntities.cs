using Crm.Domain.Common;

namespace Crm.Domain.Entities;

public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? LeadUserId { get; set; }
    public User? LeadUser { get; set; }
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
}

public class TeamMember
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}

/// <summary>Maps an SL (or dual-role) user to the service line(s) they cover.</summary>
public class UserServiceLine
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ServiceLineId { get; set; }
    public ServiceLine ServiceLine { get; set; } = null!;
}

/// <summary>Persisted capability ↔ role matrix (overlays AuthorizationPolicies defaults).</summary>
public class RolePermission : BaseEntity
{
    public string PolicyName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public bool IsAllowed { get; set; } = true;
}
