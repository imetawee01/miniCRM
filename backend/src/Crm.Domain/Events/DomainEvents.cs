using Crm.Domain.Common;
using Crm.Domain.Entities;

namespace Crm.Domain.Events;

public sealed record OpportunityCreatedEvent(Guid OpportunityId, DateTime OccurredAtUtc) : IDomainEvent;
public sealed record StatusChangedEvent(Guid OpportunityId, Guid FromStatusId, Guid ToStatusId, DateTime OccurredAtUtc) : IDomainEvent;
public sealed record GateOpenedEvent(Guid OpportunityId, Guid GateInstanceId, string GateCode, DateTime OccurredAtUtc) : IDomainEvent;
public sealed record GateDecidedEvent(Guid OpportunityId, Guid GateInstanceId, string Decision, DateTime OccurredAtUtc) : IDomainEvent;
public sealed record BuilderAssignedEvent(Guid OpportunityId, Guid BuilderUserId, DateTime OccurredAtUtc) : IDomainEvent;
public sealed record EmailGeneratedEvent(Guid OpportunityId, Guid EmailId, string TemplateCode, DateTime OccurredAtUtc) : IDomainEvent;
