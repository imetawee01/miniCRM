export type RoleCode =
  | 'AM'
  | 'BIDS_PRESALES'
  | 'BIDS_MGMT'
  | 'PRESALES'
  | 'SL'
  | 'MGMT'
  | 'ADMIN';

export const ROLE_CODES: RoleCode[] = [
  'AM',
  'BIDS_PRESALES',
  'BIDS_MGMT',
  'PRESALES',
  'SL',
  'MGMT',
  'ADMIN'
];

export type SourceChannel =
  | 'DirectClient'
  | 'Etimad'
  | 'Partner'
  | 'Referral'
  | 'AccountExpansion'
  | 'Other';

export type SubmissionTheme = 'Emdad' | 'Ahad' | 'ThiqahBusinessSolutions' | 'Elm';
export type EngagementType = 'Proactive' | 'Reactive';
export type OpportunityType = 'New' | 'Renewal';
export type ProposalLanguage = 'Arabic' | 'English' | 'Bilingual';
export type BuilderType = 'Presales' | 'ServiceLine';

export type GateState = 'Pending' | 'Approved' | 'Rejected' | 'Returned' | 'Skipped';
export type OwnerEntityType = 'Opportunity' | 'GateInstance' | 'ScopeItem' | 'Contract';
export type CollaborationPath = 'opportunities' | 'gates' | 'scope-items' | 'contracts';
export type NoteVisibility = 'Internal' | 'Shared';
export type AttachmentCategory =
  | 'RFP'
  | 'TechnicalProposal'
  | 'Costing'
  | 'BidBond'
  | 'Contract'
  | 'Other';

export type BidBondStatus = 'NotRequired' | 'Requested' | 'Issued' | 'Rejected';
export type MeetingOutcome = 'Pending' | 'Passed' | 'NotPassed';
export type AttendeeResponse = 'Invited' | 'Accepted' | 'Declined' | 'Attended';
export type SlResponseStatus = 'Pending' | 'Submitted' | 'Returned' | 'Accepted';
export type SubmissionChannel = 'Etimad' | 'Email' | 'HandDelivery' | 'Portal' | 'Other';
export type OutcomeResult = 'Won' | 'Lost';
export type ContractStatus = 'Won' | 'ContractNegotiation' | 'ContractSigned';
export type NegotiationRoundStatus = 'Open' | 'Agreed' | 'Rejected';
export type GeneratedEmailStatus = 'Draft' | 'Queued' | 'Sent' | 'Failed';
export type NotificationType = 'Info' | 'GateAssigned' | 'EmailGenerated' | 'Mention' | 'Deadline';

export type AuditAction =
  | 'OpportunityCreated'
  | 'OpportunityUpdated'
  | 'StatusChanged'
  | 'StageChanged'
  | 'GateOpened'
  | 'GateApproved'
  | 'GateRejected'
  | 'GateReturned'
  | 'NoteAdded'
  | 'NoteEdited'
  | 'CommentAdded'
  | 'CommentEdited'
  | 'CommentDeleted'
  | 'AttachmentUploaded'
  | 'AttachmentDeleted'
  | 'AttachmentDownloaded'
  | 'BuilderAssigned'
  | 'DeadlinesSet'
  | 'EstimatedCostSubmitted'
  | 'BidBondRequested'
  | 'BidBondIssued'
  | 'ProposalSubmittedForReview'
  | 'ProposalReturned'
  | 'ApprovalRequested'
  | 'Submitted'
  | 'OutcomeRecorded'
  | 'ContractStageChanged'
  | 'OpportunityHeld'
  | 'OpportunityResumed'
  | 'OpportunityCanceled'
  | 'EmailGenerated'
  | 'EmailSent'
  | 'UserLoggedIn';

export const SOURCE_CHANNELS: SourceChannel[] = [
  'DirectClient',
  'Etimad',
  'Partner',
  'Referral',
  'AccountExpansion',
  'Other'
];

export const SUBMISSION_THEMES: SubmissionTheme[] = [
  'Emdad',
  'Ahad',
  'ThiqahBusinessSolutions',
  'Elm'
];

export const ENGAGEMENT_TYPES: EngagementType[] = ['Proactive', 'Reactive'];
export const OPPORTUNITY_TYPES: OpportunityType[] = ['New', 'Renewal'];
export const PROPOSAL_LANGUAGES: ProposalLanguage[] = ['Arabic', 'English', 'Bilingual'];
export const BUILDER_TYPES: BuilderType[] = ['Presales', 'ServiceLine'];
export const ATTACHMENT_CATEGORIES: AttachmentCategory[] = [
  'RFP',
  'TechnicalProposal',
  'Costing',
  'BidBond',
  'Contract',
  'Other'
];
export const SUBMISSION_CHANNELS: SubmissionChannel[] = [
  'Etimad',
  'Email',
  'HandDelivery',
  'Portal',
  'Other'
];
export const NOTE_VISIBILITIES: NoteVisibility[] = ['Internal', 'Shared'];

export const ALLOWED_UPLOAD_EXTENSIONS = [
  '.pdf',
  '.docx',
  '.xlsx',
  '.pptx',
  '.vsdx',
  '.png',
  '.jpg',
  '.jpeg',
  '.zip',
  '.msg'
] as const;

export const MAX_UPLOAD_BYTES = 25 * 1024 * 1024;
