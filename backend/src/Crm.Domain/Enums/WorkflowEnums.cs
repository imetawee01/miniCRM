namespace Crm.Domain.Enums;

public enum GateState
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Returned = 4,
    Skipped = 5
}

public enum OwnerEntityType
{
    Opportunity = 1,
    GateInstance = 2,
    ScopeItem = 3,
    Contract = 4
}

public enum NoteVisibility
{
    Internal = 1,
    Shared = 2
}

public enum ActivityType
{
    Call = 1,
    Meeting = 2,
    Todo = 3,
    FollowUp = 4
}

public enum AttachmentCategory
{
    RFP = 1,
    TechnicalProposal = 2,
    Costing = 3,
    BidBond = 4,
    Contract = 5,
    Other = 6
}

public enum BidBondStatus
{
    NotRequired = 1,
    Requested = 2,
    Issued = 3,
    Rejected = 4
}

public enum MeetingOutcome
{
    Pending = 1,
    Passed = 2,
    NotPassed = 3
}

public enum AttendeeResponse
{
    Invited = 1,
    Accepted = 2,
    Declined = 3,
    Attended = 4
}

public enum SlResponseStatus
{
    Pending = 1,
    Submitted = 2,
    Returned = 3,
    Accepted = 4
}

public enum SubmissionChannel
{
    Etimad = 1,
    Email = 2,
    HandDelivery = 3,
    Portal = 4,
    Other = 5
}

public enum OutcomeResult
{
    Won = 1,
    Lost = 2
}

public enum ContractStatus
{
    Won = 1,
    ContractNegotiation = 2,
    ContractSigned = 3
}

public enum NegotiationRoundStatus
{
    Open = 1,
    Agreed = 2,
    Rejected = 3
}

public enum GeneratedEmailStatus
{
    Draft = 1,
    Queued = 2,
    Sent = 3,
    Failed = 4
}

public enum NotificationType
{
    Info = 1,
    GateAssigned = 2,
    EmailGenerated = 3,
    Mention = 4,
    Deadline = 5
}
