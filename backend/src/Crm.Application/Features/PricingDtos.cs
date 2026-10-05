using Crm.Domain.Entities;
using Crm.Domain.Enums;

namespace Crm.Application.Features;

public record EstimatedCostDto(
    Guid Id,
    Guid OpportunityId,
    decimal? Amount,
    string Currency,
    Guid SubmittedByUserId,
    DateTime SubmittedAtUtc,
    string? Notes,
    bool PricingVisible);

public record ProposalPricingDto(
    Guid Id,
    Guid OpportunityId,
    decimal? PriceSar,
    decimal? CostSar,
    decimal? MarginPercent,
    int Version,
    bool IsCurrent,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    bool PricingVisible);

public record SlResponseDto(
    Guid Id,
    Guid OpportunityId,
    Guid ServiceLineId,
    string? ServiceLineNameEn,
    string? ServiceLineNameAr,
    Guid? ScopeItemId,
    Guid? TechnicalProposalAttachmentId,
    Guid? CostingAttachmentId,
    decimal? CostSar,
    SlResponseStatus Status,
    DateTime? SubmittedAtUtc,
    DateTime? DueAtUtc,
    string? ReturnReason,
    bool PricingVisible);

public static class PricingFieldMask
{
    public static EstimatedCostDto? From(EstimatedCost? e, bool canView) =>
        e is null ? null : new EstimatedCostDto(
            e.Id, e.OpportunityId,
            canView ? e.Amount : null,
            e.Currency, e.SubmittedByUserId, e.SubmittedAtUtc, e.Notes, canView);

    public static ProposalPricingDto From(ProposalPricing p, bool canView) =>
        new(p.Id, p.OpportunityId,
            canView ? p.PriceSar : null,
            canView ? p.CostSar : null,
            canView ? p.MarginPercent : null,
            p.Version, p.IsCurrent, p.CreatedByUserId, p.CreatedAtUtc, canView);

    public static SlResponseDto From(SlResponse s, bool canView) =>
        new(s.Id, s.OpportunityId, s.ServiceLineId,
            s.ServiceLine?.NameEn, s.ServiceLine?.NameAr,
            s.ScopeItemId, s.TechnicalProposalAttachmentId, s.CostingAttachmentId,
            canView ? s.CostSar : null,
            s.Status, s.SubmittedAtUtc, s.DueAtUtc, s.ReturnReason, canView);

    public static void MaskContract(Contract c, bool canView)
    {
        if (canView) return;
        c.ContractValueSar = 0;
        c.PaymentTerms = null;
    }
}
