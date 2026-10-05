namespace Crm.Infrastructure.Persistence.Seed;

/// <summary>Deterministic GUIDs so seed data and tests stay stable across environments.</summary>
public static class SeedIds
{
    // Roles
    public static readonly Guid RoleAm = Guid.Parse("a0000001-0000-0000-0000-000000000001");
    public static readonly Guid RoleBidsPresales = Guid.Parse("a0000001-0000-0000-0000-000000000002");
    public static readonly Guid RoleBidsMgmt = Guid.Parse("a0000001-0000-0000-0000-000000000003");
    public static readonly Guid RolePresales = Guid.Parse("a0000001-0000-0000-0000-000000000004");
    public static readonly Guid RoleSl = Guid.Parse("a0000001-0000-0000-0000-000000000005");
    public static readonly Guid RoleMgmt = Guid.Parse("a0000001-0000-0000-0000-000000000006");
    public static readonly Guid RoleAdmin = Guid.Parse("a0000001-0000-0000-0000-000000000007");

    // Users
    public static readonly Guid UserAmSara = Guid.Parse("b0000001-0000-0000-0000-000000000001");
    public static readonly Guid UserBidsPresales = Guid.Parse("b0000001-0000-0000-0000-000000000002");
    public static readonly Guid UserBidsMgmt = Guid.Parse("b0000001-0000-0000-0000-000000000003");
    public static readonly Guid UserPresalesOmar = Guid.Parse("b0000001-0000-0000-0000-000000000004");
    public static readonly Guid UserSlNoura = Guid.Parse("b0000001-0000-0000-0000-000000000005");
    public static readonly Guid UserMgmtFahad = Guid.Parse("b0000001-0000-0000-0000-000000000006");
    public static readonly Guid UserAdmin = Guid.Parse("b0000001-0000-0000-0000-000000000007");
    public static readonly Guid UserAmKhalid = Guid.Parse("b0000001-0000-0000-0000-000000000008");
    public static readonly Guid UserSlYousef = Guid.Parse("b0000001-0000-0000-0000-000000000009");
    public static readonly Guid UserBidsLayla = Guid.Parse("b0000001-0000-0000-0000-000000000010");

    // Customers
    public static readonly Guid CustMoi = Guid.Parse("c0000001-0000-0000-0000-000000000001");
    public static readonly Guid CustStc = Guid.Parse("c0000001-0000-0000-0000-000000000002");
    public static readonly Guid CustRajhi = Guid.Parse("c0000001-0000-0000-0000-000000000003");
    public static readonly Guid CustNeom = Guid.Parse("c0000001-0000-0000-0000-000000000004");
    public static readonly Guid CustSec = Guid.Parse("c0000001-0000-0000-0000-000000000005");
    public static readonly Guid CustElm = Guid.Parse("c0000001-0000-0000-0000-000000000006");

    // Service lines
    public static readonly Guid SlCyber = Guid.Parse("d0000001-0000-0000-0000-000000000001");
    public static readonly Guid SlDigital = Guid.Parse("d0000001-0000-0000-0000-000000000002");
    public static readonly Guid SlCloud = Guid.Parse("d0000001-0000-0000-0000-000000000003");
    public static readonly Guid SlDataAi = Guid.Parse("d0000001-0000-0000-0000-000000000004");
    public static readonly Guid SlManaged = Guid.Parse("d0000001-0000-0000-0000-000000000005");

    // Stages
    public static readonly Guid StageQualification = Guid.Parse("e0000001-0000-0000-0000-000000000001");
    public static readonly Guid StageResponseDev = Guid.Parse("e0000001-0000-0000-0000-000000000002");
    public static readonly Guid StageSubmission = Guid.Parse("e0000001-0000-0000-0000-000000000003");
    public static readonly Guid StageContracting = Guid.Parse("e0000001-0000-0000-0000-000000000004");

    // Statuses
    public static readonly Guid StAwaitingAssessment = Guid.Parse("e0000002-0000-0000-0000-000000000001");
    public static readonly Guid StQualified = Guid.Parse("e0000002-0000-0000-0000-000000000002");
    public static readonly Guid StNotQualified = Guid.Parse("e0000002-0000-0000-0000-000000000003");
    public static readonly Guid StQualCanceled = Guid.Parse("e0000002-0000-0000-0000-000000000004");
    public static readonly Guid StInProgress = Guid.Parse("e0000002-0000-0000-0000-000000000005");
    public static readonly Guid StInternalReview = Guid.Parse("e0000002-0000-0000-0000-000000000006");
    public static readonly Guid StHold = Guid.Parse("e0000002-0000-0000-0000-000000000007");
    public static readonly Guid StRdCanceled = Guid.Parse("e0000002-0000-0000-0000-000000000008");
    public static readonly Guid StNotApproved = Guid.Parse("e0000002-0000-0000-0000-000000000009");
    public static readonly Guid StSubmitted = Guid.Parse("e0000002-0000-0000-0000-000000000010");
    public static readonly Guid StLost = Guid.Parse("e0000002-0000-0000-0000-000000000011");
    public static readonly Guid StWon = Guid.Parse("e0000002-0000-0000-0000-000000000012");
    public static readonly Guid StContractNegotiation = Guid.Parse("e0000002-0000-0000-0000-000000000013");
    public static readonly Guid StContractSigned = Guid.Parse("e0000002-0000-0000-0000-000000000014");

    // Gates
    public static readonly Guid GateGw1 = Guid.Parse("f0000001-0000-0000-0000-000000000001");
    public static readonly Guid GateQualDecision = Guid.Parse("f0000001-0000-0000-0000-000000000002");
    public static readonly Guid GateQualMeeting = Guid.Parse("f0000001-0000-0000-0000-000000000003");
    public static readonly Guid GateProposalReview = Guid.Parse("f0000001-0000-0000-0000-000000000004");
    public static readonly Guid GateMgmtApproval = Guid.Parse("f0000001-0000-0000-0000-000000000005");
    public static readonly Guid GateContractSignoff = Guid.Parse("f0000001-0000-0000-0000-000000000006");

    // Opportunities OPP-2026-00001 .. 00015
    public static readonly Guid Opp1 = Guid.Parse("11000001-0000-0000-0000-000000000001");
    public static readonly Guid Opp2 = Guid.Parse("11000001-0000-0000-0000-000000000002");
    public static readonly Guid Opp3 = Guid.Parse("11000001-0000-0000-0000-000000000003");
    public static readonly Guid Opp4 = Guid.Parse("11000001-0000-0000-0000-000000000004");
    public static readonly Guid Opp5 = Guid.Parse("11000001-0000-0000-0000-000000000005");
    public static readonly Guid Opp6 = Guid.Parse("11000001-0000-0000-0000-000000000006");
    public static readonly Guid Opp7 = Guid.Parse("11000001-0000-0000-0000-000000000007");
    public static readonly Guid Opp8 = Guid.Parse("11000001-0000-0000-0000-000000000008");
    public static readonly Guid Opp9 = Guid.Parse("11000001-0000-0000-0000-000000000009");
    public static readonly Guid Opp10 = Guid.Parse("11000001-0000-0000-0000-000000000010");
    public static readonly Guid Opp11 = Guid.Parse("11000001-0000-0000-0000-000000000011");
    public static readonly Guid Opp12 = Guid.Parse("11000001-0000-0000-0000-000000000012");
    public static readonly Guid Opp13 = Guid.Parse("11000001-0000-0000-0000-000000000013");
    public static readonly Guid Opp14 = Guid.Parse("11000001-0000-0000-0000-000000000014");
    public static readonly Guid Opp15 = Guid.Parse("11000001-0000-0000-0000-000000000015");
}
