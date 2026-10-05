import {
  BuilderType,
  EngagementType,
  OpportunityType,
  ProposalLanguage,
  SourceChannel,
  SubmissionTheme
} from './enums';
import { Customer } from './lookups';
import { UserProfile } from './user';

export interface OpportunityDeadlines {
  qualificationDeadline?: string | null;
  inquiriesDeadline?: string | null;
  estimatedCostDeadline?: string | null;
  internalDeadline?: string | null;
  submissionDeadline?: string | null;
}

export interface PendingOn {
  gateInstanceId: string;
  gateCode: string;
  gateNameEn: string;
  gateNameAr: string;
  roleCode: string;
  roleNameEn: string;
  roleNameAr: string;
  assignedUserId?: string | null;
  assignedUserName?: string | null;
  label: string;
  openedAtUtc: string;
}

export interface OpportunityListItem {
  id: string;
  opportunityNumber: string;
  name: string;
  customerId: string;
  customerName?: string;
  customerNameAr?: string;
  sourceChannel: SourceChannel;
  submissionTheme: SubmissionTheme;
  engagementType: EngagementType;
  opportunityType: OpportunityType;
  expectedValueSar: number;
  stageId: string;
  stageCode?: string;
  stageNameEn?: string;
  stageNameAr?: string;
  statusId: string;
  statusCode?: string;
  statusNameEn?: string;
  statusNameAr?: string;
  ownerUserId?: string | null;
  ownerDisplayName?: string | null;
  builderUserId?: string | null;
  builderDisplayName?: string | null;
  submittedByUserId: string;
  isClosed: boolean;
  createdAtUtc: string;
  submissionDeadline?: string | null;
  internalDeadline?: string | null;
  pendingGateCode?: string | null;
  pendingGateNameEn?: string | null;
  pendingGateNameAr?: string | null;
  pendingRoleCode?: string | null;
  pendingUserName?: string | null;
  pendingSinceUtc?: string | null;
}

export interface OpportunityDetail extends OpportunityListItem {
  sourceChannelOther?: string | null;
  relationWithClientScore: number;
  winProbabilityScore: number;
  durationMonths?: number | null;
  proposalLanguage: ProposalLanguage;
  builderType?: BuilderType | null;
  requiresQualificationMeeting?: boolean | null;
  requiresBidBond: boolean;
  isOnHold?: boolean;
  holdReason?: string | null;
  closedAtUtc?: string | null;
  rowVersion?: string;
  deadlines: OpportunityDeadlines;
  scopeBrief?: string | null;
  customer?: Customer;
  submittedBy?: UserProfile;
  owner?: UserProfile | null;
  builder?: UserProfile | null;
  pendingOn?: PendingOn | null;
}

export interface OpportunitySummary {
  id: string;
  opportunityNumber: string;
  name: string;
  customerName: string;
  customerNameAr?: string;
  stageCode: string;
  stageNameEn: string;
  stageNameAr: string;
  statusCode: string;
  statusNameEn: string;
  statusNameAr: string;
  expectedValueSar: number;
  isClosed: boolean;
}

export type JourneyStepState = 'done' | 'current' | 'pending' | 'skipped' | 'notStarted';

export interface JourneyStep {
  code: string;
  nameEn: string;
  nameAr: string;
  state: JourneyStepState;
  actorName?: string | null;
  actorRole?: string | null;
  atUtc?: string | null;
  decision?: string | null;
  reason?: string | null;
  round?: number | null;
  gateInstanceId?: string | null;
}

export interface OpportunityJourney {
  opportunityId: string;
  stageCode: string;
  statusCode: string;
  isClosed: boolean;
  isOnHold: boolean;
  steps: JourneyStep[];
}

export interface NextAction {
  code: string;
  labelKey: string;
  headlineEn: string;
  headlineAr: string;
  descriptionEn: string;
  descriptionAr: string;
  responsibleRole?: string | null;
  responsibleUserName?: string | null;
  canCurrentUserAct: boolean;
  primaryRoute?: string | null;
  gateInstanceId?: string | null;
  blockers: string[];
}

export interface OpportunityNextActions {
  opportunityId: string;
  primary?: NextAction | null;
  secondary: NextAction[];
}

export interface CreateScopeItemRequest {
  title: string;
  serviceLineId: string;
  description?: string | null;
  comment?: string | null;
}

export interface CreateOpportunityRequest {
  name: string;
  customerId: string;
  sourceChannel: SourceChannel;
  sourceChannelOther?: string | null;
  submissionTheme: SubmissionTheme;
  engagementType: EngagementType;
  opportunityType: OpportunityType;
  expectedValueSar: number;
  relationWithClientScore: number;
  winProbabilityScore: number;
  durationMonths?: number | null;
  proposalLanguage: ProposalLanguage;
  requiresBidBond: boolean;
  scopeBrief?: string | null;
  scopeItems?: CreateScopeItemRequest[] | null;
}

export interface UpdateOpportunityRequest extends CreateOpportunityRequest {
  rowVersion?: string;
}

export interface StatusTransitionDto {
  toStatusId: string;
  toStatusCode: string;
  toStatusNameEn: string;
  toStatusNameAr: string;
  stageId: string;
  stageCode: string;
  isTerminal: boolean;
  requiresReason: boolean;
  requiredRoleCode: string;
}

export interface OpportunityListQuery {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  search?: string;
  filter?: string;
  stageId?: string;
  statusId?: string;
  customerId?: string;
  ownerUserId?: string;
  builderUserId?: string;
  submissionTheme?: SubmissionTheme | '';
  sourceChannel?: SourceChannel | '';
  valueFrom?: number | null;
  valueTo?: number | null;
  submissionDateFrom?: string;
  submissionDateTo?: string;
  createdFrom?: string;
  createdTo?: string;
  isClosed?: boolean | null;
  myItemsOnly?: boolean;
  pendingOnMe?: boolean;
}

export interface ScopeItem {
  id: string;
  scopeOfWorkId: string;
  title: string;
  description?: string | null;
  serviceLineId: string;
  serviceLineNameEn?: string;
  serviceLineNameAr?: string;
  assignedUserId?: string | null;
  assignedUserName?: string | null;
  comment?: string | null;
  sortOrder: number;
}

export interface ScopeOfWork {
  id: string;
  opportunityId: string;
  brief: string;
  items: ScopeItem[];
}

export interface UpsertScopeItemRequest {
  title: string;
  description?: string | null;
  serviceLineId: string;
  assignedUserId?: string | null;
  comment?: string | null;
  sortOrder?: number;
}
