using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Persistence.Seed;

public static class DemoSeeder
{
    public const string DevPassword = "Passw0rd!";

    public static void SeedUsersAndLookups(AppDbContext db)
    {
        var hasher = new PasswordHasher<User>();

        if (!db.Users.IgnoreQueryFilters().Any())
        {
            var users = new[]
            {
                U(SeedIds.UserAmSara, "am.sara@crm.local", "Sara Al-Harbi", "Account Manager"),
                U(SeedIds.UserBidsPresales, "bids.presales@crm.local", "Majed Al-Qahtani", "Bids & Presales Lead"),
                U(SeedIds.UserBidsMgmt, "bids.mgmt@crm.local", "Reem Al-Saud", "Bids Manager"),
                U(SeedIds.UserPresalesOmar, "presales.omar@crm.local", "Omar Al-Ghamdi", "Presales Consultant"),
                U(SeedIds.UserSlNoura, "sl.noura@crm.local", "Noura Al-Otaibi", "Service Line Lead"),
                U(SeedIds.UserMgmtFahad, "mgmt.fahad@crm.local", "Fahad Al-Mutairi", "Management"),
                U(SeedIds.UserAdmin, "admin@crm.local", "System Administrator", "Administrator"),
                U(SeedIds.UserAmKhalid, "am.khalid@crm.local", "Khalid Al-Dossari", "Account Manager"),
                U(SeedIds.UserSlYousef, "sl.yousef@crm.local", "Yousef Al-Shammari", "Presales / SL"),
                U(SeedIds.UserBidsLayla, "bids.layla@crm.local", "Layla Al-Anazi", "Bids Specialist")
            };
            foreach (var user in users)
                user.PasswordHash = hasher.HashPassword(user, DevPassword);
            db.Users.AddRange(users);

            db.UserRoles.AddRange(
                UR(SeedIds.UserAmSara, SeedIds.RoleAm),
                UR(SeedIds.UserBidsPresales, SeedIds.RoleBidsPresales),
                UR(SeedIds.UserBidsMgmt, SeedIds.RoleBidsMgmt),
                UR(SeedIds.UserPresalesOmar, SeedIds.RolePresales),
                UR(SeedIds.UserSlNoura, SeedIds.RoleSl),
                UR(SeedIds.UserMgmtFahad, SeedIds.RoleMgmt),
                UR(SeedIds.UserAdmin, SeedIds.RoleAdmin),
                UR(SeedIds.UserAmKhalid, SeedIds.RoleAm),
                UR(SeedIds.UserSlYousef, SeedIds.RoleSl),
                UR(SeedIds.UserSlYousef, SeedIds.RolePresales),
                UR(SeedIds.UserBidsLayla, SeedIds.RoleBidsPresales),
                UR(SeedIds.UserBidsLayla, SeedIds.RoleBidsMgmt)
            );
        }

        if (!db.Customers.IgnoreQueryFilters().Any())
        {
            db.Customers.AddRange(
                C(SeedIds.CustMoi, "Ministry of Interior", "وزارة الداخلية", "Government", true),
                C(SeedIds.CustStc, "stc", "شركة الاتصالات السعودية", "Telecom", false),
                C(SeedIds.CustRajhi, "Al Rajhi Bank", "مصرف الراجحي", "Banking", false),
                C(SeedIds.CustNeom, "NEOM", "نيوم", "Giga-project", true),
                C(SeedIds.CustSec, "Saudi Electricity Company", "الشركة السعودية للكهرباء", "Energy", true),
                C(SeedIds.CustElm, "Elm Company", "شركة علم", "Technology", true)
            );
            db.CustomerContacts.AddRange(
                new CustomerContact { Id = Guid.Parse("c1000001-0000-0000-0000-000000000001"), CustomerId = SeedIds.CustMoi, Name = "Abdullah Al-Harbi", Email = "abdullah@moi.gov.sa", Title = "Procurement Lead", IsPrimary = true },
                new CustomerContact { Id = Guid.Parse("c1000001-0000-0000-0000-000000000002"), CustomerId = SeedIds.CustStc, Name = "Maha Al-Faisal", Email = "maha@stc.com.sa", Title = "Category Manager", IsPrimary = true }
            );
        }

        if (!db.ServiceLines.Any())
        {
            db.ServiceLines.AddRange(
                new ServiceLine { Id = SeedIds.SlCyber, Code = "CYBER", NameEn = "Cybersecurity", NameAr = "الأمن السيبراني", LeadUserId = SeedIds.UserSlNoura, SortOrder = 1, ColourHex = "#0ea5e9" },
                new ServiceLine { Id = SeedIds.SlDigital, Code = "DIGITAL", NameEn = "Digital Transformation", NameAr = "التحول الرقمي", LeadUserId = SeedIds.UserSlYousef, SortOrder = 2, ColourHex = "#8b5cf6" },
                new ServiceLine { Id = SeedIds.SlCloud, Code = "CLOUD", NameEn = "Cloud", NameAr = "السحابة", LeadUserId = SeedIds.UserPresalesOmar, SortOrder = 3, ColourHex = "#14b8a6" },
                new ServiceLine { Id = SeedIds.SlDataAi, Code = "DATA_AI", NameEn = "Data & AI", NameAr = "البيانات والذكاء الاصطناعي", LeadUserId = SeedIds.UserSlNoura, SortOrder = 4, ColourHex = "#f59e0b" },
                new ServiceLine { Id = SeedIds.SlManaged, Code = "MANAGED", NameEn = "Managed Services", NameAr = "الخدمات المدارة", LeadUserId = SeedIds.UserSlYousef, SortOrder = 5, ColourHex = "#ef4444" }
            );
        }

        if (!db.EmailTemplates.Any())
            db.EmailTemplates.AddRange(EmailTemplateSeeder.All());

        // AM team (shared pipeline visibility) + SL ↔ service-line mappings
        if (!db.Teams.Any())
        {
            var teamId = Guid.Parse("e0000001-0000-0000-0000-000000000001");
            db.Teams.Add(new Team { Id = teamId, Name = "Account Managers — Central", LeadUserId = SeedIds.UserAmSara });
            db.TeamMembers.AddRange(
                new TeamMember { TeamId = teamId, UserId = SeedIds.UserAmSara },
                new TeamMember { TeamId = teamId, UserId = SeedIds.UserAmKhalid });
        }

        if (!db.UserServiceLines.Any())
        {
            db.UserServiceLines.AddRange(
                new UserServiceLine { UserId = SeedIds.UserSlNoura, ServiceLineId = SeedIds.SlCyber },
                new UserServiceLine { UserId = SeedIds.UserSlNoura, ServiceLineId = SeedIds.SlDataAi },
                new UserServiceLine { UserId = SeedIds.UserSlYousef, ServiceLineId = SeedIds.SlDigital },
                new UserServiceLine { UserId = SeedIds.UserSlYousef, ServiceLineId = SeedIds.SlManaged },
                new UserServiceLine { UserId = SeedIds.UserSlYousef, ServiceLineId = SeedIds.SlCloud });
        }
    }

    public static void SeedOpportunities(AppDbContext db)
    {
        if (db.Opportunities.IgnoreQueryFilters().Any()) return;

        db.OpportunityNumberSequences.Add(new OpportunityNumberSequence { Year = 2026, LastValue = 15 });
        db.ContractNumberSequences.Add(new ContractNumberSequence { Year = 2026, LastValue = 3 });

        var now = new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);

        Opportunity Opp(Guid id, int n, string name, Guid customer, Guid status, Guid stage, Guid am, decimal value,
            SourceChannel src, SubmissionTheme theme, bool closed = false, BuilderType? builder = null, Guid? builderUser = null,
            bool? meeting = null)
        {
            var o = new Opportunity
            {
                Id = id,
                OpportunityNumber = $"OPP-2026-{n:00000}",
                Name = name,
                CustomerId = customer,
                SourceChannel = src,
                SubmissionTheme = theme,
                EngagementType = n % 2 == 0 ? EngagementType.Proactive : EngagementType.Reactive,
                OpportunityType = n % 3 == 0 ? OpportunityType.Renewal : OpportunityType.New,
                ExpectedValueSar = value,
                RelationWithClientScore = 2 + (n % 4),
                WinProbabilityScore = 2 + ((n + 1) % 4),
                DurationMonths = 12 + n,
                ProposalLanguage = n % 2 == 0 ? ProposalLanguage.Arabic : ProposalLanguage.Bilingual,
                StageId = stage,
                StatusId = status,
                SubmittedByUserId = am,
                OwnerUserId = builderUser ?? am,
                BuilderType = builder,
                BuilderUserId = builderUser,
                RequiresQualificationMeeting = meeting,
                RequiresBidBond = n % 4 == 0,
                IsClosed = closed,
                ClosedAtUtc = closed ? now.AddDays(-2) : null,
                CreatedAtUtc = now.AddDays(-30 + n),
                CreatedByUserId = am
            };
            o.Deadlines = new OpportunityDeadlines
            {
                QualificationDeadline = now.AddDays(5),
                InquiriesDeadline = now.AddDays(10),
                EstimatedCostDeadline = now.AddDays(15),
                InternalDeadline = now.AddDays(20),
                SubmissionDeadline = now.AddDays(25)
            };
            return o;
        }

        var opps = new List<Opportunity>
        {
            Opp(SeedIds.Opp1, 1, "MOI SOC Expansion", SeedIds.CustMoi, SeedIds.StAwaitingAssessment, SeedIds.StageQualification, SeedIds.UserAmSara, 4_500_000, SourceChannel.DirectClient, SubmissionTheme.Emdad),
            Opp(SeedIds.Opp2, 2, "stc Cloud Landing Zone", SeedIds.CustStc, SeedIds.StAwaitingAssessment, SeedIds.StageQualification, SeedIds.UserAmKhalid, 2_100_000, SourceChannel.AccountExpansion, SubmissionTheme.Elm),
            Opp(SeedIds.Opp3, 3, "Al Rajhi Fraud Analytics", SeedIds.CustRajhi, SeedIds.StAwaitingAssessment, SeedIds.StageQualification, SeedIds.UserAmSara, 3_250_000, SourceChannel.Referral, SubmissionTheme.ThiqahBusinessSolutions),
            Opp(SeedIds.Opp4, 4, "NEOM Identity Platform", SeedIds.CustNeom, SeedIds.StQualified, SeedIds.StageQualification, SeedIds.UserAmSara, 8_000_000, SourceChannel.Etimad, SubmissionTheme.Ahad, builder: BuilderType.Presales, builderUser: SeedIds.UserPresalesOmar, meeting: false),
            Opp(SeedIds.Opp5, 5, "SEC OT Security Assessment", SeedIds.CustSec, SeedIds.StInProgress, SeedIds.StageResponseDev, SeedIds.UserAmKhalid, 1_750_000, SourceChannel.Partner, SubmissionTheme.Emdad, builder: BuilderType.ServiceLine, builderUser: SeedIds.UserSlNoura, meeting: true),
            Opp(SeedIds.Opp6, 6, "Elm Data Platform Phase 2", SeedIds.CustElm, SeedIds.StInProgress, SeedIds.StageResponseDev, SeedIds.UserAmSara, 5_400_000, SourceChannel.AccountExpansion, SubmissionTheme.Elm, builder: BuilderType.Presales, builderUser: SeedIds.UserPresalesOmar),
            Opp(SeedIds.Opp7, 7, "MOI Endpoint Protection", SeedIds.CustMoi, SeedIds.StInProgress, SeedIds.StageResponseDev, SeedIds.UserAmKhalid, 2_800_000, SourceChannel.DirectClient, SubmissionTheme.Emdad, builder: BuilderType.ServiceLine, builderUser: SeedIds.UserSlYousef),
            Opp(SeedIds.Opp8, 8, "stc SOC as a Service", SeedIds.CustStc, SeedIds.StInternalReview, SeedIds.StageResponseDev, SeedIds.UserAmSara, 6_200_000, SourceChannel.Etimad, SubmissionTheme.Elm, builder: BuilderType.Presales, builderUser: SeedIds.UserPresalesOmar),
            Opp(SeedIds.Opp9, 9, "NEOM Smart City Network", SeedIds.CustNeom, SeedIds.StHold, SeedIds.StageResponseDev, SeedIds.UserAmKhalid, 12_000_000, SourceChannel.DirectClient, SubmissionTheme.Ahad, builder: BuilderType.Presales, builderUser: SeedIds.UserPresalesOmar),
            Opp(SeedIds.Opp10, 10, "Al Rajhi Managed SOC", SeedIds.CustRajhi, SeedIds.StSubmitted, SeedIds.StageSubmission, SeedIds.UserAmSara, 4_100_000, SourceChannel.Referral, SubmissionTheme.ThiqahBusinessSolutions, builder: BuilderType.Presales, builderUser: SeedIds.UserPresalesOmar),
            Opp(SeedIds.Opp11, 11, "SEC AMI Analytics", SeedIds.CustSec, SeedIds.StLost, SeedIds.StageSubmission, SeedIds.UserAmKhalid, 1_200_000, SourceChannel.Etimad, SubmissionTheme.Emdad, closed: true, builder: BuilderType.ServiceLine, builderUser: SeedIds.UserSlNoura),
            Opp(SeedIds.Opp12, 12, "stc Legacy SIEM Refresh", SeedIds.CustStc, SeedIds.StNotQualified, SeedIds.StageQualification, SeedIds.UserAmSara, 900_000, SourceChannel.Other, SubmissionTheme.Elm, closed: true),
            Opp(SeedIds.Opp13, 13, "MOI Privileged Access", SeedIds.CustMoi, SeedIds.StWon, SeedIds.StageContracting, SeedIds.UserAmSara, 3_600_000, SourceChannel.DirectClient, SubmissionTheme.Emdad, builder: BuilderType.Presales, builderUser: SeedIds.UserPresalesOmar),
            Opp(SeedIds.Opp14, 14, "Elm National KYC", SeedIds.CustElm, SeedIds.StContractNegotiation, SeedIds.StageContracting, SeedIds.UserAmKhalid, 7_500_000, SourceChannel.AccountExpansion, SubmissionTheme.Elm, builder: BuilderType.Presales, builderUser: SeedIds.UserPresalesOmar),
            Opp(SeedIds.Opp15, 15, "Al Rajhi Open Banking API", SeedIds.CustRajhi, SeedIds.StContractSigned, SeedIds.StageContracting, SeedIds.UserAmSara, 5_900_000, SourceChannel.Partner, SubmissionTheme.ThiqahBusinessSolutions, closed: true, builder: BuilderType.Presales, builderUser: SeedIds.UserPresalesOmar)
        };
        opps[11].SourceChannelOther = "Industry event";
        opps[8].HoldPriorStatusId = SeedIds.StInProgress;
        opps[8].HoldPriorStageId = SeedIds.StageResponseDev;
        opps[8].HoldReason = "Customer delayed RFP clarifications.";
        db.Opportunities.AddRange(opps);

        foreach (var o in opps)
        {
            db.ScopesOfWork.Add(new ScopeOfWork
            {
                Id = Guid.Parse($"12000001-0000-0000-0000-{o.OpportunityNumber[^5..].PadLeft(12, '0')}"),
                OpportunityId = o.Id,
                Brief = $"Deliver a complete technical proposal and costing for {o.Name}.",
                Items =
                [
                    new ScopeItem
                    {
                        Id = Guid.NewGuid(),
                        Title = "Core technical scope",
                        Description = "Architecture, implementation, and knowledge transfer.",
                        ServiceLineId = SeedIds.SlCyber,
                        AssignedUserId = SeedIds.UserSlNoura,
                        SortOrder = 1,
                        Comment = "Primary SL"
                    }
                ]
            });
        }

        Gate(db, SeedIds.Opp1, SeedIds.GateGw1, GateState.Pending, RoleCodes.BidsPresales, 1, now.AddDays(-2));
        Gate(db, SeedIds.Opp2, SeedIds.GateGw1, GateState.Pending, RoleCodes.BidsPresales, 1, now.AddDays(-1));
        Gate(db, SeedIds.Opp3, SeedIds.GateGw1, GateState.Pending, RoleCodes.BidsPresales, 1, now);
        Gate(db, SeedIds.Opp4, SeedIds.GateGw1, GateState.Approved, RoleCodes.BidsPresales, 1, now.AddDays(-10), "Approve", SeedIds.UserBidsPresales);
        Gate(db, SeedIds.Opp4, SeedIds.GateQualDecision, GateState.Approved, RoleCodes.Sl, 1, now.AddDays(-8), "Qualified", SeedIds.UserSlNoura);
        Gate(db, SeedIds.Opp5, SeedIds.GateGw1, GateState.Approved, RoleCodes.BidsPresales, 1, now.AddDays(-12), "Approve", SeedIds.UserBidsPresales);
        Gate(db, SeedIds.Opp5, SeedIds.GateQualMeeting, GateState.Approved, RoleCodes.BidsMgmt, 1, now.AddDays(-9), "Passed", SeedIds.UserBidsMgmt);
        Gate(db, SeedIds.Opp8, SeedIds.GateProposalReview, GateState.Pending, RoleCodes.Presales, 1, now.AddDays(-1));
        Gate(db, SeedIds.Opp12, SeedIds.GateGw1, GateState.Rejected, RoleCodes.BidsPresales, 1, now.AddDays(-20), "Reject", SeedIds.UserBidsPresales, "Outside target sector and weak client relationship.");
        Gate(db, SeedIds.Opp14, SeedIds.GateContractSignoff, GateState.Pending, RoleCodes.Mgmt, 1, now.AddDays(-3));
        Gate(db, SeedIds.Opp15, SeedIds.GateContractSignoff, GateState.Approved, RoleCodes.Mgmt, 1, now.AddDays(-30), "Approve", SeedIds.UserMgmtFahad);

        db.BidBonds.Add(new BidBond { Id = Guid.NewGuid(), OpportunityId = SeedIds.Opp8, Required = true, Status = BidBondStatus.Requested, RequestedAtUtc = now.AddDays(-4) });
        db.BidBonds.Add(new BidBond { Id = Guid.NewGuid(), OpportunityId = SeedIds.Opp10, Required = true, Status = BidBondStatus.Issued, AmountSar = 120_000, IssuingBank = "SNB", ValidUntil = now.AddMonths(3), RequestedAtUtc = now.AddDays(-20), IssuedAtUtc = now.AddDays(-15) });

        db.EstimatedCosts.Add(new EstimatedCost { Id = Guid.NewGuid(), OpportunityId = SeedIds.Opp8, Amount = 4_100_000, SubmittedByUserId = SeedIds.UserPresalesOmar, SubmittedAtUtc = now.AddDays(-3), Notes = "Includes 12 months hypercare." });
        db.ProposalPricings.Add(new ProposalPricing { Id = Guid.NewGuid(), OpportunityId = SeedIds.Opp8, PriceSar = 6_200_000, CostSar = 4_100_000, MarginPercent = ProposalPricing.ComputeMargin(6_200_000, 4_100_000), Version = 1, IsCurrent = true, CreatedByUserId = SeedIds.UserPresalesOmar, CreatedAtUtc = now.AddDays(-2) });
        db.ProposalPricings.Add(new ProposalPricing { Id = Guid.NewGuid(), OpportunityId = SeedIds.Opp10, PriceSar = 4_100_000, CostSar = 2_700_000, MarginPercent = ProposalPricing.ComputeMargin(4_100_000, 2_700_000), Version = 1, IsCurrent = true, CreatedByUserId = SeedIds.UserPresalesOmar, CreatedAtUtc = now.AddDays(-18) });

        db.Submissions.Add(new Submission { Id = Guid.NewGuid(), OpportunityId = SeedIds.Opp10, SubmittedAtUtc = now.AddDays(-10), SubmittedByUserId = SeedIds.UserBidsMgmt, Channel = SubmissionChannel.Etimad, Reference = "ETM-88421" });
        db.OpportunityOutcomes.Add(new OpportunityOutcome { Id = Guid.NewGuid(), OpportunityId = SeedIds.Opp11, Result = OutcomeResult.Lost, AnnouncedAtUtc = now.AddDays(-5), CompetitorName = "Competitor X", LossReason = "Price", Notes = "Lost on commercial terms." });

        db.Contracts.AddRange(
            new Contract { Id = Guid.Parse("14000001-0000-0000-0000-000000000013"), OpportunityId = SeedIds.Opp13, ContractNumber = "CTR-2026-00001", ContractStatus = ContractStatus.Won, ContractValueSar = 3_600_000, CreatedAtUtc = now.AddDays(-7) },
            new Contract { Id = Guid.Parse("14000001-0000-0000-0000-000000000014"), OpportunityId = SeedIds.Opp14, ContractNumber = "CTR-2026-00002", ContractStatus = ContractStatus.ContractNegotiation, ContractValueSar = 7_500_000, CreatedAtUtc = now.AddDays(-14) },
            new Contract { Id = Guid.Parse("14000001-0000-0000-0000-000000000015"), OpportunityId = SeedIds.Opp15, ContractNumber = "CTR-2026-00003", ContractStatus = ContractStatus.ContractSigned, ContractValueSar = 5_900_000, SignedAtUtc = now.AddDays(-20), SignedByCustomerRepresentative = "Ibrahim Al-Rajhi", CreatedAtUtc = now.AddDays(-40) }
        );

        foreach (var oppId in new[] { SeedIds.Opp1, SeedIds.Opp5, SeedIds.Opp8, SeedIds.Opp10, SeedIds.Opp14 })
        {
            db.Notes.Add(new Note
            {
                Id = Guid.NewGuid(),
                EntityType = OwnerEntityType.Opportunity,
                EntityId = oppId,
                Body = "Initial qualification notes captured from the AM briefing.",
                Visibility = NoteVisibility.Shared,
                CreatedByUserId = SeedIds.UserBidsPresales,
                CreatedAtUtc = now.AddDays(-6)
            });
            db.Comments.Add(new Comment
            {
                Id = Guid.NewGuid(),
                EntityType = OwnerEntityType.Opportunity,
                EntityId = oppId,
                Body = "Please confirm the submission channel before the internal deadline.",
                CreatedByUserId = SeedIds.UserBidsMgmt,
                CreatedAtUtc = now.AddDays(-4)
            });
        }

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = "Opportunity",
            EntityId = SeedIds.Opp12,
            OpportunityId = SeedIds.Opp12,
            Action = AuditAction.GateRejected,
            ActorUserId = SeedIds.UserBidsPresales,
            ActorRoleCode = RoleCodes.BidsPresales,
            OccurredAtUtc = now.AddDays(-20),
            FromValue = "Awaiting Assessment",
            ToValue = "Not Qualified",
            Description = "GW1 rejected — outside target sector."
        });

        db.Notifications.Add(new AppNotification
        {
            Id = Guid.NewGuid(),
            UserId = SeedIds.UserBidsPresales,
            Type = NotificationType.GateAssigned,
            Title = "GW1 review pending",
            Body = "MOI SOC Expansion is awaiting 1st gateway review.",
            LinkUrl = "/qualification/" + SeedIds.Opp1 + "/gw1-review",
            OpportunityId = SeedIds.Opp1,
            CreatedAtUtc = now
        });
    }

    private static void Gate(AppDbContext db, Guid oppId, Guid gateId, GateState state, string role, int round, DateTime opened,
        string? decision = null, Guid? decidedBy = null, string? reason = null)
    {
        db.GateInstances.Add(new GateInstance
        {
            Id = Guid.NewGuid(),
            OpportunityId = oppId,
            GateId = gateId,
            State = state,
            AssignedRoleCode = role,
            OpenedAtUtc = opened,
            Round = round,
            Decision = decision,
            DecidedByUserId = decidedBy,
            DecidedAtUtc = decision is null ? null : opened.AddHours(6),
            Reason = reason
        });
    }

    private static User U(Guid id, string email, string name, string title) => new()
    {
        Id = id, Email = email, DisplayName = name, JobTitle = title, IsActive = true, CreatedAtUtc = DateTime.UtcNow
    };

    private static UserRole UR(Guid user, Guid role) => new() { UserId = user, RoleId = role };

    private static Customer C(Guid id, string en, string ar, string sector, bool gov) => new()
    {
        Id = id, NameEn = en, NameAr = ar, Sector = sector, IsGovernment = gov, CreatedAtUtc = DateTime.UtcNow
    };
}
