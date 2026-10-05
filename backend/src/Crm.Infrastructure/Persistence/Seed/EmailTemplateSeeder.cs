using Crm.Domain.Common;
using Crm.Domain.Entities;

namespace Crm.Infrastructure.Persistence.Seed;

public static class EmailTemplateSeeder
{
    public static IEnumerable<EmailTemplate> All()
    {
        yield return T(EmailTemplateCodes.OppReceived, "Opportunity Receiving", "استلام فرصة",
            "New opportunity - {{OpportunityName}}",
            "bidding@crm.local", "supervisor@crm.local",
            TableBody("A new opportunity has been received.",
                ("Opportunity Name", "{{OpportunityName}}"),
                ("Customer Name", "{{CustomerName}}"),
                ("Source/Channel", "{{SourceChannel}}"),
                ("Submission Date", "{{SubmissionDate}}"),
                ("Submission Theme", "{{SubmissionTheme}}"),
                ("Proactive/Reactive", "{{EngagementType}}"),
                ("Expected Value (SAR)", "{{ExpectedValueSar}}"),
                ("New/Renewal", "{{OpportunityType}}"),
                ("Relation with Client (1–5)", "{{RelationWithClientScore}}"),
                ("Win Probability (1–5)", "{{WinProbabilityScore}}")));

        yield return T(EmailTemplateCodes.Gw1Rejected, "Not Passing 1st GW Review", "عدم اجتياز مراجعة البوابة الأولى",
            "New opportunity - {{OpportunityName}}",
            "{{SubmittingAmEmail}}", "bidding@crm.local",
            """
            <p>Dear {{SubmittingAmName}},</p>
            <p>The opportunity <strong>{{OpportunityName}}</strong> ({{OpportunityNumber}}) did not pass the first qualification stage.</p>
            <p>Rejection reasons:</p>
            <ul>{{RejectionReasonsHtml}}</ul>
            """);

        yield return T(EmailTemplateCodes.QualRequest, "Request for a Qualification", "طلب تأهيل",
            "Qualification - {{OpportunityName}} - {{OpportunityNumber}}",
            "{{SlOrPresalesEmail}}", "bidding@crm.local, {{SubmittingAmEmail}}",
            TableBody("Please review the qualification request and nominate the resource who will develop the response if qualified.",
                ("Qualification Deadline", "{{QualificationDeadline}}"),
                ("Opportunity Name", "{{OpportunityName}}"),
                ("Customer Name", "{{CustomerName}}"),
                ("Source/Channel", "{{SourceChannel}}"),
                ("Duration", "{{DurationMonths}}"),
                ("Proposal Language", "{{ProposalLanguage}}"),
                ("Submission Theme", "{{SubmissionTheme}}"),
                ("Inquiries Deadline", "{{InquiriesDeadline}}"),
                ("Estimated Cost Deadline", "{{EstimatedCostDeadline}}"),
                ("Internal Deadline", "{{InternalDeadline}}"),
                ("Submission Deadline", "{{SubmissionDeadline}}"))
            + "{{ScopeOfWorkHtml}}");

        yield return T(EmailTemplateCodes.QualMeetingRequest, "Request a Qualification Meeting", "طلب اجتماع تأهيل",
            "Qualification - {{OpportunityName}} - {{OpportunityNumber}}",
            "{{SlRepresentatives}}", "bidding@crm.local, {{SubmittingAmEmail}}",
            TableBody("You are invited to a qualification meeting.",
                ("Opportunity Name", "{{OpportunityName}}"),
                ("Customer Name", "{{CustomerName}}"),
                ("Duration", "{{DurationMonths}}"),
                ("Source/Channel", "{{SourceChannel}}"),
                ("Submission Theme", "{{SubmissionTheme}}"),
                ("Submission Deadline", "{{SubmissionDeadline}}"))
            + "{{ScopeOfWorkHtml}}");

        yield return T(EmailTemplateCodes.BuilderNotification, "Notification to the Builder", "إشعار البنّاء",
            "Proposal Request - {{OpportunityName}} - {{OpportunityNumber}}",
            "{{BuilderEmail}}", "bidding@crm.local, {{SubmittingAmEmail}}",
            TableBody("You have been assigned as builder. Both the technical proposal and associated costing are required.",
                ("Opportunity Name", "{{OpportunityName}}"),
                ("Customer Name", "{{CustomerName}}"),
                ("Source/Channel", "{{SourceChannel}}"),
                ("Duration", "{{DurationMonths}}"),
                ("Proposal Language", "{{ProposalLanguage}}"),
                ("Submission Theme", "{{SubmissionTheme}}"),
                ("Inquiries Deadline", "{{InquiriesDeadline}}"),
                ("Estimated Cost Deadline", "{{EstimatedCostDeadline}}"),
                ("Internal Deadline", "{{InternalDeadline}}"),
                ("Submission Deadline", "{{SubmissionDeadline}}"))
            + "{{ScopeOfWorkHtml}}");

        yield return T(EmailTemplateCodes.ApprovalRequest, "Approval Request", "طلب اعتماد",
            "Approval Request - {{OpportunityName}} - {{CustomerName}} - {{OpportunityNumber}}",
            "{{ApproverEmail}}", "bidding@crm.local, {{SubmittingAmEmail}}",
            TableBody("Please review and approve the proposal pricing.",
                ("Opportunity Name", "{{OpportunityName}}"),
                ("Customer Name", "{{CustomerName}}"),
                ("Duration", "{{DurationMonths}}"),
                ("Submission Date", "{{SubmissionDate}}"),
                ("Scope Brief", "{{ScopeBrief}}"),
                ("Price (SAR)", "{{PriceSar}}"),
                ("Margin %", "{{MarginPercent}}")));
    }

    private static EmailTemplate T(string code, string en, string ar, string subject, string to, string cc, string body) =>
        new()
        {
            Id = Guid.Parse("aa000001-0000-0000-0000-" + Math.Abs(code.GetHashCode()).ToString("000000000000")[^12..]),
            Code = code, NameEn = en, NameAr = ar, SubjectTemplate = subject,
            BodyTemplateHtml = Wrap(body), DefaultTo = to, DefaultCc = cc, IsActive = true
        };

    private static string Wrap(string inner) =>
        $"<html><body style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1a1a1a'>{inner}<p style='color:#666;font-size:12px'>This is a generated draft. Nothing is sent automatically.</p></body></html>";

    private static string TableBody(string intro, params (string Label, string Token)[] rows)
    {
        var tr = string.Join("", rows.Select(r =>
            $"<tr><td style='padding:6px 10px;border:1px solid #ddd;font-weight:600'>{r.Label}</td><td style='padding:6px 10px;border:1px solid #ddd'>{r.Token}</td><td style='padding:6px 10px;border:1px solid #ddd;color:#888'>Comment</td></tr>"));
        return $"<p>{intro}</p><table cellpadding='0' cellspacing='0' style='border-collapse:collapse;width:100%'>{tr}</table>";
    }
}
