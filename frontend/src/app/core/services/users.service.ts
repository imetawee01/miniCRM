import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { RoleCode } from '../models/enums';
import { RoleDto, UpsertUserRequest, UserListItem, UserPickItem } from '../models/user';
import { PagedQuery, PagedResult } from '../models/paged-result';
import { ServiceLineLookup } from '../models/lookups';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly api = inject(ApiService);

  list(query?: PagedQuery): Observable<PagedResult<UserListItem>> {
    return this.api.get<PagedResult<UserListItem>>('/users', query);
  }

  /** Non-admin picker list. Pass role codes as comma/pipe-separated string. */
  pickable(role?: string, search?: string): Observable<UserPickItem[]> {
    return this.api.get<UserPickItem[]>('/users/pickable', { role, search });
  }

  create(body: UpsertUserRequest): Observable<UserListItem> {
    return this.api.post<UserListItem>('/users', body);
  }

  update(id: string, body: UpsertUserRequest): Observable<UserListItem> {
    return this.api.put<UserListItem>(`/users/${id}`, body);
  }

  setRoles(id: string, roleCodes: RoleCode[]): Observable<void> {
    return this.api.put<void>(`/users/${id}/roles`, { roleCodes });
  }

  activate(id: string): Observable<void> {
    return this.api.post<void>(`/users/${id}/activate`);
  }

  deactivate(id: string): Observable<void> {
    return this.api.post<void>(`/users/${id}/deactivate`);
  }

  roles(): Observable<RoleDto[]> {
    return this.api.get<RoleDto[]>('/roles');
  }

  serviceLines(includeInactive = false): Observable<ServiceLineLookup[]> {
    return this.api.get<ServiceLineLookup[]>('/service-lines', { includeInactive });
  }

  createServiceLine(body: Partial<ServiceLineLookup> & { code: string; nameEn: string; nameAr: string }): Observable<ServiceLineLookup> {
    return this.api.post<ServiceLineLookup>('/service-lines', body);
  }

  updateServiceLine(
    id: string,
    body: Partial<ServiceLineLookup> & { code: string; nameEn: string; nameAr: string; isActive: boolean }
  ): Observable<void> {
    return this.api.put<void>(`/service-lines/${id}`, body);
  }

  deactivateServiceLine(id: string): Observable<void> {
    return this.api.post<void>(`/service-lines/${id}/deactivate`);
  }

  permissionMatrix(): Observable<{
    policies: string[];
    roles: string[];
    cells: { policyName: string; roleCode: string; isAllowed: boolean }[];
  }> {
    return this.api.get('/admin/permissions');
  }

  savePermissionMatrix(
    cells: { policyName: string; roleCode: string; isAllowed: boolean }[]
  ): Observable<void> {
    return this.api.put<void>('/admin/permissions', { cells });
  }
}
