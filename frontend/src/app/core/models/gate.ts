import { GateState } from './enums';
import { OpportunitySummary } from './opportunity';

export interface WorkflowGate {
  id: string;
  code: string;
  nameEn: string;
  nameAr: string;
  sortOrder: number;
  responsibleRoleCode: string;
  allowedDecisions: string;
  requiresReasonOnReject: boolean;
  requiresAttachmentOnApprove: boolean;
  isActive: boolean;
}

export interface GateInstance {
  id: string;
  opportunityId: string;
  opportunityNumber?: string;
  opportunityName?: string;
  customerName?: string;
  gateId: string;
  gate?: WorkflowGate;
  gateCode?: string;
  gateNameEn?: string;
  gateNameAr?: string;
  allowedDecisions?: string;
  state: GateState;
  assignedRoleCode: string;
  assignedUserId?: string | null;
  assignedUserName?: string | null;
  openedAtUtc: string;
  decidedAtUtc?: string | null;
  decidedByUserId?: string | null;
  decidedByName?: string | null;
  decision?: string | null;
  reason?: string | null;
  round: number;
  requiresReasonOnReject?: boolean;
  requiresAttachmentOnApprove?: boolean;
  opportunity?: OpportunitySummary;
}

export interface GateDecisionRequest {
  decision: string;
  reason?: string | null;
  noteBody?: string | null;
  attachmentIds?: string[];
}

export interface PendingApproval extends GateInstance {
  opportunityNumber: string;
  opportunityName: string;
  customerName: string;
  slaHours?: number | null;
}

export interface StatusTransitionConfig {
  id: string;
  fromStatusId: string;
  fromStatusCode?: string;
  toStatusId: string;
  toStatusCode?: string;
  requiredRoleCode: string;
  requiresReason: boolean;
}
