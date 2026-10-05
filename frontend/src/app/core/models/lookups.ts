export interface NamedLookup {
  id: string;
  code: string;
  nameEn: string;
  nameAr: string;
  sortOrder?: number;
  isActive?: boolean;
}

export interface StatusLookup extends NamedLookup {
  stageId: string;
  isTerminal: boolean;
}

export interface StageLookup extends NamedLookup {
  statuses?: StatusLookup[];
}

export interface ServiceLineLookup extends NamedLookup {
  leadUserId?: string | null;
  isActive?: boolean;
  sortOrder?: number;
  colourHex?: string | null;
}

export interface EnumLookup {
  value: string;
  nameEn: string;
  nameAr: string;
}

export interface LookupsAll {
  stages: StageLookup[];
  statuses: StatusLookup[];
  sourceChannels: EnumLookup[];
  submissionThemes: EnumLookup[];
  proposalLanguages: EnumLookup[];
  attachmentCategories: EnumLookup[];
  serviceLines: ServiceLineLookup[];
  engagementTypes?: EnumLookup[];
  opportunityTypes?: EnumLookup[];
}

export interface CustomerContact {
  id: string;
  customerId: string;
  name: string;
  email?: string | null;
  phone?: string | null;
  title?: string | null;
  isPrimary: boolean;
}

export interface Customer {
  id: string;
  nameEn: string;
  nameAr: string;
  sector: string;
  isGovernment: boolean;
  website?: string | null;
  contacts?: CustomerContact[];
}

export interface UpsertCustomerRequest {
  nameEn: string;
  nameAr: string;
  sector: string;
  isGovernment: boolean;
  website?: string | null;
  isQuickCreated?: boolean;
  primaryContact?: {
    name: string;
    email?: string | null;
    phone?: string | null;
    title?: string | null;
  } | null;
}
