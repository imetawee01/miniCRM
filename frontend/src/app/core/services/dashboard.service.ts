import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { DashboardChartsDto, KpiDto, MyWorkDto, PipelineDto } from '../models/contract';
import { ApiService } from './api.service';

export interface DashboardRange {
  fromUtc?: string | null;
  toUtc?: string | null;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly api = inject(ApiService);

  kpis(range?: DashboardRange): Observable<KpiDto> {
    return this.api.get<KpiDto>('/dashboard/kpis', range);
  }

  myWork(): Observable<MyWorkDto> {
    return this.api.get<MyWorkDto>('/dashboard/my-work');
  }

  pipeline(): Observable<PipelineDto> {
    return this.api.get<PipelineDto>('/dashboard/pipeline');
  }

  charts(range?: DashboardRange): Observable<DashboardChartsDto> {
    return this.api.get<DashboardChartsDto>('/dashboard/charts', range);
  }
}
