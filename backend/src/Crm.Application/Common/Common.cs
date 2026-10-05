namespace Crm.Application.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        => new() { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
}

public record PagedQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? SortBy { get; set; }
    public string SortDir { get; set; } = "desc";
    public string? Search { get; set; }

    public int Skip => Math.Max(Page - 1, 0) * Take;
    public int Take => Math.Clamp(PageSize, 1, 200);
}

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }
    public IDictionary<string, string[]>? Errors { get; }

    protected AppException(int statusCode, string title, string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Title = title;
        Errors = errors;
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string name, object key)
        : base(404, "Not Found", $"{name} ({key}) was not found.") { }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Authentication required.")
        : base(401, "Unauthorized", message) { }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You are not allowed to perform this action.")
        : base(403, "Forbidden", message) { }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message, IDictionary<string, string[]>? errors = null)
        : base(409, "Conflict", message, errors) { }
}

public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, IDictionary<string, string[]>? errors = null)
        : base(422, "Unprocessable Entity", message, errors) { }
}

public sealed class ValidationAppException : AppException
{
    public ValidationAppException(IDictionary<string, string[]> errors)
        : base(400, "Validation Failed", "One or more validation errors occurred.", errors) { }
}

public static class AuthorizationPolicies
{
    public const string CanReviewGw1 = "CanReviewGw1";
    public const string CanDecideQualification = "CanDecideQualification";
    public const string CanRunQualificationMeeting = "CanRunQualificationMeeting";
    public const string CanBuildProposal = "CanBuildProposal";
    public const string CanReviewProposal = "CanReviewProposal";
    public const string CanApprove = "CanApprove";
    public const string CanSubmit = "CanSubmit";
    public const string CanManageContract = "CanManageContract";
    public const string CanAdminister = "CanAdminister";
    public const string CanCreateOpportunity = "CanCreateOpportunity";
    public const string CanRecordOutcome = "CanRecordOutcome";
    public const string CanAssignBuilder = "CanAssignBuilder";
    public const string CanManageMeeting = "CanManageMeeting";
    public const string CanManageScope = "CanManageScope";
    public const string CanManageCustomers = "CanManageCustomers";
    public const string CanViewPricing = "CanViewPricing";
    public const string CanEditPricing = "CanEditPricing";
    public const string CanManageBidBond = "CanManageBidBond";
    public const string CanManageSlResponses = "CanManageSlResponses";
    public const string CanRespondAsServiceLine = "CanRespondAsServiceLine";
    public const string CanViewAudit = "CanViewAudit";

    public static readonly IReadOnlyDictionary<string, string[]> PolicyRoles =
        new Dictionary<string, string[]>
        {
            [CanAssignBuilder] = [Domain.Common.RoleCodes.BidsPresales, Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.Admin],
            [CanManageMeeting] = [Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.BidsPresales, Domain.Common.RoleCodes.Admin],
            [CanManageScope] = [Domain.Common.RoleCodes.AM, Domain.Common.RoleCodes.BidsPresales, Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.Presales, Domain.Common.RoleCodes.Admin],
            [CanManageCustomers] = [Domain.Common.RoleCodes.AM, Domain.Common.RoleCodes.BidsPresales, Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.Admin],
            [CanViewPricing] = [Domain.Common.RoleCodes.BidsPresales, Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.Presales, Domain.Common.RoleCodes.Mgmt, Domain.Common.RoleCodes.Admin],
            [CanEditPricing] = [Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.Presales, Domain.Common.RoleCodes.Admin],
            [CanManageBidBond] = [Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.BidsPresales, Domain.Common.RoleCodes.Presales, Domain.Common.RoleCodes.Admin],
            [CanManageSlResponses] = [Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.Presales, Domain.Common.RoleCodes.Admin],
            [CanRespondAsServiceLine] = [Domain.Common.RoleCodes.Sl, Domain.Common.RoleCodes.Presales, Domain.Common.RoleCodes.Admin],
            [CanViewAudit] = [Domain.Common.RoleCodes.Mgmt, Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.Admin],
            [CanReviewGw1] = [Domain.Common.RoleCodes.BidsPresales, Domain.Common.RoleCodes.Admin],
            [CanDecideQualification] = [Domain.Common.RoleCodes.Sl, Domain.Common.RoleCodes.Presales, Domain.Common.RoleCodes.Admin],
            [CanRunQualificationMeeting] = [Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.Admin],
            [CanBuildProposal] = [Domain.Common.RoleCodes.Presales, Domain.Common.RoleCodes.Sl, Domain.Common.RoleCodes.Admin],
            [CanReviewProposal] = [Domain.Common.RoleCodes.Presales, Domain.Common.RoleCodes.Admin],
            [CanApprove] = [Domain.Common.RoleCodes.Mgmt, Domain.Common.RoleCodes.Admin],
            [CanSubmit] = [Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.Admin],
            [CanManageContract] = [Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.Mgmt, Domain.Common.RoleCodes.Admin],
            [CanAdminister] = [Domain.Common.RoleCodes.Admin],
            [CanCreateOpportunity] = [Domain.Common.RoleCodes.AM, Domain.Common.RoleCodes.BidsPresales, Domain.Common.RoleCodes.Admin],
            [CanRecordOutcome] = [Domain.Common.RoleCodes.BidsMgmt, Domain.Common.RoleCodes.AM, Domain.Common.RoleCodes.Admin]
        };
}
