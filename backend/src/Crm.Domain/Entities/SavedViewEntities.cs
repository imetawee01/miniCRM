using Crm.Domain.Common;

namespace Crm.Domain.Entities;

/// <summary>Named list configuration (search, filters, group-by, columns, sort).</summary>
public class SavedView : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = "{}";
    public bool IsShared { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Reusable export column template.</summary>
public class ExportTemplate : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string FieldsJson { get; set; } = "[]";
    public bool IsShared { get; set; }
}
