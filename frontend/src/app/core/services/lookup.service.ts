import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, of, tap } from 'rxjs';
import { LookupsAll, NamedLookup, StageLookup, StatusLookup } from '../models/lookups';
import { ApiService } from './api.service';
import { TranslateService } from '@ngx-translate/core';
import { resolveLabel } from '../utils/locale';

@Injectable({ providedIn: 'root' })
export class LookupService {
  private readonly api = inject(ApiService);
  private readonly i18n = inject(TranslateService);
  private readonly all = signal<LookupsAll | null>(null);

  readonly lookups = this.all.asReadonly();
  readonly stages = computed(() => this.all()?.stages ?? []);
  readonly statuses = computed(() => this.all()?.statuses ?? []);
  readonly serviceLines = computed(() => this.all()?.serviceLines ?? []);

  bootstrap(): Observable<LookupsAll> {
    const cached = this.all();
    if (cached) {
      return of(cached);
    }
    return this.api.get<LookupsAll>('/lookups/all').pipe(tap((data) => this.all.set(data)));
  }

  refresh(): Observable<LookupsAll> {
    return this.api.get<LookupsAll>('/lookups/all').pipe(tap((data) => this.all.set(data)));
  }

  stageName(idOrCode: string | undefined, lang: string): string {
    if (!idOrCode) {
      return '';
    }
    const item = this.stages().find((s) => s.id === idOrCode || s.code === idOrCode);
    return this.label(item, lang);
  }

  statusName(idOrCode: string | undefined, lang: string): string {
    if (!idOrCode) {
      return '';
    }
    const item = this.statuses().find((s) => s.id === idOrCode || s.code === idOrCode);
    return this.label(item, lang);
  }

  statusesForStage(stageId: string): StatusLookup[] {
    return this.statuses().filter((s) => s.stageId === stageId);
  }

  label(item: NamedLookup | StageLookup | undefined, lang: string): string {
    if (!item) {
      return '';
    }
    const prefix = this.stages().some((s) => s.id === item.id || s.code === item.code) ? 'stages' : 'statuses';
    return resolveLabel(
      { instant: (key) => this.i18n.instant(key), getCurrentLang: () => lang },
      item.code,
      item.nameEn,
      item.nameAr,
      [prefix, 'enums']
    );
  }
}
