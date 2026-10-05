import { RoleCode } from './enums';

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  jobTitle?: string | null;
  phone?: string | null;
  isActive: boolean;
  roles: RoleCode[];
  permissions?: string[];
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: UserProfile;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface SsoConfig {
  ssoEnabled: boolean;
}

export interface UserListItem {
  id: string;
  email: string;
  displayName: string;
  jobTitle?: string | null;
  phone?: string | null;
  isActive: boolean;
  roles: RoleCode[];
  createdAtUtc?: string;
}

/** Lightweight user for pickers (GET /users/pickable). */
export interface UserPickItem {
  id: string;
  displayName: string;
  email: string;
  roles: RoleCode[];
}

export interface UpsertUserRequest {
  email: string;
  displayName: string;
  jobTitle?: string | null;
  phone?: string | null;
  password?: string;
  roleCodes: RoleCode[];
  isActive?: boolean;
}

export interface RoleDto {
  id: string;
  code: RoleCode;
  nameEn: string;
  nameAr: string;
  description?: string | null;
}
