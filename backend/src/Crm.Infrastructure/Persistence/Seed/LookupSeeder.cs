using Crm.Domain.Common;
using Crm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Persistence.Seed;

public static class LookupSeeder
{
    public static void Seed(ModelBuilder? modelBuilder, AppDbContext? db)
    {
        var roles = new[]
        {
            R(SeedIds.RoleAm, RoleCodes.AM, "Account Manager", "مدير الحساب"),
            R(SeedIds.RoleBidsPresales, RoleCodes.BidsPresales, "Bids & Presales", "العطاءات والمبيعات المسبقة"),
            R(SeedIds.RoleBidsMgmt, RoleCodes.BidsMgmt, "Bids Management", "إدارة العطاءات"),
            R(SeedIds.RolePresales, RoleCodes.Presales, "Presales", "المبيعات المسبقة"),
            R(SeedIds.RoleSl, RoleCodes.Sl, "Service Line", "خط الخدمة"),
            R(SeedIds.RoleMgmt, RoleCodes.Mgmt, "Management", "الإدارة"),
            R(SeedIds.RoleAdmin, RoleCodes.Admin, "Administrator", "مدير النظام")
        };

        var stages = new[]
        {
            new Stage { Id = SeedIds.StageQualification, Code = StageCodes.Qualification, NameEn = "Qualification", NameAr = "التأهيل", SortOrder = 1, IsActive = true },
            new Stage { Id = SeedIds.StageResponseDev, Code = StageCodes.ResponseDevelopment, NameEn = "Response Development", NameAr = "تطوير الرد", SortOrder = 2, IsActive = true },
            new Stage { Id = SeedIds.StageSubmission, Code = StageCodes.Submission, NameEn = "Submission", NameAr = "التقديم", SortOrder = 3, IsActive = true },
            new Stage { Id = SeedIds.StageContracting, Code = StageCodes.Contracting, NameEn = "Contracting", NameAr = "التعاقد", SortOrder = 4, IsActive = true }
        };

        var statuses = new[]
        {
            S(SeedIds.StAwaitingAssessment, SeedIds.StageQualification, StatusCodes.AwaitingAssessment, "Awaiting Assessment", "بانتظار التقييم", 1, false),
            S(SeedIds.StQualified, SeedIds.StageQualification, StatusCodes.Qualified, "Qualified", "مؤهل", 2, false),
            S(SeedIds.StNotQualified, SeedIds.StageQualification, StatusCodes.NotQualified, "Not Qualified", "غير مؤهل", 3, true),
            S(SeedIds.StQualCanceled, SeedIds.StageQualification, StatusCodes.Canceled, "Canceled", "ملغى", 4, true),
            S(SeedIds.StInProgress, SeedIds.StageResponseDev, StatusCodes.InProgress, "In Progress", "قيد التنفيذ", 1, false),
            S(SeedIds.StInternalReview, SeedIds.StageResponseDev, StatusCodes.InternalReview, "Internal Review", "مراجعة داخلية", 2, false),
            S(SeedIds.StHold, SeedIds.StageResponseDev, StatusCodes.Hold, "Hold", "معلق", 3, false),
            S(SeedIds.StRdCanceled, SeedIds.StageResponseDev, StatusCodes.Canceled, "Canceled", "ملغى", 4, true),
            S(SeedIds.StNotApproved, SeedIds.StageSubmission, StatusCodes.NotApproved, "Not Approved", "غير معتمد", 1, false),
            S(SeedIds.StSubmitted, SeedIds.StageSubmission, StatusCodes.Submitted, "Submitted", "تم التقديم", 2, false),
            S(SeedIds.StLost, SeedIds.StageSubmission, StatusCodes.Lost, "Lost", "خاسر", 3, true),
            S(SeedIds.StWon, SeedIds.StageContracting, StatusCodes.Won, "Won", "فائز", 1, false),
            S(SeedIds.StContractNegotiation, SeedIds.StageContracting, StatusCodes.ContractNegotiation, "Contract Negotiation", "تفاوض العقد", 2, false),
            S(SeedIds.StContractSigned, SeedIds.StageContracting, StatusCodes.ContractSigned, "Contract Signed", "عقد موقّع", 3, true)
        };

        var gates = new[]
        {
            G(SeedIds.GateGw1, GateCodes.Gw1Review, "1st Gateway Review", "مراجعة البوابة الأولى", 1, RoleCodes.BidsPresales, "Approve,Reject", true, false),
            G(SeedIds.GateQualDecision, GateCodes.QualDecision, "Service Line Qualification", "تأهيل خط الخدمة", 2, $"{RoleCodes.Sl}|{RoleCodes.Presales}", "Qualified,NotQualified", true, false),
            G(SeedIds.GateQualMeeting, GateCodes.QualMeeting, "Qualification Meeting Outcome", "نتيجة اجتماع التأهيل", 3, RoleCodes.BidsMgmt, "Passed,NotPassed", true, false),
            G(SeedIds.GateProposalReview, GateCodes.ProposalReview, "Proposal Review", "مراجعة العرض", 4, RoleCodes.Presales, "Approve,Return", true, false),
            G(SeedIds.GateMgmtApproval, GateCodes.MgmtApproval, "Management Approval", "اعتماد الإدارة", 5, RoleCodes.Mgmt, "Approve,Reject", true, false),
            G(SeedIds.GateContractSignoff, GateCodes.ContractSignoff, "Contract Sign-off", "اعتماد العقد", 6, RoleCodes.Mgmt, "Approve,Reject", true, false)
        };

        // Legal transitions. RequiredRoleCode may be comma-separated.
        var anyBids = $"{RoleCodes.BidsPresales},{RoleCodes.BidsMgmt},{RoleCodes.Admin}";
        var transitions = new List<StatusTransition>
        {
            T(SeedIds.StAwaitingAssessment, SeedIds.StNotQualified, RoleCodes.BidsPresales, true),
            T(SeedIds.StAwaitingAssessment, SeedIds.StQualified, RoleCodes.BidsPresales, false),
            T(SeedIds.StAwaitingAssessment, SeedIds.StQualCanceled, anyBids, true),
            T(SeedIds.StAwaitingAssessment, SeedIds.StHold, anyBids, true),
            T(SeedIds.StQualified, SeedIds.StInProgress, anyBids, false),
            T(SeedIds.StQualified, SeedIds.StQualCanceled, anyBids, true),
            T(SeedIds.StQualified, SeedIds.StHold, anyBids, true),
            T(SeedIds.StInProgress, SeedIds.StInternalReview, $"{RoleCodes.Presales},{RoleCodes.Sl},{RoleCodes.Admin}", false),
            T(SeedIds.StInProgress, SeedIds.StHold, anyBids, true),
            T(SeedIds.StInProgress, SeedIds.StRdCanceled, anyBids, true),
            T(SeedIds.StInternalReview, SeedIds.StInProgress, $"{RoleCodes.Presales},{RoleCodes.Admin}", true),
            T(SeedIds.StInternalReview, SeedIds.StSubmitted, RoleCodes.BidsMgmt, false),
            T(SeedIds.StInternalReview, SeedIds.StNotApproved, RoleCodes.Mgmt, true),
            T(SeedIds.StInternalReview, SeedIds.StHold, anyBids, true),
            T(SeedIds.StInternalReview, SeedIds.StRdCanceled, anyBids, true),
            T(SeedIds.StHold, SeedIds.StInProgress, anyBids, false),
            T(SeedIds.StHold, SeedIds.StAwaitingAssessment, anyBids, false),
            T(SeedIds.StHold, SeedIds.StQualified, anyBids, false),
            T(SeedIds.StHold, SeedIds.StInternalReview, anyBids, false),
            T(SeedIds.StHold, SeedIds.StNotApproved, anyBids, false),
            T(SeedIds.StHold, SeedIds.StSubmitted, anyBids, false),
            T(SeedIds.StHold, SeedIds.StWon, anyBids, false),
            T(SeedIds.StHold, SeedIds.StContractNegotiation, anyBids, false),
            T(SeedIds.StHold, SeedIds.StRdCanceled, anyBids, true),
            T(SeedIds.StNotApproved, SeedIds.StInProgress, $"{RoleCodes.Presales},{RoleCodes.BidsMgmt},{RoleCodes.Admin}", false),
            T(SeedIds.StNotApproved, SeedIds.StRdCanceled, anyBids, true),
            T(SeedIds.StSubmitted, SeedIds.StLost, $"{RoleCodes.BidsMgmt},{RoleCodes.AM},{RoleCodes.Admin}", true),
            T(SeedIds.StSubmitted, SeedIds.StWon, $"{RoleCodes.BidsMgmt},{RoleCodes.AM},{RoleCodes.Admin}", false),
            T(SeedIds.StSubmitted, SeedIds.StHold, anyBids, true),
            T(SeedIds.StWon, SeedIds.StContractNegotiation, $"{RoleCodes.BidsMgmt},{RoleCodes.Mgmt},{RoleCodes.Admin}", false),
            T(SeedIds.StContractNegotiation, SeedIds.StContractSigned, RoleCodes.Mgmt, false),
            T(SeedIds.StWon, SeedIds.StHold, anyBids, true),
            T(SeedIds.StContractNegotiation, SeedIds.StHold, anyBids, true)
        };

        if (modelBuilder is not null)
        {
            modelBuilder.Entity<Role>().HasData(roles);
            modelBuilder.Entity<Stage>().HasData(stages);
            modelBuilder.Entity<Status>().HasData(statuses);
            modelBuilder.Entity<WorkflowGate>().HasData(gates);
            modelBuilder.Entity<StatusTransition>().HasData(transitions);
        }
        else if (db is not null)
        {
            if (!db.Roles.Any()) db.Roles.AddRange(roles);
            if (!db.Stages.Any()) db.Stages.AddRange(stages);
            if (!db.Statuses.Any()) db.Statuses.AddRange(statuses);
            if (!db.WorkflowGates.Any()) db.WorkflowGates.AddRange(gates);
            if (!db.StatusTransitions.Any()) db.StatusTransitions.AddRange(transitions);
        }
    }

    private static Role R(Guid id, string code, string en, string ar) =>
        new() { Id = id, Code = code, NameEn = en, NameAr = ar };

    private static Status S(Guid id, Guid stageId, string code, string en, string ar, int order, bool terminal) =>
        new() { Id = id, StageId = stageId, Code = code, NameEn = en, NameAr = ar, SortOrder = order, IsTerminal = terminal, IsActive = true };

    private static WorkflowGate G(Guid id, string code, string en, string ar, int order, string role, string decisions, bool reasonOnReject, bool attachOnApprove) =>
        new()
        {
            Id = id, Code = code, NameEn = en, NameAr = ar, SortOrder = order,
            ResponsibleRoleCode = role, AllowedDecisions = decisions,
            RequiresReasonOnReject = reasonOnReject, RequiresAttachmentOnApprove = attachOnApprove, IsActive = true
        };

    private static StatusTransition T(Guid from, Guid to, string role, bool reason) =>
        new() { Id = Guid.NewGuid(), FromStatusId = from, ToStatusId = to, RequiredRoleCode = role, RequiresReason = reason };
}
