using Crm.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Persistence;

public sealed class NumberGenerator : INumberGenerator
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public NumberGenerator(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<string> NextOpportunityNumberAsync(CancellationToken ct = default)
    {
        var year = _clock.UtcNow.Year;
        var seq = await _db.OpportunityNumberSequences.FirstOrDefaultAsync(s => s.Year == year, ct);
        if (seq is null)
        {
            seq = new Domain.Entities.OpportunityNumberSequence { Year = year, LastValue = 0 };
            _db.OpportunityNumberSequences.Add(seq);
        }
        seq.LastValue++;
        return $"OPP-{year}-{seq.LastValue:00000}";
    }

    public async Task<string> NextContractNumberAsync(CancellationToken ct = default)
    {
        var year = _clock.UtcNow.Year;
        var seq = await _db.ContractNumberSequences.FirstOrDefaultAsync(s => s.Year == year, ct);
        if (seq is null)
        {
            seq = new Domain.Entities.ContractNumberSequence { Year = year, LastValue = 0 };
            _db.ContractNumberSequences.Add(seq);
        }
        seq.LastValue++;
        return $"CTR-{year}-{seq.LastValue:00000}";
    }
}
