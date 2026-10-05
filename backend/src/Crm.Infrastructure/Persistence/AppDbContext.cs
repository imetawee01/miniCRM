using Crm.Application.Abstractions;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    private readonly AuditSaveChangesInterceptor _auditInterceptor;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        AuditSaveChangesInterceptor auditInterceptor,
        ICurrentUser currentUser,
        IDateTime clock)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
        _currentUser = currentUser;
        _clock = clock;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<ServiceLine> ServiceLines => Set<ServiceLine>();
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<StatusTransition> StatusTransitions => Set<StatusTransition>();
    public DbSet<WorkflowGate> WorkflowGates => Set<WorkflowGate>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<ScopeOfWork> ScopesOfWork => Set<ScopeOfWork>();
    public DbSet<ScopeItem> ScopeItems => Set<ScopeItem>();
    public DbSet<GateInstance> GateInstances => Set<GateInstance>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();
    public DbSet<EstimatedCost> EstimatedCosts => Set<EstimatedCost>();
    public DbSet<ProposalPricing> ProposalPricings => Set<ProposalPricing>();
    public DbSet<BidBond> BidBonds => Set<BidBond>();
    public DbSet<QualificationMeeting> QualificationMeetings => Set<QualificationMeeting>();
    public DbSet<QualificationMeetingAttendee> QualificationMeetingAttendees => Set<QualificationMeetingAttendee>();
    public DbSet<SlResponse> SlResponses => Set<SlResponse>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<OpportunityOutcome> OpportunityOutcomes => Set<OpportunityOutcome>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractNegotiationRound> ContractNegotiationRounds => Set<ContractNegotiationRound>();
    public DbSet<ContractMilestone> ContractMilestones => Set<ContractMilestone>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<GeneratedEmail> GeneratedEmails => Set<GeneratedEmail>();
    public DbSet<OpportunityNumberSequence> OpportunityNumberSequences => Set<OpportunityNumberSequence>();
    public DbSet<ContractNumberSequence> ContractNumberSequences => Set<ContractNumberSequence>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<UserServiceLine> UserServiceLines => Set<UserServiceLine>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SavedView> SavedViews => Set<SavedView>();
    public DbSet<ExportTemplate> ExportTemplates => Set<ExportTemplate>();
    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(_auditInterceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(x => x.TokenHash);
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(x => x.NameEn).HasMaxLength(300).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(300).IsRequired();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<CustomerContact>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<ServiceLine>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Stage>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Status>(e =>
        {
            e.HasIndex(x => new { x.StageId, x.Code }).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<StatusTransition>(e =>
        {
            e.HasOne(x => x.FromStatus).WithMany().HasForeignKey(x => x.FromStatusId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToStatus).WithMany().HasForeignKey(x => x.ToStatusId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.RequiredRoleCode).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<WorkflowGate>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.AllowedDecisions).HasMaxLength(200).IsRequired();
            e.Property(x => x.ResponsibleRoleCode).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<Opportunity>(e =>
        {
            e.HasIndex(x => x.OpportunityNumber).IsUnique();
            e.Property(x => x.OpportunityNumber).HasMaxLength(32).IsRequired();
            e.Property(x => x.Name).HasMaxLength(300).IsRequired();
            e.Property(x => x.SourceChannelOther).HasMaxLength(200);
            e.Property(x => x.ExpectedValueSar).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.OwnsOne(x => x.Deadlines, d =>
            {
                d.Property(p => p.QualificationDeadline);
                d.Property(p => p.InquiriesDeadline);
                d.Property(p => p.EstimatedCostDeadline);
                d.Property(p => p.InternalDeadline);
                d.Property(p => p.SubmissionDeadline);
            });
            e.HasOne(x => x.Customer).WithMany(x => x.Opportunities).HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SubmittedByUser).WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.OwnerUser).WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BuilderUser).WithMany().HasForeignKey(x => x.BuilderUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<ScopeOfWork>(e =>
        {
            e.HasIndex(x => x.OpportunityId).IsUnique();
        });

        modelBuilder.Entity<ScopeItem>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.HasOne(x => x.ServiceLine).WithMany().HasForeignKey(x => x.ServiceLineId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GateInstance>(e =>
        {
            e.HasOne(x => x.Gate).WithMany().HasForeignKey(x => x.GateId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AssignedRoleCode).HasMaxLength(80).IsRequired();
            e.Property(x => x.Decision).HasMaxLength(50);
        });

        modelBuilder.Entity<Note>(e =>
        {
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Comment>(e =>
        {
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasOne(x => x.ParentComment).WithMany(x => x.Replies).HasForeignKey(x => x.ParentCommentId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Attachment>(e =>
        {
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => x.OpportunityId);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.OccurredAtUtc);
            e.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.ActorRoleCode).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<AppNotification>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAtUtc });
        });

        modelBuilder.Entity<EstimatedCost>(e =>
        {
            e.HasIndex(x => x.OpportunityId).IsUnique();
            e.Property(x => x.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProposalPricing>(e =>
        {
            e.Property(x => x.PriceSar).HasPrecision(18, 2);
            e.Property(x => x.CostSar).HasPrecision(18, 2);
            e.Property(x => x.MarginPercent).HasPrecision(9, 2);
        });

        modelBuilder.Entity<BidBond>(e =>
        {
            e.HasIndex(x => x.OpportunityId).IsUnique();
            e.Property(x => x.AmountSar).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SlResponse>(e =>
        {
            e.Property(x => x.CostSar).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Submission>(e =>
        {
            e.HasIndex(x => x.OpportunityId).IsUnique();
        });

        modelBuilder.Entity<OpportunityOutcome>(e =>
        {
            e.HasIndex(x => x.OpportunityId).IsUnique();
            e.Property(x => x.AwardedValueSar).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Contract>(e =>
        {
            e.HasIndex(x => x.OpportunityId).IsUnique();
            e.HasIndex(x => x.ContractNumber).IsUnique();
            e.Property(x => x.ContractNumber).HasMaxLength(32).IsRequired();
            e.Property(x => x.ContractValueSar).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<ContractMilestone>(e =>
        {
            e.Property(x => x.AmountSar).HasPrecision(18, 2);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
        });

        modelBuilder.Entity<EmailTemplate>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<GeneratedEmail>(e =>
        {
            e.HasIndex(x => x.OpportunityId);
            e.Property(x => x.TemplateCode).HasMaxLength(80).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<OpportunityNumberSequence>(e =>
        {
            e.HasKey(x => x.Year);
            e.Property(x => x.Year).ValueGeneratedNever();
        });

        modelBuilder.Entity<ContractNumberSequence>(e =>
        {
            e.HasKey(x => x.Year);
            e.Property(x => x.Year).ValueGeneratedNever();
        });

        modelBuilder.Entity<Team>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.LeadUser).WithMany().HasForeignKey(x => x.LeadUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TeamMember>(e =>
        {
            e.HasKey(x => new { x.TeamId, x.UserId });
            e.HasOne(x => x.Team).WithMany(t => t.Members).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserServiceLine>(e =>
        {
            e.HasKey(x => new { x.UserId, x.ServiceLineId });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ServiceLine).WithMany().HasForeignKey(x => x.ServiceLineId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasIndex(x => new { x.PolicyName, x.RoleCode }).IsUnique();
            e.Property(x => x.PolicyName).HasMaxLength(100).IsRequired();
            e.Property(x => x.RoleCode).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<SavedView>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            e.Property(x => x.DefinitionJson).HasMaxLength(8000).IsRequired();
            e.HasIndex(x => new { x.EntityType, x.UserId });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExportTemplate>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            e.Property(x => x.FieldsJson).HasMaxLength(4000).IsRequired();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Activity>(e =>
        {
            e.Property(x => x.Summary).HasMaxLength(500).IsRequired();
            e.Property(x => x.Note).HasMaxLength(4000);
            e.HasIndex(x => new { x.OpportunityId, x.DueAtUtc });
            e.HasIndex(x => new { x.AssignedUserId, x.DoneAtUtc });
            e.HasOne(x => x.Opportunity).WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = _clock.UtcNow;
                entry.Entity.CreatedByUserId ??= _currentUser.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = _clock.UtcNow;
                entry.Entity.ModifiedByUserId = _currentUser.UserId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
