import {
  AttendeeResponse,
  BidBondStatus,
  BuilderType,
  ContractStatus,
  GeneratedEmailStatus,
  MeetingOutcome,
  NegotiationRoundStatus,
  OutcomeResult,
  SlResponseStatus,
  SubmissionChannel
} from './enums';

export interface EstimatedCost {
  id: string;
  opportunityId: string;
  amount?: number | null;
  currency: string;
  submittedByUserId: string;
  submittedByName?: string;
  submittedAtUtc: string;
  notes?: string | null;
  pricingVisible?: boolean;
}

export interface ProposalPricing {
  id: string;
  opportunityId: string;
  priceSar?: number | null;
  costSar?: number | null;
  marginPercent?: number | null;
  version: number;
  isCurrent: boolean;
  createdByUserId: string;
  createdByName?: string;
  createdAtUtc: string;
  pricingVisible?: boolean;
}

export interface BidBond {
  id: string;
  opportunityId: string;
  required: boolean;
  amountSar?: number | null;
  validUntil?: string | null;
  issuingBank?: string | null;
  status: BidBondStatus;
  requestedAtUtc?: string | null;
  issuedAtUtc?: string | null;
  rejectReason?: string | null;
}

export interface QualificationMeetingAttendee {
  id: string;
  meetingId: string;
  userId: string;
  userName?: string;
  serviceLineId?: string | null;
  serviceLineName?: string | null;
  response: AttendeeResponse;
}

export interface QualificationMeeting {
  id: string;
  opportunityId: string;
  scheduledAtUtc: string;
  location?: string | null;
  meetingLink?: string | null;
  agenda?: string | null;
  minutesOfMeeting?: string | null;
  outcome: MeetingOutcome;
  heldAtUtc?: string | null;
  attendees: QualificationMeetingAttendee[];
}

export interface SlResponse {
  id: string;
  opportunityId: string;
  serviceLineId: string;
  serviceLineNameEn?: string;
  serviceLineNameAr?: string;
  scopeItemId?: string | null;
  scopeItemTitle?: string | null;
  technicalProposalAttachmentId?: string | null;
  costingAttachmentId?: string | null;
  costSar?: number | null;
  status: SlResponseStatus;
  submittedAtUtc?: string | null;
  dueAtUtc?: string | null;
  returnReason?: string | null;
}

export interface SubmissionRecord {
  id: string;
  opportunityId: string;
  submittedAtUtc: string;
  submittedByUserId: string;
  submittedByName?: string;
  channel: SubmissionChannel;
  reference?: string | null;
}

export interface OpportunityOutcome {
  id: string;
  opportunityId: string;
  result: OutcomeResult;
  announcedAtUtc: string;
  awardedValueSar?: number | null;
  competitorName?: string | null;
  lossReason?: string | null;
  notes?: string | null;
}

export interface AssignBuilderRequest {
  builderType: BuilderType;
  builderUserId: string;
}

export interface QualificationRouteRequest {
  requiresQualificationMeeting: boolean;
}

export interface CreateMeetingRequest {
  scheduledAtUtc: string;
  location?: string | null;
  meetingLink?: string | null;
  agenda?: string | null;
  attendeeUserIds?: string[];
}

export interface ProposalTask {
  opportunityId: string;
  opportunityNumber: string;
  opportunityName: string;
  customerName: string;
  dueAtUtc?: string | null;
  builderType?: BuilderType | null;
  statusCode?: string;
  statusNameEn?: string;
}

export interface Contract {
  id: string;
  opportunityId: string;
  opportunityNumber?: string;
  opportunityName?: string;
  customerName?: string;
  contractNumber: string;
  contractStatus: ContractStatus;
  contractValueSar: number;
  startDate?: string | null;
  endDate?: string | null;
  durationMonths?: number | null;
  signedAtUtc?: string | null;
  signedByCustomerRepresentative?: string | null;
  paymentTerms?: string | null;
  notes?: string | null;
  rowVersion?: string;
}

export interface ContractNegotiationRound {
  id: string;
  contractId: string;
  roundNumber: number;
  requestedChanges: string;
  ourPosition?: string | null;
  customerPosition?: string | null;
  status: NegotiationRoundStatus;
  openedAtUtc: string;
  closedAtUtc?: string | null;
  openedByUserId: string;
  openedByName?: string;
}

export interface ContractMilestone {
  id: string;
  contractId: string;
  title: string;
  dueDate: string;
  amountSar?: number | null;
  isCompleted: boolean;
  completedAtUtc?: string | null;
}

export interface GeneratedEmail {
  id: string;
  opportunityId: string;
  templateCode: string;
  to: string;
  cc: string;
  subject: string;
  bodyHtml: string;
  status: GeneratedEmailStatus;
  generatedAtUtc: string;
  generatedByUserId: string;
  sentAtUtc?: string | null;
  failureReason?: string | null;
}

export interface EmailTemplate {
  id: string;
  code: string;
  nameEn: string;
  nameAr: string;
  subjectTemplate: string;
  bodyTemplateHtml: string;
  defaultTo: string;
  defaultCc: string;
  isActive: boolean;
}

export interface KpiDto {
  openCount: number;
  openValueSar: number;
  winRate: number;
  overdueDeadlines: number;
  avgCycleDays: number;
  wonCount?: number;
  wonValueSar?: number;
  pendingApprovals?: number;
  byStage: { stageCode: string; stageNameEn: string; stageNameAr: string; count: number; valueSar: number }[];
  byStatus?: { statusCode: string; count: number }[];
}

export interface NamedBucket {
  key: string;
  labelEn: string;
  labelAr: string;
  count: number;
  valueSar: number;
}

export interface AgingBucket {
  bucket: string;
  count: number;
  valueSar: number;
}

export interface MonthTrend {
  month: string;
  won: number;
  lost: number;
  wonValueSar: number;
  lostValueSar: number;
}

export interface DashboardChartsDto {
  byTheme: NamedBucket[];
  byServiceLine: NamedBucket[];
  topCustomers: NamedBucket[];
  aging: AgingBucket[];
  winLossTrend: MonthTrend[];
}

export interface MyWorkDto {
  pendingGates: { id: string; opportunityId: string; opportunityNumber: string; gateNameEn: string; gateNameAr: string; openedAtUtc: string }[];
  builderTasks: ProposalTask[];
  upcomingDeadlines: { opportunityId: string; opportunityNumber: string; label: string; dueAtUtc: string }[];
}

export interface PipelineDto {
  items: { stageCode: string; stageNameEn: string; stageNameAr: string; valueSar: number; count: number }[];
}

export interface FunnelReport {
  created: number;
  gw1Passed?: number;
  qualified: number;
  submitted: number;
  won: number;
  lost?: number;
}

export interface WinLossRow {
  groupKey: string;
  groupLabel: string;
  groupLabelAr?: string;
  won: number;
  lost: number;
  winRate: number;
  wonValue?: number;
  lostValue?: number;
  awardedValueSar: number;
}

export interface CycleTimeRow {
  stageOrGate: string;
  kind?: string;
  nameEn: string;
  nameAr: string;
  avgDays: number;
  medianDays?: number;
  count?: number;
}

export interface SlPerformanceRow {
  serviceLineId: string;
  nameEn: string;
  nameAr: string;
  avgTurnaroundDays: number;
  onTimePercent?: number;
  submittedCount: number;
  returnedCount: number;
  pendingCount?: number;
}

export interface DeadlineComplianceRow {
  month: string;
  met: number;
  missed: number;
  compliancePercent: number;
}

export interface ApprovalThroughputRow {
  gateCode: string;
  nameEn: string;
  nameAr: string;
  pending: number;
  decided: number;
  avgDecisionDays: number;
  maxRounds: number;
}

export interface PivotQuery {
  rows: string;
  columns?: string | null;
  measure: string;
  fromUtc?: string | null;
  toUtc?: string | null;
}

export interface PivotResult {
  rows: string;
  columns?: string | null;
  measure: string;
  columnKeys: string[];
  columnLabels: string[];
  rowsData: { rowKey: string; rowLabel: string; rowLabelAr: string; cells: { columnKey: string; value: number }[]; total: number }[];
  grandTotal: number;
}
