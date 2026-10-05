import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateMeetingRequest,
  QualificationMeeting,
  QualificationRouteRequest
} from '../models/contract';
import { OpportunityListItem } from '../models/opportunity';
import { PagedQuery, PagedResult } from '../models/paged-result';
import { AttendeeResponse } from '../models/enums';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class QualificationService {
  private readonly api = inject(ApiService);

  queue(query?: PagedQuery): Observable<PagedResult<OpportunityListItem>> {
    return this.api.get<PagedResult<OpportunityListItem>>('/opportunities', {
      ...query,
      myItemsOnly: true
    });
  }

  setRoute(opportunityId: string, body: QualificationRouteRequest): Observable<void> {
    return this.api.post<void>(`/opportunities/${opportunityId}/qualification/route`, body);
  }

  createMeeting(opportunityId: string, body: CreateMeetingRequest): Observable<QualificationMeeting> {
    return this.api.post<QualificationMeeting>(`/opportunities/${opportunityId}/qualification/meeting`, body);
  }

  getMeeting(opportunityId: string): Observable<QualificationMeeting> {
    return this.api.get<QualificationMeeting>(`/opportunities/${opportunityId}/qualification/meeting`);
  }

  updateMeeting(opportunityId: string, body: Partial<CreateMeetingRequest>): Observable<QualificationMeeting> {
    return this.api.put<QualificationMeeting>(`/opportunities/${opportunityId}/qualification/meeting`, body);
  }

  saveMinutes(opportunityId: string, minutesOfMeeting: string): Observable<void> {
    return this.api.post<void>(`/opportunities/${opportunityId}/qualification/meeting/minutes`, {
      minutesOfMeeting
    });
  }

  addAttendees(opportunityId: string, userIds: string[]): Observable<void> {
    return this.api.post<void>(`/opportunities/${opportunityId}/qualification/meeting/attendees`, {
      userIds
    });
  }

  attendeeResponse(attendeeId: string, response: AttendeeResponse): Observable<void> {
    return this.api.put<void>(`/qualification/meeting/attendees/${attendeeId}/response`, { response });
  }
}
