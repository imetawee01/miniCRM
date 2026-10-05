using System.Linq.Expressions;
using Crm.Application.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using MediatR;

namespace Crm.Application.Query;

public record GetEntityMetaFieldsQuery(string Entity) : IRequest<IReadOnlyList<MetaFieldDto>>;

public static class OpportunityMeta
{
    private static readonly string[] StringOps = ["=", "!=", "like", "ilike", "in", "not in", "is null", "is not null"];
    private static readonly string[] NumOps = ["=", "!=", ">", ">=", "<", "<=", "between", "in", "not in", "is null", "is not null"];
    private static readonly string[] BoolOps = ["=", "!="];
    private static readonly string[] DateOps = ["=", "!=", ">", ">=", "<", "<=", "between", "is null", "is not null"];
    private static readonly string[] GuidOps = ["=", "!=", "in", "not in", "is null", "is not null"];
    private static readonly string[] EnumOps = ["=", "!=", "in", "not in"];

    public static IReadOnlyList<MetaFieldDto> Fields { get; } =
    [
        F("opportunityNumber", "Number", "الرقم", "String", StringOps, sortable: true, exportable: true),
        F("name", "Name", "الاسم", "String", StringOps, sortable: true, exportable: true),
        F("customerName", "Customer", "العميل", "String", StringOps, groupable: true, sortable: true, exportable: true),
        F("customerId", "Customer Id", "معرف العميل", "Guid", GuidOps, relation: "Customer", exportable: false),
        F("stageCode", "Stage", "المرحلة", "String", StringOps, groupable: true, sortable: true, exportable: true),
        F("stageId", "Stage Id", "معرف المرحلة", "Guid", GuidOps, exportable: false),
        F("statusCode", "Status", "الحالة", "String", StringOps, groupable: true, sortable: true, exportable: true),
        F("statusId", "Status Id", "معرف الحالة", "Guid", GuidOps, exportable: false),
        F("ownerUserId", "Owner", "المالك", "Guid", GuidOps, relation: "User", groupable: true, exportable: true),
        F("builderUserId", "Builder", "المنشئ", "Guid", GuidOps, relation: "User", groupable: true, exportable: true),
        F("expectedValueSar", "Expected value (SAR)", "القيمة المتوقعة", "Number", NumOps, sortable: true, exportable: true),
        F("submissionTheme", "Submission theme", "موضوع التقديم", "Enum", EnumOps, groupable: true, sortable: true, exportable: true),
        F("sourceChannel", "Source channel", "قناة المصدر", "Enum", EnumOps, groupable: true, sortable: true, exportable: true),
        F("engagementType", "Engagement type", "نوع التفاعل", "Enum", EnumOps, groupable: true, exportable: true),
        F("opportunityType", "Opportunity type", "نوع الفرصة", "Enum", EnumOps, groupable: true, exportable: true),
        F("isClosed", "Closed", "مغلقة", "Boolean", BoolOps, groupable: true, exportable: true),
        F("requiresBidBond", "Bid bond required", "يتطلب ضمان ابتدائي", "Boolean", BoolOps, exportable: true),
        F("submissionDeadline", "Submission deadline", "موعد التقديم", "DateTime", DateOps, sortable: true, exportable: true),
        F("internalDeadline", "Internal deadline", "الموعد الداخلي", "DateTime", DateOps, sortable: true, exportable: true),
        F("createdAtUtc", "Created", "تاريخ الإنشاء", "DateTime", DateOps, sortable: true, exportable: true),
        F("pendingRoleCode", "Pending role", "الدور المعلق", "String", StringOps, exportable: true),
    ];

    public static IReadOnlyList<DomainFieldMap<Opportunity>> Maps { get; } = BuildMaps();

    private static IReadOnlyList<DomainFieldMap<Opportunity>> BuildMaps()
    {
        var p = Expression.Parameter(typeof(Opportunity), "x");
        DomainFieldMap<Opportunity> Map(string name, MetaFieldType type, Expression body) => new()
        {
            Name = name,
            Type = type,
            Parameter = p,
            Accessor = _ => new ParameterReplacer(p).Visit(body)!
        };

        return
        [
            Map("opportunityNumber", MetaFieldType.String, Expression.Property(p, nameof(Opportunity.OpportunityNumber))),
            Map("name", MetaFieldType.String, Expression.Property(p, nameof(Opportunity.Name))),
            Map("customerName", MetaFieldType.String, Expression.Property(Expression.Property(p, nameof(Opportunity.Customer)), nameof(Customer.NameEn))),
            Map("customerId", MetaFieldType.Guid, Expression.Property(p, nameof(Opportunity.CustomerId))),
            Map("stageCode", MetaFieldType.String, Expression.Property(Expression.Property(p, nameof(Opportunity.Stage)), nameof(Stage.Code))),
            Map("stageId", MetaFieldType.Guid, Expression.Property(p, nameof(Opportunity.StageId))),
            Map("statusCode", MetaFieldType.String, Expression.Property(Expression.Property(p, nameof(Opportunity.Status)), nameof(Status.Code))),
            Map("statusId", MetaFieldType.Guid, Expression.Property(p, nameof(Opportunity.StatusId))),
            Map("ownerUserId", MetaFieldType.Guid, Expression.Property(p, nameof(Opportunity.OwnerUserId))),
            Map("builderUserId", MetaFieldType.Guid, Expression.Property(p, nameof(Opportunity.BuilderUserId))),
            Map("expectedValueSar", MetaFieldType.Number, Expression.Property(p, nameof(Opportunity.ExpectedValueSar))),
            Map("submissionTheme", MetaFieldType.Enum, Expression.Property(p, nameof(Opportunity.SubmissionTheme))),
            Map("sourceChannel", MetaFieldType.Enum, Expression.Property(p, nameof(Opportunity.SourceChannel))),
            Map("engagementType", MetaFieldType.Enum, Expression.Property(p, nameof(Opportunity.EngagementType))),
            Map("opportunityType", MetaFieldType.Enum, Expression.Property(p, nameof(Opportunity.OpportunityType))),
            Map("isClosed", MetaFieldType.Boolean, Expression.Property(p, nameof(Opportunity.IsClosed))),
            Map("requiresBidBond", MetaFieldType.Boolean, Expression.Property(p, nameof(Opportunity.RequiresBidBond))),
            Map("submissionDeadline", MetaFieldType.DateTime, Expression.Property(Expression.Property(p, nameof(Opportunity.Deadlines)), nameof(OpportunityDeadlines.SubmissionDeadline))),
            Map("internalDeadline", MetaFieldType.DateTime, Expression.Property(Expression.Property(p, nameof(Opportunity.Deadlines)), nameof(OpportunityDeadlines.InternalDeadline))),
            Map("createdAtUtc", MetaFieldType.DateTime, Expression.Property(p, nameof(Opportunity.CreatedAtUtc))),
        ];
    }

    private static MetaFieldDto F(
        string name, string en, string ar, string type, string[] ops,
        bool groupable = false, bool sortable = false, bool exportable = true,
        string? relation = null, string? permission = null) =>
        new(name, en, ar, type, ops, relation, groupable, sortable, exportable, permission);

    private sealed class ParameterReplacer(ParameterExpression param) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => param;
    }
}

public sealed class MetaHandlers : IRequestHandler<GetEntityMetaFieldsQuery, IReadOnlyList<MetaFieldDto>>
{
    public Task<IReadOnlyList<MetaFieldDto>> Handle(GetEntityMetaFieldsQuery request, CancellationToken ct)
    {
        var entity = request.Entity.Trim().ToLowerInvariant();
        IReadOnlyList<MetaFieldDto> fields = entity switch
        {
            "opportunities" or "opportunity" => OpportunityMeta.Fields,
            _ => throw new NotFoundException("Entity", request.Entity)
        };
        return Task.FromResult(fields);
    }
}
