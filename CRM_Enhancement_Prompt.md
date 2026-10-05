# Enhancement Prompt — Opportunity Lifecycle CRM v2 (Odoo-grade UX)

> Companion to `CRM_Build_Prompt.md`. That document remains the domain/workflow
> specification. This document is the **change order**: what is broken, what the
> business owner asked for, and what must be built to reach an Odoo-CRM-class
> experience. Build in the phases of section 9. Every phase ends runnable.

Source inputs: `Business and Notes/Notes from business owner.txt`,
`Opportunity Journey V 0.4.vsdx`, `Opportunity Journey - Email Template N 0.3.docx`,
`Stage-Status Mapping.xlsx`, and a full code audit of `/backend` and `/frontend`.

---

## 0. Audit verdict (why the owner is unhappy)

The engine and data model are sound. The **experience layer is an MVP**: users are
dropped on a record with nine tabs and no guidance, the work is split across four
separate queues, several dashboard widgets are silently broken because the Angular
DTOs do not match the API DTOs, and power-user features (search, filters, grouping,
saved views, custom exports, charts) do not exist.

### 0.1 Defects that must be fixed first (Phase 0)

| # | Defect | Where |
|---|---|---|
| D1 | Dashboard KPI cards read `openCount`, `byStage[].stageCode`; API returns `TotalOpen`, `ByStage[].Key` → cards blank | `dashboard.page.ts` / `DashboardAdminScopeHandlers.cs` L95 |
| D2 | Pipeline widget expects `{ items: [...] }`; API returns a bare array → `p.items.length` throws, widget never renders | `dashboard.page.ts` L72 / `FeatureController.cs` L192 |
| D3 | "My work → builder tasks" reads `opportunityId/opportunityName`; API returns `Id/Name` → broken links | `dashboard.page.ts` L124–130 |
| D4 | Sidenav badges: `pendingCount` expects `{count}` (API returns `int`); `myTasks` expects `PagedResult` (API returns list) → badges always 0 | `sidenav.component.ts` L315–322 |
| D5 | Overview tab reads `o.deadlines.qualificationDeadline`; API returns flat `qualificationDeadline` → runtime error, deadlines never shown | `overview.tab.ts` L29–33 / `OpportunityDetailDto` |
| D6 | Win/Loss report reads `groupKey/groupLabel/winRate`; API returns `Group/Won/Lost/WonValue/LostValue` | `report-pages.ts` L57–63 |
| D7 | Cycle-time report returns `AvgDays = 0` for every row; SL performance hard-codes `AvgHours = 24` | `DashboardAdminScopeHandlers.cs` L181–191 |
| D8 | Hold / Cancel reason captured with `window.prompt('Reason')` — English-only, not a Material dialog, record not refreshed after action | `opportunity-detail.page.ts` L123, L135 |
| D9 | Assign builder = free-text box; the value is looked up **by email** server-side and NREs when not found | `proposal.tab.ts` L33–35 / `ProposalSubmissionHandlers.cs` L50–55 |
| D10 | `ScopeBrief` accepted by `CreateOpportunityCommand` but the create form never sends it | `opportunity-form.component.ts` |
| D11 | `sortBy` ignored (only `CreatedAtUtc`); `submissionDateFrom/To` filter `CreatedAtUtc` | `OpportunityHandlers.cs` L227–241 |
| D12 | Search matches `Name`, `Number`, `Customer.NameEn` only — never Arabic names, owner, status | `OpportunityHandlers.cs` L232–236 |
| D13 | Export hard-capped at 200 rows with fixed columns | `FeatureController.cs` L198–218 |
| D14 | `GET /users` admin policy commented out — every user can enumerate users | `FeatureController.cs` L180 |
| D15 | 9 of 11 authorization policies are defined but applied to **no** endpoint; most mutations rely on class-level `[Authorize]` only | `Common.cs` L78–106 |
| D16 | `RequiresAttachmentOnApprove` seeded on gates but never enforced | `WorkflowEngine.cs` |
| D17 | Notification titles hard-coded English (`"Gate {x} pending"`, `"You have been assigned as builder"`) | `WorkflowEngine.cs` L60–67, L209 |
| D18 | Each detail tab re-fetches the opportunity; no shared record store; no skeleton loaders | `detail/tabs/*.ts` |
| D19 | `/submission/:id` and `/submission/:id/outcome` are reachable from nowhere (no nav, no CTA) | `app.routes.ts`, sidenav |
| D20 | `POST /opportunities/{id}/status` can jump `InternalReview → Submitted` via the transition table, bypassing gate side-effects | `LookupSeeder` transitions |

Ship a **shared DTO contract**: generate the Angular models from the API (NSwag /
openapi-typescript) or add a contract test that fails CI when the two drift.

---

## 1. Business owner request → required change

### 1.1 Creating a new opportunity
1. **Customer quick-create**: replace the customer `<mat-select>` with a searchable
   autocomplete (EN/AR, sector). When there is no match show *"Create "{text}""* as
   the last option → opens a compact dialog (NameEn, NameAr, Sector, IsGovernment,
   Website, first contact). On save the new customer is selected in-place. Any
   role that can create an opportunity can quick-create a customer; full customer
   editing stays in Admin.
2. **Attachments at creation**: the create page becomes a 3-step wizard
   *Details → Scope & Attachments → Review*. Files are staged client-side with the
   mandatory description + category; on *Create* the opportunity is POSTed, then
   staged files are uploaded to `/opportunities/{id}/attachments`, with per-file
   progress and retry. If any upload fails the record is still created and the
   user is told which files to retry. RFP is a suggested category on step 2.
3. **Scope description field**: add `Scope Brief` (multiline, required, min 20
   chars) and **Scope Items** (title + service line, at least one) to step 2. Send
   `ScopeBrief` and items in the create command (extend it with
   `ScopeItems: [{Title, ServiceLineId, Comment?}]`).
4. Field help: inline hints under Relation/Win scores ("1 = very weak · 5 = very
   strong"), Submission Theme options rendered as chips with descriptions.

### 1.2 Opportunity stage visibility
1. Replace the 4-dot stage stepper with an **Odoo-style status bar + journey map**
   in the record header:
   - Top row: the four stages as arrow segments (done / current / upcoming /
     terminal-red for Not Qualified, Lost, Canceled; amber for Hold).
   - Under the current stage: the **statuses of that stage** with the current one
     highlighted (`Awaiting Assessment → Qualified`, etc.).
   - A collapsible **Journey** panel listing every workflow step from the Visio
     (GW1 Review, Qualification Route, SL Decision / Qualification Meeting,
     Builder Assignment, Deadlines, Estimated Cost, Bid Bond, Proposal Build,
     Proposal Review (round n), Management Approval (round n), Submission, Outcome,
     Contract) with ✔ done (who / when), ● in progress (pending on whom, age),
     ○ not started, ⤼ skipped (with reason). Data comes from a new
     `GET /opportunities/{id}/journey` endpoint computed server-side from
     gates + audit + status.
2. **List view**: add columns *Stage*, *Status* (coloured badge), *Pending on*
   (role/user), *Next action*, *Age in stage*, *Submission deadline* chip.
3. A **Kanban view** grouped by stage (columns) or status, cards showing
   number, name, customer, value, pending-on avatar, deadline chip, bid-bond
   flag. Drag between columns is allowed only when a legal transition exists for
   the user's role; otherwise the card snaps back with the reason.

### 1.3 Navigation & next action
1. **Action Center** on the record header, fed by a new
   `GET /opportunities/{id}/next-actions` that returns, for the current user:
   `{ headline, description, responsibleRole, responsibleUser, canCurrentUserAct,
   primaryAction: {code, labelKey}, secondaryActions[], blockers[] }`.
   Examples: *"Waiting for 1st Gateway Review by Bids & Presales (2 days)"* with
   **Review now** for that role; *"Proposal in progress — builder Omar; internal
   deadline in 3 days"* with **Mark proposal ready**; *"Approved by Management —
   ready to submit"* with **Submit** and a blocker *"Bid bond not issued"*.
2. **Every workflow action runs inline** on the record (dialog or side sheet) —
   GW1 review, qualification route + SL decision, meeting scheduling & outcome,
   builder assignment + deadlines, estimated cost, bid bond, mark ready, proposal
   review, management approval, submission, outcome, contract. The separate pages
   under `/qualification`, `/proposals`, `/approvals/:id`, `/submission` become
   deep-links that open the same dialogs on the record, then redirect to the
   record.
3. **Chained decisions** so the user is never left wondering: GW1 *Approve* asks
   *"Does this require a qualification meeting?"* in the same dialog and opens the
   right gate; *Qualified* / *Passed* immediately opens *Assign builder & set
   deadlines*; Management *Approve* offers *Submit now*.
4. Replace the four queues with one **My Work inbox** (`/inbox`): tabs *To do*
   (gates I can decide, proposals I build, SL responses I owe, submissions I own,
   bid bonds I must issue), *Waiting on others* (records I own that are pending
   elsewhere), *Overdue*, *Done this week*. Sidenav: Dashboard · Inbox (badge) ·
   Opportunities · Customers · Contracts · Reports · Admin.
5. Hold / Resume / Cancel: proper reason dialog (localized), *Resume* button
   visible when on Hold, record re-fetched after every mutation, undo toast where
   the API allows.
6. A single `OpportunityRecordStore` (signal-based) loaded once by the detail
   shell and shared by all tabs; skeleton loaders on header and tabs.

### 1.4 Service line names
1. Service lines are business data, not seed constants: full Admin CRUD
   (`PUT /service-lines/{id}`, `POST /service-lines/{id}/deactivate`), fields
   `Code, NameEn, NameAr, LeadUserId, IsActive, SortOrder, ColourHex`. Inactive
   lines are hidden from pickers but keep history.
2. Seed the **names supplied by the business owner** (to be provided; current seeds
   Cybersecurity / Digital Transformation / Cloud / Data & AI / Managed Services
   are placeholders). Add a one-off migration that renames existing rows by code.
3. Show service line on scope items, SL responses, kanban cards, and as a
   group-by / filter everywhere.

### 1.5 Permissions and data visibility
1. **Row-level visibility policy** (server-side, applied to list, detail, export,
   dashboard, reports, search, notifications):
   - `AM`: opportunities they submitted or own, plus their team's if a team is
     configured (add optional `Team` entity: `Id, Name, LeadUserId, Members`).
   - `SL`: opportunities where any scope item / SL response / meeting invitation
     references their service line(s) (add `UserServiceLine` mapping).
   - `PRESALES`, `BIDS_PRESALES`, `BIDS_MGMT`, `MGMT`, `ADMIN`: all.
   - Implement as one `IOpportunityVisibility.Apply(IQueryable<Opportunity>)` used
     by every handler; add integration tests per role.
2. **Field-level visibility**: `EstimatedCost`, `ProposalPricing` (price, cost,
   margin), `SlResponse.CostSar`, contract value & payment terms are returned
   only to `PRESALES`, `BIDS_MGMT`, `MGMT`, `ADMIN`. Others receive `null` and the
   UI hides the section (`*crmHasPermission`). AM sees expected value only.
3. **Apply the existing policies to endpoints**: every mutating route gets its
   `[Authorize(Policy = …)]`; `GET /users` restored to `CanAdminister` (add a
   `GET /users/pickable?role=` endpoint for pickers that returns `Id, DisplayName,
   Roles` only). Enforce `RequiresAttachmentOnApprove`.
4. **Admin → Permissions matrix** screen: rows = capabilities (policies), columns
   = roles, editable checkboxes persisted to a `RolePermission` table that
   `AuthorizationPolicies` reads at startup (with cache invalidation). Includes
   the visibility rules above as toggles (e.g. *"AM can see pricing"*).
5. Audit every denied request (403) with actor, policy, resource.

### 1.6 Reporting & dashboards
1. **Dashboard** (role-aware, date-range picker, "compare to previous period"):
   - KPI tiles: open opportunities & value, won YTD (count/value), win rate,
     average cycle time (created → submitted), overdue deadlines, pending
     approvals older than N days.
   - Charts (ng2-charts / Chart.js): pipeline value by stage (funnel), open value
     by submission theme, by service line, by customer (top 10), win/loss trend
     by month, aging buckets per stage, deadline calendar (next 30 days).
   - Click-through: every bar/segment navigates to the list pre-filtered.
   - Per-role variants: AM sees own pipeline; SL sees its service line's load;
     MGMT sees approvals waiting and margin distribution.
2. **Reports** with real numbers and filters (date range, stage, status, theme,
   source, customer, service line, owner, builder):
   - Funnel (created → GW1 passed → qualified → submitted → won) with conversion %
     per step and drop-off reasons.
   - Win/Loss by theme / source / customer / service line / owner, with values,
     win rate, average deal size, top loss reasons.
   - Cycle time: real average & median days per stage and per gate, computed from
     `AuditLog` `StageChanged` / `GateOpened→Decided` pairs; SLA breaches.
   - SL performance: real turnaround (`SlResponse.DueAtUtc` vs `SubmittedAtUtc`),
     on-time %, returned count.
   - Deadline compliance: submission deadline met/missed per month.
   - Approval throughput: pending/decided per gate, average decision time, rounds.
3. **Pivot / report builder** (Odoo-style): pick *rows* (stage, status, customer,
   theme, source, service line, owner, builder, month created, month submitted),
   *columns* (same list), *measure* (count, expected value, awarded value,
   margin %, avg cycle days). Rendered as a pivot table with drill-down and
   exported to xlsx with the same layout. Backend: `POST /reports/pivot`.
4. **Export everywhere** (list, kanban, every report, pivot): see §2.4.

---

## 2. Odoo-class list experience (Opportunities, Customers, Contracts, Users)

### 2.1 Smart search bar
- One search box. Typing shows a dropdown *"Search Name for: x"*, *"Search
  Number for: x"*, *"Search Customer for: x"*, *"Search Owner for: x"*, *"Search
  Service Line for: x"*. Selecting adds a **facet chip**; multiple chips of the
  same field are OR-ed, different fields are AND-ed. Plain Enter searches
  Name + Number + Customer (EN + AR) + Owner + Builder.
- API: `GET /opportunities?filter=<json>` where `filter` is a domain expression
  `[["field","op",value], "&"|"|", …]` supporting ops `=, !=, in, not in, like,
  ilike, >, >=, <, <=, between, is null, is not null`. Whitelist of filterable
  fields with types, exposed by `GET /meta/opportunities/fields` (also drives the
  custom-filter UI, the column chooser, the export wizard and the pivot).

### 2.2 Filters dropdown
- Predefined: *My opportunities*, *My team*, *Open*, *Closed*, *Won*, *Lost*,
  *On hold*, *Overdue deadline*, *Pending on me*, *Created this month/quarter/year*,
  *Submission deadline in 7 days*, *Bid bond required*, per-stage and per-status.
- **Add custom filter**: field → operator → value rows with AND/OR, nested
  groups, date presets (today, this week, last 30 days, this quarter, custom).
- **Group by**: stage, status, customer, owner, builder, submission theme,
  source, engagement type, opportunity type, service line, created month,
  submission-deadline month; multi-level (e.g. stage › owner). Group rows show
  count and sum of value; collapsible.
- **Favorites**: save current search + filters + group-by + sort + columns as a
  named view; *personal* or *shared with roles*; *use as default*. Persisted in
  `SavedView (Id, UserId?, Name, EntityType, Definition JSON, IsShared,
  IsDefault, SortOrder)`. Views appear in the sidenav under Opportunities.

### 2.3 Table
- Migrate `crm-data-table` to `mat-table` with server-side sort on **every**
  column, sticky header, row density toggle, optional columns (**column
  chooser** menu, persisted per user per view), column reorder & resize,
  row multi-select with bulk actions (assign owner, set deadlines, export
  selected, hold/cancel with reason — all authorization-checked per row),
  inline quick-edit for simple fields (owner, expected value, deadlines), keyboard
  navigation, footer totals for numeric columns.
- View switcher on every list: **List · Kanban · Calendar** (deadlines) ·
  **Pivot** · **Graph**; state kept in the URL so views are shareable.
- Record pager on the detail page (« ‹ 3 / 48 › ») navigating within the list
  context.

### 2.4 Export wizard (the "customize fields for Excel" requirement)
- *Export* opens a dialog: left = all available fields (including related:
  `Customer › Sector`, `Deadlines › Submission`, `Pricing › Margin %`,
  `Builder › Name`, `Scope › Service lines`), right = selected fields, drag to
  order, checkbox *"I want to update data (import-compatible)"*, format
  **xlsx / csv**, scope *current page / all filtered rows / selected rows*, and
  *"Save as export template"* (personal/shared). Respects field-level
  permissions. Runs server-side (`POST /export/opportunities`) with streaming
  and no row cap; over 5 000 rows it runs as a background job and notifies with
  a download link. Same wizard for customers, contracts, audit, and reports.
- Optional follow-up: **Import** wizard (xlsx template download, column mapping,
  validation preview) for customers and opportunities.

### 2.5 Record page ("form view")
- Header: status bar (§1.2), Action Center (§1.3), **smart buttons** with counts
  (Attachments, Emails, Approvals, Notes, Scope items, Contract), star/follow.
- **Chatter** (Odoo-style) replacing the Discussion / Activity / Emails tabs:
  single reverse-chronological feed of notes, comments (@mentions), status &
  gate changes, attachments, generated emails; filter chips; "Log note" vs
  "Send message"; followers list; per-record notification subscription.
- **Activities**: schedule an activity (call, meeting, to-do, follow-up) with
  due date and assignee; shows on the record, in the inbox, on the dashboard
  calendar; overdue in red. Entity `Activity (Id, OpportunityId, Type, Summary,
  DueAtUtc, AssignedUserId, DoneAtUtc, Note)`.
- Tabs: Overview (editable in place), Scope, Proposal & Pricing (permission
  gated), Approvals (rounds timeline), Attachments (table with preview), Contract.

---

## 3. Backend additions

| Area | Work |
|---|---|
| Query engine | Generic `DomainFilter` parser → `Expression<Func<T,bool>>` with field whitelist; `GroupBy` with aggregates; `SortBy` any whitelisted field; reusable for opportunities, customers, contracts, audit |
| Metadata | `GET /meta/{entity}/fields` → `{name, labelEn, labelAr, type, operators[], relation?, groupable, sortable, exportable, requiredPermission?}` |
| Saved views / export templates | `SavedView`, `ExportTemplate` entities + CRUD |
| Journey / next actions | `GET /opportunities/{id}/journey`, `GET /opportunities/{id}/next-actions` |
| Visibility | `IOpportunityVisibility`, `Team`, `UserServiceLine`; field-masking in DTO mappers |
| Permissions | `RolePermission` table, admin CRUD, policy provider reading it |
| Service lines | full CRUD + deactivate + reorder |
| Activities | `Activity` entity + CRUD + overdue query |
| Reports | real cycle-time & SL turnaround; date-range filters on all; pivot endpoint; xlsx per report (ClosedXML, styled headers, frozen row, auto-filter, number formats) |
| Export | streaming exporter, background job for large exports (Hangfire or hosted service), `ExportJob` status endpoint |
| Notifications | template-based, localized (`NotificationTemplate` with EN/AR), link to the record + action |
| Workflow | enforce `RequiresAttachmentOnApprove`; remove transition rows that bypass gates; chained-decision commands (`GW1ApproveWithRoute`, `QualifyAndAssignBuilder`) executed in one transaction |
| Controllers | split `FeatureController` into per-feature controllers; add `[Authorize(Policy)]` everywhere |
| Contract safety | OpenAPI → TypeScript model generation in `npm run gen:api`; CI fails on drift |

---

## 4. Frontend additions

- `core/query/`: `DomainFilter` builder, facet model, saved-view store.
- `shared/components/`: `smart-search-bar`, `filter-menu`, `group-by-menu`,
  `favorites-menu`, `column-chooser`, `view-switcher`, `kanban-board`,
  `pivot-table`, `chart-card`, `export-dialog`, `status-bar`, `journey-panel`,
  `action-center`, `chatter`, `activity-scheduler`, `user-picker`
  (role-filtered, avatar, search), `customer-picker` (with quick-create),
  `reason-dialog`, `record-pager`, `skeleton`.
- `features/inbox/` replaces qualification/proposals/approvals queue pages.
- All strings via i18n (EN/AR) including notification and next-action texts;
  RTL verified on kanban, pivot, and status bar.
- Charts: `ng2-charts` with RTL-aware legends and SAR number formatting.

---

## 5. Data-model changes (migrations)

`Team`, `TeamMember`, `UserServiceLine`, `SavedView`, `ExportTemplate`,
`RolePermission`, `Activity`, `NotificationTemplate`, `ExportJob`;
`ServiceLine` + `IsActive, SortOrder, ColourHex`; `Customer` + `CreatedByUserId`
(already via audit) and `IsQuickCreated`; `Opportunity` + `LastActivityAtUtc`,
`StageEnteredAtUtc` (for age-in-stage), `NextActionCode` (denormalised for list
performance, recomputed by the engine on every state change).

---

## 6. Non-functional

- All list/export/report queries paginate or stream; no `ToListAsync()` of whole
  tables (current KPI/funnel/win-loss handlers load every opportunity).
- Indexes: `Opportunity(StageId, StatusId)`, `(OwnerUserId)`, `(BuilderUserId)`,
  `(CustomerId)`, `(CreatedAtUtc)`, `Deadlines.SubmissionDeadline`, `GateInstance(OpportunityId, State)`,
  `AuditLog(OpportunityId, Action, OccurredAtUtc)`.
- Tests: visibility per role, domain-filter parser, journey/next-action
  computation for every status, export wizard field permission masking, contract
  drift test, Cypress/Playwright happy path through the inline action dialogs.
- Accessibility pass on new components (keyboard on kanban & pivot).

---

## 7. Definition of done (v2)

1. An AM can create an opportunity with a brand-new customer, scope brief, scope
   items and attachments in one flow without leaving the page.
2. Anyone opening a record sees: stage, status, where it is in the journey, who
   must act next, and — if it is them — a single primary button that performs it.
3. No workflow step requires navigating to a separate queue page; the Inbox shows
   everything a user owes.
4. Every list supports facet search, predefined + custom filters, multi-level
   group-by, saved favorites, column chooser, column sort, kanban, and a
   field-selectable Excel export with no row cap.
5. AM/SL users cannot retrieve records or fields outside their visibility rules
   via UI **or** API (verified by integration tests).
6. Dashboard and all six reports show real, filterable numbers with charts and
   drill-down; pivot builder exports what it displays.
7. Service lines carry the business-approved names and are maintainable in Admin.
8. Zero frontend/backend contract mismatches (generated models; CI drift check).
9. Fully usable in Arabic with RTL on every new component.

---

## 8. Open questions for the business owner

1. Final **service line names** (EN + AR) and their leads.
2. Should AMs see **only their own** opportunities or their **team's**? Do teams exist?
3. Confirm **field-level rule**: AM and SL must not see price, cost, margin, estimated cost. Correct?
4. Should SL users see the **whole opportunity** when their line is in scope, or only their scope items / SL response?
5. Attachments at creation: is an **RFP mandatory** for Reactive/Etimad opportunities?
6. Any additional **status** needed (e.g. *Draft* before *Awaiting Assessment*, *Awarded* between Won and Contract)?
7. Which KPIs matter most on the management dashboard (win rate, pipeline value, cycle time, SL turnaround, deadline compliance)?

---

## 9. Build order

| Phase | Scope | Outcome |
|---|---|---|
| **0 — Stabilise** | D1–D20, generated API models, per-feature controllers, policies applied, localized notifications | Everything that exists actually works |
| **1 — Guided journey** | status bar + journey panel, next-actions API, Action Center, inline action dialogs, chained decisions, Inbox, record store & skeletons, reason dialogs | Owner complaint "journey/next step is not clear" resolved |
| **2 — Create flow & master data** | create wizard, customer quick-create, attachments at creation, scope brief/items, service-line CRUD + renames | Owner complaint on creation resolved |
| **3 — Permissions** | row-level visibility, field masking, permission matrix admin, `/users/pickable`, 403 audit | Owner complaint on permissions resolved |
| **4 — Odoo list experience** | domain-filter engine, metadata, smart search, filters, group-by, favorites, column chooser, `mat-table`, kanban, export wizard, saved templates | Odoo-class search & export |
| **5 — Reporting** | real metrics, date filters, charts, role-aware dashboard, six reports, pivot builder, per-report xlsx, background export | Owner complaint on reporting resolved |
| **6 — Chatter & polish** | chatter, activities, smart buttons, record pager, calendar view, import wizard, Arabic/RTL pass, e2e tests | Odoo-class record page |
