import {
  AttachmentCategory,
  AuditAction,
  NoteVisibility,
  OwnerEntityType
} from './enums';

export interface Note {
  id: string;
  entityType: OwnerEntityType;
  entityId: string;
  body: string;
  visibility: NoteVisibility;
  createdByUserId: string;
  createdByName?: string;
  createdByRole?: string;
  createdAtUtc: string;
  modifiedAtUtc?: string | null;
}

export interface Comment {
  id: string;
  entityType: OwnerEntityType;
  entityId: string;
  body: string;
  parentCommentId?: string | null;
  mentionedUserIds: string[];
  createdByUserId: string;
  createdByName?: string;
  createdByRole?: string;
  createdAtUtc: string;
  editedAtUtc?: string | null;
  isDeleted: boolean;
  replies?: Comment[];
}

export interface Attachment {
  id: string;
  entityType: OwnerEntityType;
  entityId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  description: string;
  category: AttachmentCategory;
  uploadedByUserId: string;
  uploadedByName?: string;
  uploadedAtUtc: string;
}

export interface AuditLogEntry {
  id: string;
  entityType: string;
  entityId: string;
  opportunityId?: string | null;
  action: AuditAction | string;
  actorUserId: string;
  actorName?: string;
  actorRoleCode: string;
  occurredAtUtc: string;
  fromValue?: string | null;
  toValue?: string | null;
  description: string;
  metadataJson?: string | null;
}

export interface AppNotification {
  id: string;
  type: string;
  title: string;
  body: string;
  messageKey?: string | null;
  paramsJson?: string | null;
  linkUrl?: string | null;
  opportunityId?: string | null;
  isRead: boolean;
  createdAtUtc: string;
  readAtUtc?: string | null;
}
