using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Security;

public sealed class OpportunityVisibility : IOpportunityVisibility
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IPermissionStore _permissions;

    public OpportunityVisibility(IApplicationDbContext db, ICurrentUser user, IPermissionStore permissions)
    {
        _db = db;
        _user = user;
        _permissions = permissions;
    }

    public bool CanViewPricingFields
    {
        get
        {
            var allowed = _permissions.GetAllowedRoles(AuthorizationPolicies.CanViewPricing);
            return allowed.Any(r => _user.HasRole(r) || (_user.IsAdmin && r == RoleCodes.Admin));
        }
    }

    public IQueryable<Opportunity> Apply(IQueryable<Opportunity> query)
    {
        if (!_user.IsAuthenticated || _user.UserId is not Guid uid)
            return query.Where(_ => false);

        if (SeesAll())
            return query;

        var isAm = _user.HasRole(RoleCodes.AM);
        var isSl = _user.HasRole(RoleCodes.Sl);

        if (isAm && !isSl)
        {
            var teamMateIds = TeamMateIds(uid);
            return query.Where(o =>
                o.OwnerUserId == uid
                || o.SubmittedByUserId == uid
                || (o.OwnerUserId != null && teamMateIds.Contains(o.OwnerUserId.Value))
                || teamMateIds.Contains(o.SubmittedByUserId));
        }

        if (isSl)
        {
            var lineIds = _db.UserServiceLines.AsNoTracking()
                .Where(x => x.UserId == uid)
                .Select(x => x.ServiceLineId);

            return query.Where(o =>
                o.BuilderUserId == uid
                || (o.ScopeOfWork != null && o.ScopeOfWork.Items.Any(i => lineIds.Contains(i.ServiceLineId)))
                || o.SlResponses.Any(s => lineIds.Contains(s.ServiceLineId))
                || o.QualificationMeetings.Any(m => m.Attendees.Any(a =>
                    a.ServiceLineId != null && lineIds.Contains(a.ServiceLineId.Value))));
        }

        return query.Where(o =>
            o.OwnerUserId == uid || o.SubmittedByUserId == uid || o.BuilderUserId == uid);
    }

    public async Task EnsureCanAccessAsync(Guid opportunityId, CancellationToken ct = default)
    {
        var ok = await Apply(_db.Opportunities.AsNoTracking()).AnyAsync(o => o.Id == opportunityId, ct);
        if (!ok)
            throw new NotFoundException(nameof(Opportunity), opportunityId);
    }

    private bool SeesAll() =>
        _user.IsAdmin
        || _user.HasRole(RoleCodes.Presales)
        || _user.HasRole(RoleCodes.BidsPresales)
        || _user.HasRole(RoleCodes.BidsMgmt)
        || _user.HasRole(RoleCodes.Mgmt);

    private List<Guid> TeamMateIds(Guid uid)
    {
        var teamIds = _db.TeamMembers.AsNoTracking()
            .Where(m => m.UserId == uid)
            .Select(m => m.TeamId);
        return _db.TeamMembers.AsNoTracking()
            .Where(m => teamIds.Contains(m.TeamId))
            .Select(m => m.UserId)
            .Distinct()
            .ToList();
    }
}
