using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Query;

public record SavedViewDto(Guid Id, string Name, string EntityType, string DefinitionJson, bool IsShared, bool IsDefault, int SortOrder, bool IsMine);
public record GetSavedViewsQuery(string EntityType) : IRequest<IReadOnlyList<SavedViewDto>>;
public record CreateSavedViewCommand(string Name, string EntityType, string DefinitionJson, bool IsShared, bool IsDefault) : IRequest<Guid>;
public record UpdateSavedViewCommand(Guid Id, string Name, string DefinitionJson, bool IsShared, bool IsDefault, int SortOrder) : IRequest<Unit>;
public record DeleteSavedViewCommand(Guid Id) : IRequest<Unit>;

public sealed class SavedViewHandlers :
    IRequestHandler<GetSavedViewsQuery, IReadOnlyList<SavedViewDto>>,
    IRequestHandler<CreateSavedViewCommand, Guid>,
    IRequestHandler<UpdateSavedViewCommand, Unit>,
    IRequestHandler<DeleteSavedViewCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public SavedViewHandlers(IApplicationDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<IReadOnlyList<SavedViewDto>> Handle(GetSavedViewsQuery q, CancellationToken ct)
    {
        var uid = _user.UserId;
        var rows = await _db.SavedViews.AsNoTracking()
            .Where(v => v.EntityType == q.EntityType && (v.IsShared || v.UserId == uid))
            .OrderBy(v => v.SortOrder).ThenBy(v => v.Name)
            .ToListAsync(ct);
        return rows.Select(v => new SavedViewDto(v.Id, v.Name, v.EntityType, v.DefinitionJson, v.IsShared, v.IsDefault, v.SortOrder, v.UserId == uid)).ToList();
    }

    public async Task<Guid> Handle(CreateSavedViewCommand r, CancellationToken ct)
    {
        if (r.IsDefault)
            await ClearDefaults(r.EntityType, ct);
        var v = new SavedView
        {
            UserId = _user.UserId,
            Name = r.Name.Trim(),
            EntityType = r.EntityType.Trim(),
            DefinitionJson = r.DefinitionJson,
            IsShared = r.IsShared,
            IsDefault = r.IsDefault
        };
        _db.SavedViews.Add(v);
        await _db.SaveChangesAsync(ct);
        return v.Id;
    }

    public async Task<Unit> Handle(UpdateSavedViewCommand r, CancellationToken ct)
    {
        var v = await _db.SavedViews.FirstOrDefaultAsync(x => x.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(SavedView), r.Id);
        if (v.UserId != _user.UserId && !_user.IsAdmin)
            throw new ForbiddenException();
        if (r.IsDefault)
            await ClearDefaults(v.EntityType, ct);
        v.Name = r.Name.Trim();
        v.DefinitionJson = r.DefinitionJson;
        v.IsShared = r.IsShared;
        v.IsDefault = r.IsDefault;
        v.SortOrder = r.SortOrder;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(DeleteSavedViewCommand r, CancellationToken ct)
    {
        var v = await _db.SavedViews.FirstOrDefaultAsync(x => x.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(SavedView), r.Id);
        if (v.UserId != _user.UserId && !_user.IsAdmin)
            throw new ForbiddenException();
        _db.SavedViews.Remove(v);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private async Task ClearDefaults(string entityType, CancellationToken ct)
    {
        var uid = _user.UserId;
        var mine = await _db.SavedViews.Where(v => v.EntityType == entityType && v.UserId == uid && v.IsDefault).ToListAsync(ct);
        foreach (var v in mine) v.IsDefault = false;
    }
}
