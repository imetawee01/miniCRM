using Crm.Application.Abstractions;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Crm.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Append-only audit writer hooked into SaveChanges so no state change can bypass the log.
/// Explicit <see cref="IAuditWriter"/> calls cover domain-event style entries in the same transaction.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor, IAuditWriter
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;
    private readonly List<AuditLog> _pending = [];

    public AuditSaveChangesInterceptor(ICurrentUser currentUser, IDateTime clock)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public void Add(
        string entityType,
        Guid entityId,
        AuditAction action,
        string description,
        Guid? opportunityId = null,
        string? fromValue = null,
        string? toValue = null,
        string? metadataJson = null)
    {
        _pending.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            OpportunityId = opportunityId,
            Action = action,
            ActorUserId = _currentUser.UserId ?? Guid.Empty,
            ActorRoleCode = _currentUser.Roles.FirstOrDefault() ?? "SYSTEM",
            OccurredAtUtc = _clock.UtcNow,
            FromValue = fromValue,
            ToValue = toValue,
            Description = description,
            MetadataJson = metadataJson,
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent
        });
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog or RefreshToken or OpportunityNumberSequence or ContractNumberSequence)
                continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var entityType = entry.Entity.GetType().Name;
            var id = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id")?.CurrentValue as Guid? ?? Guid.Empty;
            Guid? oppId = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "OpportunityId")?.CurrentValue as Guid?;

            var action = InferAction(entityType, entry.State);
            if (action is null) continue;

            // Skip if an explicit entry for the same action/entity is already queued
            if (_pending.Any(a => a.EntityId == id && a.Action == action)) continue;

            Add(entityType, id, action.Value,
                $"{entry.State} {entityType}",
                oppId);
        }

        if (_pending.Count > 0)
        {
            context.Set<AuditLog>().AddRange(_pending);
            _pending.Clear();
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static AuditAction? InferAction(string entityType, EntityState state) => (entityType, state) switch
    {
        ("Opportunity", EntityState.Added) => AuditAction.OpportunityCreated,
        ("Opportunity", EntityState.Modified) => AuditAction.OpportunityUpdated,
        ("Note", EntityState.Added) => AuditAction.NoteAdded,
        ("Note", EntityState.Modified) => AuditAction.NoteEdited,
        ("Comment", EntityState.Added) => AuditAction.CommentAdded,
        ("Attachment", EntityState.Added) => AuditAction.AttachmentUploaded,
        ("GeneratedEmail", EntityState.Added) => AuditAction.EmailGenerated,
        _ => null
    };
}
