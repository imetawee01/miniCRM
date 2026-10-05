# Build Prompt — Opportunity Lifecycle CRM (Presales & Bids)

> Paste this whole document into your AI coding tool as the master specification.
> Build it in the phases listed in section 16. Do not skip the seed data — the
> workflow will not function without it.

---

## 1. Objective

Build a production-grade CRM that manages the full opportunity lifecycle for a
Presales & Bids department: from receiving an opportunity, through qualification,
proposal development, management approval, submission, and finally contracting.

Deliver **two separate projects in one repository**:

| Project | Technology | Folder |
|---|---|---|
| Backend REST API | .NET 8 Web API, C# 12 | `/backend` |
| Frontend SPA | Angular 18+, TypeScript, standalone components | `/frontend` |

Both must run locally with a documented two-command startup, plus a
`docker-compose.yml` that brings up API + SQL Server + frontend.

---

## 2. Tech stack (fixed — do not substitute)

**Backend**
- .NET 8 Web API, nullable reference types enabled, `TreatWarningsAsErrors` off
- Entity Framework Core 8 + SQL Server (LocalDB acceptable for dev)
- Code-first migrations + idempotent seeders
- MediatR for command/query handling
- FluentValidation for request validation
- AutoMapper for entity ↔ DTO
- Serilog (console + rolling file)
- Swashbuckle / Swagger UI with JWT auth button
- xUnit + FluentAssertions + EF Core InMemory for tests

**Frontend**
- Angular 18+ with **standalone components** (no NgModules except where unavoidable)
- Angular Router with **lazy-loaded feature routes**
- Angular **signals** for local/component state; RxJS only for HTTP streams
- Reactive Forms (typed) everywhere — no template-driven forms
- Angular Material + a small custom SCSS theme layer
- `@ngx-translate/core` for i18n (EN + AR) with full RTL support
- Jest or Karma unit tests for services and guards

---

## 3. Backend solution structure

```
backend/
├── Crm.sln
├── src/
│   ├── Crm.Domain/                  # entities, enums, domain events, no dependencies
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── ValueObjects/
│   │   └── Common/                  # BaseEntity, IAuditable, ISoftDelete
│   ├── Crm.Application/             # use cases, DTOs, interfaces, validators
│   │   ├── Common/                  # behaviours, mapping, exceptions, PagedResult<T>
│   │   ├── Abstractions/            # IFileStorage, IEmailComposer, IEmailSender,
│   │   │                            # ICurrentUser, IDateTime, IWorkflowEngine
│   │   ├── Opportunities/           # Commands/, Queries/, Dtos/, Validators/
│   │   ├── Qualification/
│   │   ├── Proposals/
│   │   ├── Approvals/
│   │   ├── Contracting/
│   │   ├── Collaboration/           # notes, comments, attachments
│   │   ├── Notifications/           # email templates + generated emails
│   │   ├── Audit/
│   │   ├── Lookups/
│   │   └── Identity/
│   ├── Crm.Infrastructure/
│   │   ├── Persistence/             # AppDbContext, Configurations/, Migrations/, Seed/
│   │   ├── Identity/                # JwtTokenService, PasswordHasher, (SSO stub)
│   │   ├── Files/                   # LocalDiskFileStorage : IFileStorage
│   │   ├── Email/                   # RazorEmailComposer, SmtpEmailSender (disabled)
│   │   └── Workflow/                # WorkflowEngine, TransitionValidator
│   └── Crm.Api/
│       ├── Controllers/
│       ├── Middleware/              # ExceptionHandlingMiddleware, RequestLogging
│       ├── Extensions/              # DI registration per layer
│       ├── appsettings.json
│       └── Program.cs
└── tests/
    ├── Crm.Application.Tests/
    └── Crm.Api.IntegrationTests/
```

Dependency direction: `Api → Application → Domain`, `Infrastructure → Application`.
`Domain` references nothing.

---

## 4. Roles (organisational swimlanes)

Seed these six roles exactly. Every workflow action is authorised against them.

| Role code | Display name | Responsibility |
|---|---|---|
| `AM` | Account Manager | Submits new opportunities, receives rejection notices |
| `BIDS_PRESALES` | Bids & Presales | Performs 1st gate review, decides qualification route |
| `BIDS_MGMT` | Bids Management | Runs qualification meetings, notifies builders, estimated cost, bid bond, submission |
| `PRESALES` | Presales | Builds proposals, collects SL responses, performs proposal review |
| `SL` | Service Line | Builds technical proposal + costing for assigned scope items |
| `MGMT` | Management | Final approval on price and margin |
| `ADMIN` | Administrator | User management, lookups, email templates, workflow config |

A user may hold multiple roles. `ADMIN` implies read access to everything.

---

## 5. Domain model

### 5.1 Opportunity (aggregate root)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `OpportunityNumber` | string | Auto-generated, format `OPP-{yyyy}-{00000}`, unique, sequential per year |
| `Name` | string(300) | Required |
| `CustomerId` | Guid | FK → Customer |
| `SourceChannel` | enum | `DirectClient`, `Etimad`, `Partner`, `Referral`, `AccountExpansion`, `Other` |
| `SourceChannelOther` | string(200)? | Required only when `SourceChannel = Other` |
| `SubmissionTheme` | enum | `Emdad`, `Ahad`, `ThiqahBusinessSolutions`, `Elm` |
| `EngagementType` | enum | `Proactive`, `Reactive` |
| `OpportunityType` | enum | `New`, `Renewal` |
| `ExpectedValueSar` | decimal(18,2) | Required, > 0 |
| `RelationWithClientScore` | int | 1–5 |
| `WinProbabilityScore` | int | 1–5 |
| `DurationMonths` | int? | |
| `ProposalLanguage` | enum | `Arabic`, `English`, `Bilingual` |
| `StageId` / `StatusId` | Guid | FK → Stage / Status (current position) |
| `SubmittedByUserId` | Guid | The AM who raised it |
| `OwnerUserId` | Guid? | Current responsible user |
| `BuilderType` | enum? | `Presales`, `ServiceLine` |
| `BuilderUserId` | Guid? | Assigned builder |
| `RequiresQualificationMeeting` | bool? | Decision outcome |
| `RequiresBidBond` | bool | Default false |
| `IsClosed` | bool | Computed from terminal status |
| `ClosedAtUtc` | DateTime? | |
| Audit fields | | `CreatedAtUtc`, `CreatedByUserId`, `ModifiedAtUtc`, `ModifiedByUserId` |

### 5.2 OpportunityDeadlines (owned entity / 1-1)

`QualificationDeadline`, `InquiriesDeadline`, `EstimatedCostDeadline`,
`InternalDeadline`, `SubmissionDeadline` — all `DateTime?` (UTC).
Validation rule: `QualificationDeadline ≤ InquiriesDeadline ≤ EstimatedCostDeadline
≤ InternalDeadline ≤ SubmissionDeadline` when populated.

### 5.3 ScopeOfWork / ScopeItem

- `ScopeOfWork`: `Id`, `OpportunityId`, `Brief` (multiline text)
- `ScopeItem`: `Id`, `ScopeOfWorkId`, `Title`, `Description?`, `ServiceLineId` (FK → ServiceLine), `AssignedUserId?`, `Comment?`, `SortOrder`

### 5.4 Stage & Status (seeded lookups — see 6.1)

- `Stage`: `Id`, `Code`, `NameEn`, `NameAr`, `SortOrder`, `IsActive`
- `Status`: `Id`, `StageId`, `Code`, `NameEn`, `NameAr`, `SortOrder`, `IsTerminal`, `IsActive`
- `StatusTransition`: `Id`, `FromStatusId`, `ToStatusId`, `RequiredRoleCode`, `RequiresReason` (bool)

### 5.5 Gate / Approval engine

- `WorkflowGate` (config, seeded): `Id`, `Code`, `NameEn`, `NameAr`, `SortOrder`,
  `ResponsibleRoleCode`, `AllowedDecisions` (csv), `RequiresReasonOnReject` (bool),
  `RequiresAttachmentOnApprove` (bool), `IsActive`
- `GateInstance`: `Id`, `OpportunityId`, `GateId`, `State` (`Pending`, `Approved`,
  `Rejected`, `Returned`, `Skipped`), `AssignedRoleCode`, `AssignedUserId?`,
  `OpenedAtUtc`, `DecidedAtUtc?`, `DecidedByUserId?`, `Decision?`, `Reason?`,
  `Round` (int — increments each time the gate is re-opened after a return)
- Every `GateInstance` supports **notes, comments, attachments, and audit entries**
  (section 5.6). This is a hard requirement for every approval level.

### 5.6 Collaboration entities (attachable to Opportunity **and** GateInstance)

All three use a polymorphic owner: `EntityType` (`Opportunity`, `GateInstance`,
`ScopeItem`, `Contract`) + `EntityId`.

- `Note`: `Id`, `EntityType`, `EntityId`, `Body` (text), `Visibility`
  (`Internal`, `Shared`), `CreatedByUserId`, `CreatedAtUtc`, `ModifiedAtUtc?`
- `Comment`: `Id`, `EntityType`, `EntityId`, `Body`, `ParentCommentId?` (single-level
  threading), `MentionedUserIds` (json array), `CreatedByUserId`, `CreatedAtUtc`,
  `EditedAtUtc?`, `IsDeleted`
- `Attachment`: `Id`, `EntityType`, `EntityId`, `FileName`, `StoredFileName`,
  `ContentType`, `SizeBytes`, `Description` **(required — do not allow upload
  without a description)**, `Category` (`RFP`, `TechnicalProposal`, `Costing`,
  `BidBond`, `Contract`, `Other`), `UploadedByUserId`, `UploadedAtUtc`, `IsDeleted`

Constraints: max 25 MB per file; allow `pdf, docx, xlsx, pptx, vsdx, png, jpg,
zip, msg`; reject everything else with a 422; store on disk under
`{StorageRoot}/{yyyy}/{MM}/{Guid}{ext}` and never trust the client filename.

### 5.7 AuditLog (append-only, never updated or deleted)

`Id`, `EntityType`, `EntityId`, `OpportunityId?` (denormalised for fast filtering),
`Action` (enum below), `ActorUserId`, `ActorRoleCode`, `OccurredAtUtc` (UTC),
`FromValue?`, `ToValue?`, `Description`, `MetadataJson?`, `IpAddress?`, `UserAgent?`

`Action` values: `OpportunityCreated`, `OpportunityUpdated`, `StatusChanged`,
`StageChanged`, `GateOpened`, `GateApproved`, `GateRejected`, `GateReturned`,
`NoteAdded`, `NoteEdited`, `CommentAdded`, `CommentEdited`, `CommentDeleted`,
`AttachmentUploaded`, `AttachmentDeleted`, `AttachmentDownloaded`,
`BuilderAssigned`, `DeadlinesSet`, `EstimatedCostSubmitted`, `BidBondRequested`,
`BidBondIssued`, `ProposalSubmittedForReview`, `ProposalReturned`,
`ApprovalRequested`, `Submitted`, `OutcomeRecorded`, `ContractStageChanged`,
`OpportunityHeld`, `OpportunityResumed`, `OpportunityCanceled`, `EmailGenerated`,
`EmailSent`, `UserLoggedIn`.

Implement audit writing in an EF Core `SaveChangesInterceptor` **plus** explicit
domain-event handlers, so no state change can bypass the log. Every entry carries
date and time in UTC; the API returns UTC and the frontend renders in the user's
local timezone with a tooltip showing UTC.

### 5.8 Financial & supporting entities

- `EstimatedCost`: `Id`, `OpportunityId`, `Amount`, `Currency` (default SAR),
  `SubmittedByUserId`, `SubmittedAtUtc`, `Notes?`
- `ProposalPricing`: `Id`, `OpportunityId`, `PriceSar`, `CostSar`,
  `MarginPercent` (computed, persisted), `Version` (int), `IsCurrent`,
  `CreatedByUserId`, `CreatedAtUtc`
- `BidBond`: `Id`, `OpportunityId`, `Required`, `AmountSar?`, `ValidUntil?`,
  `IssuingBank?`, `Status` (`NotRequired`, `Requested`, `Issued`, `Rejected`),
  `RequestedAtUtc?`, `IssuedAtUtc?`
- `QualificationMeeting`: `Id`, `OpportunityId`, `ScheduledAtUtc`, `Location?`,
  `MeetingLink?`, `Agenda?`, `MinutesOfMeeting?`, `Outcome`
  (`Pending`, `Passed`, `NotPassed`), `HeldAtUtc?`
- `QualificationMeetingAttendee`: `Id`, `MeetingId`, `UserId`, `ServiceLineId?`,
  `Response` (`Invited`, `Accepted`, `Declined`, `Attended`)
- `SlResponse`: `Id`, `OpportunityId`, `ServiceLineId`, `ScopeItemId?`,
  `TechnicalProposalAttachmentId?`, `CostingAttachmentId?`, `CostSar?`,
  `Status` (`Pending`, `Submitted`, `Returned`, `Accepted`), `SubmittedAtUtc?`,
  `DueAtUtc?`
- `Submission`: `Id`, `OpportunityId`, `SubmittedAtUtc`, `SubmittedByUserId`,
  `Channel` (`Etimad`, `Email`, `HandDelivery`, `Portal`, `Other`), `Reference?`
- `OpportunityOutcome`: `Id`, `OpportunityId`, `Result` (`Won`, `Lost`),
  `AnnouncedAtUtc`, `AwardedValueSar?`, `CompetitorName?`, `LossReason?`, `Notes?`

### 5.9 Contracting module (full implementation — requested)

- `Contract`: `Id`, `OpportunityId` (unique), `ContractNumber` (auto
  `CTR-{yyyy}-{00000}`), `ContractStatus` (`Won`, `ContractNegotiation`,
  `ContractSigned`), `ContractValueSar`, `StartDate?`, `EndDate?`,
  `DurationMonths?`, `SignedAtUtc?`, `SignedByCustomerRepresentative?`,
  `PaymentTerms?`, `Notes?`
- `ContractNegotiationRound`: `Id`, `ContractId`, `RoundNumber`, `RequestedChanges`,
  `OurPosition`, `CustomerPosition`, `Status` (`Open`, `Agreed`, `Rejected`),
  `OpenedAtUtc`, `ClosedAtUtc?`, `OpenedByUserId`
- `ContractMilestone`: `Id`, `ContractId`, `Title`, `DueDate`, `AmountSar?`,
  `IsCompleted`, `CompletedAtUtc?`
- Contracts also support notes, comments, and attachments via the polymorphic owner.

### 5.10 Reference entities

`Customer` (`Id`, `NameEn`, `NameAr`, `Sector`, `IsGovernment`, `Website?`,
contacts collection), `CustomerContact`, `ServiceLine` (`Id`, `Code`, `NameEn`,
`NameAr`, `LeadUserId?`), `User`, `Role`, `UserRole`, `EmailTemplate`,
`GeneratedEmail`.

---

## 6. Seed data (mandatory)

### 6.1 Stages and statuses — exactly as supplied

| Stage | Statuses (in order) |
|---|---|
| `Qualification` | `Awaiting Assessment`, `Qualified`, `Not Qualified` (terminal), `Canceled` (terminal) |
| `Response Development` | `In Progress`, `Internal Review`, `Hold`, `Canceled` (terminal) |
| `Submission` | `Not Approved`, `Submitted`, `Lost` (terminal) |
| `Contracting` | `Won`, `Contract Negotiation`, `Contract Signed` (terminal) |

Provide Arabic names for each. Mark `IsTerminal` as indicated above.

**Note on the Contracting order:** the source file lists `Won` first, then
`Contract Negotiation`, then `Contract Signed`. Preserve that display order in the
lookup table, but enforce the transition order
`Won → Contract Negotiation → Contract Signed` in `StatusTransition`.

### 6.2 Workflow gates

| Code | Name | Responsible role | Decisions |
|---|---|---|---|
| `GW1_REVIEW` | 1st Gateway Review | `BIDS_PRESALES` | Approve, Reject |
| `QUAL_DECISION` | Service Line Qualification | `SL` or `PRESALES` | Qualified, NotQualified |
| `QUAL_MEETING` | Qualification Meeting Outcome | `BIDS_MGMT` | Passed, NotPassed |
| `PROPOSAL_REVIEW` | Proposal Review | `PRESALES` | Approve, Return |
| `MGMT_APPROVAL` | Management Approval | `MGMT` | Approve, Reject |
| `CONTRACT_SIGNOFF` | Contract Sign-off | `MGMT` | Approve, Reject |

### 6.3 Demo data

15 opportunities spread across all four stages and all six gates, 6 customers,
5 service lines, 10 users covering every role (password `Passw0rd!` for all in dev
only), plus notes, comments, attachments, and audit history on each so every screen
has content on first run.

---

## 7. Workflow specification

Implement this as a **configurable engine** (`IWorkflowEngine`) that reads
`WorkflowGate` + `StatusTransition` from the database. Do not hardcode the flow in
controllers.

### 7.1 The flow

```
[AM] Create Opportunity
  → Status: Qualification / Awaiting Assessment
  → Generate email #1 (Opportunity Receiving)
  → Open gate GW1_REVIEW

[BIDS_PRESALES] GW1_REVIEW
  ├─ Reject → Status: Qualification / Not Qualified
  │            → Generate email #2 (Not Passing 1st GW Review) to the submitting AM
  │            → Opportunity closed
  └─ Approve → Decision: qualification meeting required?
       ├─ No  (single service line engaged)
       │     → Generate email #3 (Request for a Qualification)
       │     → Open gate QUAL_DECISION
       │         ├─ Not Qualified → Status: Qualification / Not Qualified → closed
       │         └─ Qualified     → go to BUILDER ASSIGNMENT
       └─ Yes (multiple service lines engaged)
             → Generate email #4 (Request a Qualification Meeting)
             → Create QualificationMeeting, invite attendees
             → Conduct meeting, record minutes
             → Open gate QUAL_MEETING
                 ├─ Not Passed → Status: Qualification / Not Qualified → closed
                 └─ Passed     → go to BUILDER ASSIGNMENT

BUILDER ASSIGNMENT [BIDS_PRESALES or BIDS_MGMT]
  → Choose BuilderType: Presales | ServiceLine, assign BuilderUserId
  → Status: Qualification / Qualified, then immediately Response Development / In Progress
  → Set the five deadlines
  → Generate email #5 (Notification to the Builder with deadlines)
  → In parallel: request Estimated Cost from the builder
  → In parallel (if RequiresBidBond): Bid Bond request → issued
      NOTE: bid bond is a parallel side-track. It must NOT terminate the flow.
      The main flow continues regardless of bid bond state, but submission is
      blocked while a required bid bond is not yet Issued.

PROPOSAL DEVELOPMENT
  ├─ BuilderType = Presales      → Presales builds technical proposal + costing
  └─ BuilderType = ServiceLine   → SL builds; Presales collects SlResponse records
                                    (technical proposal + costing per scope item)
  → Builder marks proposal ready
  → Status: Response Development / Internal Review
  → Open gate PROPOSAL_REVIEW

[PRESALES] PROPOSAL_REVIEW  (both builder paths pass through this gate)
  ├─ Return  → Create "Proposals and Costing updates" task for the builder
  │            → Status back to Response Development / In Progress
  │            → Increment gate Round; loop back to PROPOSAL DEVELOPMENT
  └─ Approve → Record ProposalPricing (price, cost, margin %)
               → Generate email #6 (Approval Request)
               → Open gate MGMT_APPROVAL

[MGMT] MGMT_APPROVAL
  ├─ Reject  → Status: Submission / Not Approved
  │            → Create "Proposals and Costing updates" task
  │            → Loop back to PROPOSAL DEVELOPMENT (new pricing version)
  └─ Approve → [BIDS_MGMT] Proposal Submission
               → Status: Submission / Submitted
               → Record Submission (channel, reference, date/time)

OUTCOME [BIDS_MGMT or AM]
  ├─ Lost → Status: Submission / Lost (terminal) + LossReason
  └─ Won  → Status: Contracting / Won
            → Create Contract
            → Status: Contracting / Contract Negotiation (one or more rounds)
            → Open gate CONTRACT_SIGNOFF
            → Approve → Status: Contracting / Contract Signed (terminal)

SIDE TRANSITIONS (available from any non-terminal status)
  ├─ Hold   → Response Development / Hold. Requires reason. Resumable to prior status.
  └─ Cancel → Canceled (terminal) in the current stage. Requires reason.
              Allowed roles: BIDS_PRESALES, BIDS_MGMT, ADMIN.
```

### 7.2 Deviations from the source diagram — implement the corrected version

The supplied Visio diagram contains three structural defects. Build the corrected
behaviour described above and add a code comment at each point referencing this
section:

1. In the diagram, the qualification-meeting branch ends at *Bid Bond Issuing* with
   no outgoing path. **Corrected:** the meeting branch rejoins the main flow at
   Builder Assignment, and bid bond is a parallel side-track, not a terminus.
2. In the diagram, *Proposals and Costing updates* has two incoming arrows and no
   outgoing arrow. **Corrected:** it returns the opportunity to the builder and
   re-opens the relevant gate with an incremented round number.
3. In the diagram, a Presales-built proposal bypasses *Proposal Reviewal* and goes
   straight to *Approval*. **Corrected:** both builder paths pass through
   `PROPOSAL_REVIEW`.

### 7.3 Enforcement rules

- Reject any status change not present in `StatusTransition` → HTTP 409 with the
  list of legal next statuses.
- Reject any gate decision by a user lacking `ResponsibleRoleCode` → HTTP 403.
- A gate decision is atomic: decision + status change + audit entries + generated
  email all commit in one transaction, or none of them do.
- Submission is blocked (422) if: no current `ProposalPricing`, or
  `MGMT_APPROVAL` not approved, or a required bid bond is not `Issued`.

---

## 8. Email templates

Store all six as `EmailTemplate` rows with `Code`, `NameEn`, `NameAr`,
`SubjectTemplate`, `BodyTemplateHtml`, `DefaultTo`, `DefaultCc`, `IsActive`.
Use `{{TokenName}}` placeholders resolved server-side from the opportunity.

| Code | Trigger | To | CC | Subject |
|---|---|---|---|---|
| `OPP_RECEIVED` | Opportunity created | Bidding mailbox | Configured supervisor | `New opportunity - {{OpportunityName}}` |
| `GW1_REJECTED` | GW1_REVIEW rejected | Submitting AM | Bidding mailbox | `New opportunity - {{OpportunityName}}` |
| `QUAL_REQUEST` | Single-SL qualification route | SL representative / Presales | Bidding mailbox, AM | `Qualification - {{OpportunityName}} - {{OpportunityNumber}}` |
| `QUAL_MEETING_REQUEST` | Multi-SL qualification route | SL representatives | Bidding mailbox, AM | `Qualification - {{OpportunityName}} - {{OpportunityNumber}}` |
| `BUILDER_NOTIFICATION` | Builder assigned | Assigned builder | Bidding mailbox, AM | `Proposal Request - {{OpportunityName}} - {{OpportunityNumber}}` |
| `APPROVAL_REQUEST` | PROPOSAL_REVIEW approved | Approver | Bidding mailbox, AM | `Approval Request - {{OpportunityName}} - {{CustomerName}} - {{OpportunityNumber}}` |

Body content per template (rebuild the tables from the source document as HTML):

- `OPP_RECEIVED` — details table: Opportunity Name, Customer Name, Source/Channel,
  Submission Date, Submission Theme, Proactive/Reactive, Expected Value (SAR),
  New/Renewal, Relation with Client (1–5), Win Probability (1–5), each with a
  Comment column.
- `GW1_REJECTED` — greeting to the sender, statement that the opportunity did not
  pass the first qualification stage, then a bulleted list of rejection reasons
  taken from the gate decision `Reason`.
- `QUAL_REQUEST` — details table: Qualification Deadline, Opportunity Name,
  Customer Name, Source/Channel, Duration, Proposal Language, Submission Theme,
  Inquiries Deadline, Estimated Cost Deadline, Internal Deadline, Submission
  Deadline. Then a Scope of Work block: Scope Brief, and a table of Scope Item /
  Service Line / Comment. Asks the recipient to nominate the resource who will
  develop the response if qualified.
- `QUAL_MEETING_REQUEST` — details table: Opportunity Name, Customer Name,
  Duration, Source/Channel, Submission Theme, Submission Deadline, plus the Scope
  of Work block.
- `BUILDER_NOTIFICATION` — same field set as `QUAL_REQUEST` minus Qualification
  Deadline, plus the Scope of Work block; states that both the technical proposal
  and associated costing are required.
- `APPROVAL_REQUEST` — details table: Opportunity Name, Customer Name, Duration,
  Submission Date, Scope Brief (must reference the SoW and relevant service line),
  Price (SAR), Margin %.

### 8.1 Sending behaviour — draft-first (chosen approach)

- On each trigger, compose and persist a `GeneratedEmail` row:
  `Id`, `OpportunityId`, `TemplateCode`, `To`, `Cc`, `Subject`, `BodyHtml`,
  `Status` (`Draft`, `Queued`, `Sent`, `Failed`), `GeneratedAtUtc`,
  `GeneratedByUserId`, `SentAtUtc?`, `FailureReason?`, `AttachmentIds` (json).
- Default behaviour: **status `Draft` only — nothing leaves the system.**
- The frontend shows the rendered email with **Copy subject**, **Copy body (HTML)**,
  **Copy body (plain text)**, **Download .eml**, and **Mark as sent manually**.
- Raise a matching in-app notification for the recipient(s) so the workflow moves
  without email.
- Implement `IEmailSender` with `SmtpEmailSender` behind config flag
  `Email:SendingEnabled` (default `false`). When false, resolve a `NullEmailSender`
  that only logs. **Do not wire real SMTP credentials.** This keeps a future switch
  to real sending or Microsoft Graph a configuration change, not a rewrite.

---

## 9. Cross-cutting backend requirements

### 9.1 Authentication and authorisation

- **JWT is the active mechanism.** Local user store, ASP.NET Core Identity password
  hashing (PBKDF2), access token 60 min + refresh token 14 days with rotation and
  revocation. Claims: `sub`, `name`, `email`, `role` (multiple), `jti`, `exp`.
- Policy-based authorisation. Named policies per capability, e.g.
  `CanReviewGw1`, `CanDecideQualification`, `CanBuildProposal`, `CanApprove`,
  `CanSubmit`, `CanManageContract`, `CanAdminister`. Map policies to role codes in
  one place (`AuthorizationPolicies.cs`) so they are easy to re-map later.
- **SSO scaffolded but disabled.** Add config section:
  ```json
  "Authentication": {
    "Jwt":  { "Enabled": true,  "Issuer": "...", "Audience": "...", "Key": "..." },
    "EntraId": { "Enabled": false, "TenantId": "", "ClientId": "", "Authority": "" }
  }
  ```
  Register the Entra ID / OpenID Connect JWT bearer scheme **only when
  `EntraId:Enabled` is true**, with a `TODO(SSO)` comment at each integration
  point: scheme registration, user provisioning-on-first-login stub, role mapping
  from group claims, and the frontend login button. Ship it commented-in but
  inactive so activation is a later, isolated task. Do not implement Entra ID
  behaviour now beyond these stubs.

### 9.2 API conventions

- Base path `/api/v1`. Kebab-case route segments, camelCase JSON.
- All list endpoints: `?page=1&pageSize=25&sortBy=&sortDir=asc|desc&search=` plus
  entity-specific filters, returning
  `{ items: [], page, pageSize, totalCount, totalPages }`.
- Errors: RFC 7807 `ProblemDetails` with a `traceId` and, for validation, an
  `errors` dictionary keyed by field.
- Status codes: 200/201/204 success, 400 malformed, 401 unauthenticated,
  403 unauthorised, 404 not found, 409 illegal transition or concurrency conflict,
  422 business rule violation.
- Optimistic concurrency on Opportunity and Contract via `RowVersion`; return 409
  with the server's current values on conflict.
- Rate-limit auth endpoints (10 attempts / 5 min / IP).
- CORS: allow the frontend origin from config only.
- Health checks at `/health/live` and `/health/ready`.

---

## 10. API surface

Implement every endpoint below.

### Auth
```
POST   /api/v1/auth/login                     { email, password } → tokens + user profile
POST   /api/v1/auth/refresh                   { refreshToken }
POST   /api/v1/auth/logout
GET    /api/v1/auth/me                        current user, roles, permissions
POST   /api/v1/auth/change-password
GET    /api/v1/auth/sso/config                returns { ssoEnabled: false }  // TODO(SSO)
GET    /api/v1/auth/sso/challenge             501 Not Implemented            // TODO(SSO)
```

### Opportunities
```
GET    /api/v1/opportunities                  filters: stageId, statusId, customerId,
                                              ownerUserId, builderUserId, submissionTheme,
                                              sourceChannel, valueFrom, valueTo,
                                              submissionDateFrom, submissionDateTo,
                                              isClosed, myItemsOnly
GET    /api/v1/opportunities/{id}             full aggregate
GET    /api/v1/opportunities/{id}/summary     header card data
POST   /api/v1/opportunities                  create (AM) → auto number, initial status, gate, email
PUT    /api/v1/opportunities/{id}             update core fields
DELETE /api/v1/opportunities/{id}             soft delete (ADMIN only)
PUT    /api/v1/opportunities/{id}/deadlines
PUT    /api/v1/opportunities/{id}/owner
GET    /api/v1/opportunities/{id}/timeline    merged gates + status changes + audit, chronological
GET    /api/v1/opportunities/{id}/available-transitions
POST   /api/v1/opportunities/{id}/status      { toStatusId, reason? }  guarded by transition table
POST   /api/v1/opportunities/{id}/hold        { reason }
POST   /api/v1/opportunities/{id}/resume
POST   /api/v1/opportunities/{id}/cancel      { reason }
GET    /api/v1/opportunities/export           xlsx export of the filtered list
```

### Scope of work
```
GET    /api/v1/opportunities/{id}/scope
PUT    /api/v1/opportunities/{id}/scope                     brief
POST   /api/v1/opportunities/{id}/scope/items
PUT    /api/v1/opportunities/{id}/scope/items/{itemId}
DELETE /api/v1/opportunities/{id}/scope/items/{itemId}
PUT    /api/v1/opportunities/{id}/scope/items/reorder
```

### Gates and approvals
```
GET    /api/v1/opportunities/{id}/gates                     all instances, all rounds
GET    /api/v1/gates/{gateInstanceId}
POST   /api/v1/gates/{gateInstanceId}/decision              { decision, reason?, noteBody?, attachmentIds? }
POST   /api/v1/gates/{gateInstanceId}/reassign               { assignedUserId }
GET    /api/v1/approvals/pending                            current user's queue across all gates
GET    /api/v1/approvals/pending/count                      badge counter
GET    /api/v1/workflow/gates                               gate configuration (ADMIN)
PUT    /api/v1/workflow/gates/{id}                          (ADMIN)
GET    /api/v1/workflow/transitions                         (ADMIN)
```

### Qualification
```
POST   /api/v1/opportunities/{id}/qualification/route       { requiresQualificationMeeting: bool }
POST   /api/v1/opportunities/{id}/qualification/meeting     create + invite
GET    /api/v1/opportunities/{id}/qualification/meeting
PUT    /api/v1/opportunities/{id}/qualification/meeting
POST   /api/v1/opportunities/{id}/qualification/meeting/minutes
POST   /api/v1/opportunities/{id}/qualification/meeting/attendees
PUT    /api/v1/qualification/meeting/attendees/{id}/response
```

### Proposal development
```
POST   /api/v1/opportunities/{id}/builder                   { builderType, builderUserId }
GET    /api/v1/opportunities/{id}/estimated-cost
POST   /api/v1/opportunities/{id}/estimated-cost
GET    /api/v1/opportunities/{id}/sl-responses
POST   /api/v1/opportunities/{id}/sl-responses
PUT    /api/v1/sl-responses/{id}
POST   /api/v1/sl-responses/{id}/submit
POST   /api/v1/sl-responses/{id}/return                     { reason }
GET    /api/v1/opportunities/{id}/pricing                   all versions
POST   /api/v1/opportunities/{id}/pricing                   new version, margin computed server-side
POST   /api/v1/opportunities/{id}/proposal/ready            builder marks ready → opens PROPOSAL_REVIEW
GET    /api/v1/proposals/my-tasks                           builder's work queue
```

### Bid bond
```
GET    /api/v1/opportunities/{id}/bid-bond
POST   /api/v1/opportunities/{id}/bid-bond/request
POST   /api/v1/opportunities/{id}/bid-bond/issue            { amount, validUntil, issuingBank, attachmentId }
POST   /api/v1/opportunities/{id}/bid-bond/reject           { reason }
```

### Submission and outcome
```
POST   /api/v1/opportunities/{id}/submit                    { channel, reference, submittedAtUtc }
GET    /api/v1/opportunities/{id}/submission
POST   /api/v1/opportunities/{id}/outcome                   { result: Won|Lost, ... }
GET    /api/v1/opportunities/{id}/outcome
```

### Contracting
```
GET    /api/v1/contracts                                    filters: status, customerId, valueFrom/To,
                                                            signedFrom/To, expiringWithinDays
GET    /api/v1/contracts/{id}
POST   /api/v1/opportunities/{id}/contract                  create from a Won opportunity
PUT    /api/v1/contracts/{id}
POST   /api/v1/contracts/{id}/status                        { toStatus, reason? }
GET    /api/v1/contracts/{id}/negotiation-rounds
POST   /api/v1/contracts/{id}/negotiation-rounds
PUT    /api/v1/contracts/negotiation-rounds/{roundId}
POST   /api/v1/contracts/negotiation-rounds/{roundId}/close { status: Agreed|Rejected }
GET    /api/v1/contracts/{id}/milestones
POST   /api/v1/contracts/{id}/milestones
PUT    /api/v1/contracts/milestones/{id}
POST   /api/v1/contracts/{id}/sign                          { signedAtUtc, signatory, attachmentId }
```

### Notes, comments, attachments (generic — work for every owner type)
```
GET    /api/v1/{entityType}/{entityId}/notes
POST   /api/v1/{entityType}/{entityId}/notes                { body, visibility }
PUT    /api/v1/notes/{id}
DELETE /api/v1/notes/{id}

GET    /api/v1/{entityType}/{entityId}/comments             threaded
POST   /api/v1/{entityType}/{entityId}/comments             { body, parentCommentId?, mentionedUserIds? }
PUT    /api/v1/comments/{id}
DELETE /api/v1/comments/{id}                                soft delete, keeps audit

GET    /api/v1/{entityType}/{entityId}/attachments
POST   /api/v1/{entityType}/{entityId}/attachments          multipart: file + description (required) + category
GET    /api/v1/attachments/{id}                             metadata
GET    /api/v1/attachments/{id}/download                    streams file, writes AttachmentDownloaded audit
PUT    /api/v1/attachments/{id}                             edit description/category
DELETE /api/v1/attachments/{id}                             soft delete
```
`entityType` accepts `opportunities`, `gates`, `scope-items`, `contracts`.

### Audit
```
GET    /api/v1/audit                                        filters: entityType, entityId, opportunityId,
                                                            actorUserId, action, fromUtc, toUtc
GET    /api/v1/opportunities/{id}/audit
GET    /api/v1/audit/export                                 xlsx
```

### Emails
```
GET    /api/v1/opportunities/{id}/emails
GET    /api/v1/emails/{id}                                  rendered subject + body
POST   /api/v1/emails/{id}/regenerate                       re-render from current data
POST   /api/v1/emails/{id}/mark-sent                        manual confirmation
GET    /api/v1/emails/{id}/eml                              downloadable .eml
GET    /api/v1/email-templates                              (ADMIN)
PUT    /api/v1/email-templates/{code}                       (ADMIN)
POST   /api/v1/email-templates/{code}/preview               { opportunityId } → rendered
```

### Notifications
```
GET    /api/v1/notifications                                current user, unread first
POST   /api/v1/notifications/{id}/read
POST   /api/v1/notifications/read-all
GET    /api/v1/notifications/unread-count
```

### Lookups and admin
```
GET    /api/v1/lookups/stages
GET    /api/v1/lookups/statuses?stageId=
GET    /api/v1/lookups/source-channels
GET    /api/v1/lookups/submission-themes
GET    /api/v1/lookups/service-lines
GET    /api/v1/lookups/proposal-languages
GET    /api/v1/lookups/attachment-categories
GET    /api/v1/lookups/all                                  single bootstrap call for the SPA

GET    /api/v1/customers            POST /api/v1/customers
GET    /api/v1/customers/{id}       PUT  /api/v1/customers/{id}
GET    /api/v1/customers/{id}/contacts   POST .../contacts

GET    /api/v1/users                POST /api/v1/users               (ADMIN)
PUT    /api/v1/users/{id}           PUT  /api/v1/users/{id}/roles    (ADMIN)
POST   /api/v1/users/{id}/activate  POST /api/v1/users/{id}/deactivate
GET    /api/v1/roles
GET    /api/v1/service-lines        POST /api/v1/service-lines       (ADMIN)
```

### Dashboard and reports
```
GET    /api/v1/dashboard/kpis                   counts by stage/status, open value, win rate,
                                                avg cycle time per stage, overdue deadlines
GET    /api/v1/dashboard/my-work                pending gates + builder tasks + upcoming deadlines
GET    /api/v1/dashboard/pipeline               value grouped by stage
GET    /api/v1/reports/funnel                   created → qualified → submitted → won
GET    /api/v1/reports/win-loss                 grouped by theme, service line, customer, source
GET    /api/v1/reports/cycle-time               avg days per stage and per gate
GET    /api/v1/reports/sl-performance           SL response turnaround
GET    /api/v1/reports/{report}/export          xlsx
```

---

## 11. Frontend structure

```
frontend/
├── src/
│   ├── app/
│   │   ├── app.component.ts
│   │   ├── app.config.ts                     # providers, interceptors, router, translate
│   │   ├── app.routes.ts                     # top-level route table (section 12)
│   │   ├── core/
│   │   │   ├── auth/                         # auth.service, token.service, auth.store,
│   │   │   │                                 # auth.guard, role.guard, permission.directive
│   │   │   ├── http/                         # auth.interceptor, error.interceptor,
│   │   │   │                                 # loading.interceptor, retry.interceptor
│   │   │   ├── layout/                        # shell, sidenav, topbar, breadcrumbs,
│   │   │   │                                 # notification-bell, user-menu, lang-switcher
│   │   │   ├── services/                      # lookup.service (cached), notification.service,
│   │   │   │                                 # toast.service, confirm-dialog.service
│   │   │   ├── models/                        # shared TS interfaces mirroring the DTOs
│   │   │   └── utils/                         # date-time helpers, file-size, validators
│   │   ├── shared/
│   │   │   ├── components/
│   │   │   │   ├── data-table/                # server-side paging, sorting, filtering
│   │   │   │   ├── page-header/
│   │   │   │   ├── stage-status-badge/
│   │   │   │   ├── status-stepper/            # visual stage progress
│   │   │   │   ├── empty-state/
│   │   │   │   ├── confirm-dialog/
│   │   │   │   ├── file-upload/               # enforces description before upload
│   │   │   │   ├── attachment-list/
│   │   │   │   ├── notes-panel/               # reusable, takes entityType + entityId
│   │   │   │   ├── comments-thread/           # reusable, threaded, @mentions
│   │   │   │   ├── activity-log/              # reusable audit trail viewer
│   │   │   │   ├── approval-panel/            # note + comment + attach + decide, one component
│   │   │   │   ├── user-avatar/
│   │   │   │   ├── money-input/
│   │   │   │   ├── score-selector/            # 1–5 rating control
│   │   │   │   └── deadline-chip/             # colour-coded by proximity/overdue
│   │   │   ├── pipes/                         # localDateTime, timeAgo, sar, enumLabel, truncate
│   │   │   └── directives/                    # hasRole, hasPermission, autofocus
│   │   └── features/
│   │       ├── auth/                          # login, forgot-password, change-password
│   │       ├── dashboard/
│   │       ├── opportunities/
│   │       │   ├── list/
│   │       │   ├── create/
│   │       │   ├── detail/
│   │       │   │   └── tabs/                  # overview, scope, qualification, proposal,
│   │       │   │                              # approvals, attachments, discussion,
│   │       │   │                              # emails, activity, contract
│   │       │   ├── components/
│   │       │   └── services/
│   │       ├── qualification/                 # queue, gw1-review, sl-decision, meeting
│   │       ├── proposals/                     # my-tasks, builder-workspace, sl-responses,
│   │       │                                  # pricing, review
│   │       ├── approvals/                     # pending queue, approval detail
│   │       ├── submission/                    # submit form, outcome recording
│   │       ├── contracting/                   # list, detail, negotiation rounds, milestones, sign-off
│   │       ├── emails/                        # generated email viewer with copy actions
│   │       ├── notifications/
│   │       ├── reports/
│   │       └── admin/                         # users, roles, service-lines, customers,
│   │                                          # lookups, email-templates, workflow-config, audit
│   ├── assets/i18n/en.json, ar.json
│   ├── environments/environment.ts, environment.prod.ts
│   └── styles/                                # _variables, _theme, _rtl, _utilities, styles.scss
```

Rules:
- Every feature folder is lazy-loaded via `loadChildren` pointing at a `*.routes.ts`.
- Feature services use `HttpClient` + typed DTOs; no `any`.
- The four collaboration components (`notes-panel`, `comments-thread`,
  `attachment-list`, `activity-log`) are written **once** and reused on every
  approval level, the opportunity itself, scope items, and contracts. Do not
  duplicate them per feature.

---

## 12. Routes

| Path | Component | Guard | Notes |
|---|---|---|---|
| `/auth/login` | LoginPage | anonymous only | SSO button rendered but disabled with tooltip "Coming soon" |
| `/auth/change-password` | ChangePasswordPage | auth | |
| `/` | redirect → `/dashboard` | auth | |
| `/dashboard` | DashboardPage | auth | KPIs, my work, pipeline |
| `/opportunities` | OpportunityListPage | auth | filters persisted in query params |
| `/opportunities/new` | OpportunityCreatePage | role `AM`, `BIDS_PRESALES`, `ADMIN` | |
| `/opportunities/:id` | OpportunityDetailPage | auth | shell with child tab routes |
| `/opportunities/:id/overview` | OverviewTab | auth | default child |
| `/opportunities/:id/scope` | ScopeTab | auth | |
| `/opportunities/:id/qualification` | QualificationTab | auth | |
| `/opportunities/:id/proposal` | ProposalTab | auth | |
| `/opportunities/:id/approvals` | ApprovalsTab | auth | all gate rounds, expandable |
| `/opportunities/:id/attachments` | AttachmentsTab | auth | |
| `/opportunities/:id/discussion` | DiscussionTab | auth | notes + comments |
| `/opportunities/:id/emails` | EmailsTab | auth | |
| `/opportunities/:id/activity` | ActivityTab | auth | audit log |
| `/opportunities/:id/contract` | ContractTab | auth | visible only when Won |
| `/opportunities/:id/edit` | OpportunityEditPage | role-guarded | |
| `/qualification` | QualificationQueuePage | role `BIDS_PRESALES`, `BIDS_MGMT`, `PRESALES`, `SL` | |
| `/qualification/:id/gw1-review` | Gw1ReviewPage | role `BIDS_PRESALES` | |
| `/qualification/:id/decision` | SlDecisionPage | role `SL`, `PRESALES` | |
| `/qualification/:id/meeting` | MeetingPage | role `BIDS_MGMT` | schedule, attendees, minutes, outcome |
| `/proposals/my-tasks` | MyTasksPage | role `PRESALES`, `SL` | |
| `/proposals/:id/workspace` | BuilderWorkspacePage | role `PRESALES`, `SL` | TP + costing + SL responses |
| `/proposals/:id/pricing` | PricingPage | role `PRESALES` | price, cost, margin, versions |
| `/proposals/:id/review` | ProposalReviewPage | role `PRESALES` | approve / return |
| `/approvals` | PendingApprovalsPage | auth | queue across all gates for my roles |
| `/approvals/:gateInstanceId` | ApprovalDetailPage | gate-role guard | the approval panel |
| `/submission/:id` | SubmissionPage | role `BIDS_MGMT` | |
| `/submission/:id/outcome` | OutcomePage | role `BIDS_MGMT`, `AM` | Won / Lost |
| `/contracting` | ContractListPage | role `BIDS_MGMT`, `MGMT`, `ADMIN` | |
| `/contracting/:id` | ContractDetailPage | same | negotiation, milestones, sign-off |
| `/notifications` | NotificationsPage | auth | |
| `/reports` | ReportsHomePage | role `BIDS_MGMT`, `MGMT`, `ADMIN` | |
| `/reports/funnel` · `/win-loss` · `/cycle-time` · `/sl-performance` | report pages | same | |
| `/admin/users` · `/roles` · `/customers` · `/service-lines` · `/lookups` · `/email-templates` · `/workflow` · `/audit` | admin pages | role `ADMIN` | |
| `/403` | ForbiddenPage | — | |
| `/**` | NotFoundPage | — | |

Guards: `authGuard` (canActivate), `roleGuard` (canActivate, reads `data.roles`),
`gateRoleGuard` (resolves the gate, checks the responsible role),
`unsavedChangesGuard` (canDeactivate on all create/edit forms).

---

## 13. Approval panel — the core reusable UI

Every approval level in the application renders the same `<approval-panel>`
component. It must contain, in this order:

1. **Context header** — opportunity number, name, customer, current stage/status
   badge, gate name, round number, assigned role, opened date/time, SLA/age.
2. **Decision area** — the decisions allowed by that gate's configuration, radio
   or button group. Reason textarea, required when the decision is a rejection or
   return.
3. **Notes** — add a note (internal or shared), list of existing notes with author,
   role, and full date/time. Editable by author within 15 minutes, then locked.
4. **Comments** — threaded discussion with `@mention` autocomplete over users,
   reply, edit, soft delete. Each entry shows author, role, and date/time.
5. **Attachments** — drag-and-drop upload. **The description field is required and
   the upload button stays disabled until it is filled.** Each row shows file name,
   description, category, size, uploader, and upload date/time, with download and
   delete actions.
6. **Activity log** — the audit trail scoped to this gate instance, newest first,
   each line showing actor, role, action, from → to value, and exact date/time.
7. **Submit decision** — one confirmation dialog summarising what will happen
   (status change, email generated, who gets notified) before committing.

All timestamps display as `dd MMM yyyy, HH:mm` in the user's local timezone, with
a tooltip showing the UTC value and a relative "3 hours ago" label.

---

## 14. UI/UX requirements

- Responsive down to 768 px; the data table collapses to cards on small screens.
- Left sidenav grouped: Dashboard · Opportunities · Qualification · Proposals ·
  Approvals · Contracting · Reports · Admin. Badge counters on Approvals and
  Proposals fed by the count endpoints.
- Opportunity detail page shows a persistent header card plus a horizontal
  `status-stepper` visualising the four stages with the current status highlighted.
- Deadline chips colour-coded: grey (no date), green (> 7 days), amber (≤ 3 days),
  red (overdue).
- Global loading indicator driven by the loading interceptor; skeleton loaders on
  table and detail pages.
- Toast notifications for every successful mutation, with an Undo affordance where
  the backend supports it.
- Full keyboard accessibility, visible focus rings, ARIA labels, WCAG AA contrast.
- Language switcher toggling EN/AR; switching to Arabic sets `dir="rtl"` on
  `<html>` and mirrors the layout. All labels come from the i18n files — **no
  hardcoded display strings in templates**.
- Empty states with a clear call to action on every list.

---

## 15. Non-functional requirements

- All dates stored and transmitted as UTC (`DateTime` with `DateTimeKind.Utc`).
  Never store local time. Never trust a client-supplied timestamp for audit.
- Soft delete on Note, Comment, Attachment, Opportunity, Customer, User. Never
  hard delete anything that appears in the audit log.
- Every mutating endpoint writes at least one `AuditLog` row inside the same
  transaction.
- Serilog request logging with correlation id; propagate `traceId` to the frontend
  and show it on the error page for support.
- Unit tests: workflow engine transitions (every legal and illegal path), margin
  calculation, opportunity number generation, validators. Minimum 60% line
  coverage on `Crm.Application`.
- Integration tests: full happy path (create → GW1 approve → qualify → build →
  review → approve → submit → won → contract signed) and the two loop-back paths.
- `README.md` at the repo root: prerequisites, how to run migrations, how to seed,
  how to run both projects, how to run tests, environment variables table, and a
  section titled **"Enabling SSO later"** documenting every `TODO(SSO)` location.
- `docker-compose.yml`: `sqlserver`, `api`, `web`.
- `.editorconfig`, `.gitignore`, ESLint + Prettier for the frontend, and a
  GitHub Actions workflow that builds both projects and runs tests.

---

## 16. Build order

Deliver in these phases, each ending in a runnable state:

1. **Foundation** — solution scaffolding, both projects, EF Core + SQL Server,
   `User`/`Role`, JWT auth, Swagger, Angular shell with login and sidenav, i18n
   wiring, lookup bootstrap endpoint.
2. **Opportunity core** — full opportunity CRUD, customers, service lines, scope
   of work, stage/status seeds and transition enforcement, list + create + detail
   overview screens.
3. **Collaboration layer** — notes, comments, attachments with mandatory
   descriptions, audit log, and the four reusable Angular components. Build this
   *before* the gates so the approval panel has everything it needs.
4. **Workflow engine and gates** — configurable engine, all six gates, the
   `<approval-panel>`, GW1 review, qualification routes, qualification meeting.
5. **Proposal development** — builder assignment, deadlines, estimated cost, bid
   bond, SL responses, pricing versions with margin, proposal review with the
   return loop.
6. **Approval, submission, outcome** — management approval with its rejection
   loop, submission with its blocking rules, Won/Lost outcome recording.
7. **Contracting** — contract creation from Won, negotiation rounds, milestones,
   sign-off gate, Contract Signed terminal state.
8. **Emails and notifications** — six templates, draft generation, the email
   viewer with copy/.eml/mark-sent, in-app notifications, template admin.
9. **Dashboard, reports, admin, exports** — KPIs, four reports, xlsx exports,
   all admin screens including workflow configuration.
10. **Hardening** — tests to the stated coverage, docker-compose, CI, README,
    accessibility pass, Arabic RTL pass.

**Do not enable SSO in any phase.** It is a separate follow-up task that will be
ordered once the JWT-based system is verified. Leave the `TODO(SSO)` stubs and
config flags in place and documented.

---

## 17. Definition of done

- A user can complete the entire lifecycle end to end in the UI, from creating an
  opportunity to a signed contract, without touching the database.
- Both rejection paths (GW1 reject, qualification not passed) close the
  opportunity correctly and generate the right draft email.
- Both loop-back paths (proposal returned, approval rejected) return the work to
  the builder, increment the gate round, and preserve the full history of prior
  rounds.
- Every one of the six gates offers notes, comments, attachments with
  descriptions, and a complete timestamped action log.
- The audit log for a completed opportunity reconstructs the entire history: who
  did what, when, and what changed.
- Illegal status transitions and unauthorised gate decisions are rejected by the
  API, not just hidden in the UI.
- The application is fully usable in Arabic with correct RTL layout.
- `docker-compose up` produces a working system with seeded demo data.
