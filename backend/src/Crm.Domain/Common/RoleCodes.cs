namespace Crm.Domain.Common;

public static class RoleCodes
{
    public const string AM = "AM";
    public const string BidsPresales = "BIDS_PRESALES";
    public const string BidsMgmt = "BIDS_MGMT";
    public const string Presales = "PRESALES";
    public const string Sl = "SL";
    public const string Mgmt = "MGMT";
    public const string Admin = "ADMIN";

    public static readonly string[] All =
    [
        AM, BidsPresales, BidsMgmt, Presales, Sl, Mgmt, Admin
    ];
}

public static class GateCodes
{
    public const string Gw1Review = "GW1_REVIEW";
    public const string QualDecision = "QUAL_DECISION";
    public const string QualMeeting = "QUAL_MEETING";
    public const string ProposalReview = "PROPOSAL_REVIEW";
    public const string MgmtApproval = "MGMT_APPROVAL";
    public const string ContractSignoff = "CONTRACT_SIGNOFF";
}

public static class EmailTemplateCodes
{
    public const string OppReceived = "OPP_RECEIVED";
    public const string Gw1Rejected = "GW1_REJECTED";
    public const string QualRequest = "QUAL_REQUEST";
    public const string QualMeetingRequest = "QUAL_MEETING_REQUEST";
    public const string BuilderNotification = "BUILDER_NOTIFICATION";
    public const string ApprovalRequest = "APPROVAL_REQUEST";
}

public static class StageCodes
{
    public const string Qualification = "QUALIFICATION";
    public const string ResponseDevelopment = "RESPONSE_DEVELOPMENT";
    public const string Submission = "SUBMISSION";
    public const string Contracting = "CONTRACTING";
}

public static class StatusCodes
{
    public const string AwaitingAssessment = "AWAITING_ASSESSMENT";
    public const string Qualified = "QUALIFIED";
    public const string NotQualified = "NOT_QUALIFIED";
    public const string Canceled = "CANCELED";
    public const string InProgress = "IN_PROGRESS";
    public const string InternalReview = "INTERNAL_REVIEW";
    public const string Hold = "HOLD";
    public const string NotApproved = "NOT_APPROVED";
    public const string Submitted = "SUBMITTED";
    public const string Lost = "LOST";
    public const string Won = "WON";
    public const string ContractNegotiation = "CONTRACT_NEGOTIATION";
    public const string ContractSigned = "CONTRACT_SIGNED";
}
