import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, OnInit, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { Customer } from '../../../core/models/lookups';
import { CustomersService } from '../../../core/services/customers.service';
import { MATERIAL_IMPORTS } from '../../material';
import { CustomerQuickCreateDialogComponent } from './customer-quick-create.dialog';

const CREATE_TOKEN = '__crm_create__';

@Component({
  selector: 'crm-customer-picker',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-form-field appearance="outline" class="full">
      <mat-label>{{ 'opp.customer' | translate }}</mat-label>
      <input
        matInput
        [formControl]="search"
        [matAutocomplete]="auto"
        [placeholder]="'customer.searchPlaceholder' | translate"
      />
      <mat-autocomplete #auto="matAutocomplete" [displayWith]="displayFn" (optionSelected)="onSelected($event.option.value)">
        @for (c of options; track c.id) {
          <mat-option [value]="c">
            <span class="opt">{{ c.nameEn }} <span class="muted">/ {{ c.nameAr }}</span></span>
            <small class="sector">{{ c.sector }}</small>
          </mat-option>
        }
        @if (showCreate) {
          <mat-option [value]="createToken">
            <mat-icon>add</mat-icon>
            {{ 'customer.createNamed' | translate: { name: search.value } }}
          </mat-option>
        }
      </mat-autocomplete>
      @if (control().invalid && control().touched) {
        <mat-error>{{ 'common.required' | translate }}</mat-error>
      }
    </mat-form-field>
  `,
  styles: `
    .full { width: 100%; }
    .opt { display: block; }
    .muted { color: var(--crm-muted); }
    .sector { display: block; color: var(--crm-muted); font-size: 0.75rem; }
    mat-option mat-icon { vertical-align: middle; margin-inline-end: 0.35rem; }
  `
})
export class CustomerPickerComponent implements OnInit {
  private readonly api = inject(CustomersService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly control = input.required<FormControl<string>>();
  readonly changed = output<Customer | null>();

  readonly search = new FormControl('', { nonNullable: true });
  readonly createToken = CREATE_TOKEN;
  options: Customer[] = [];
  showCreate = false;
  private selected: Customer | null = null;

  ngOnInit(): void {
    this.search.valueChanges
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((q) => {
          const term = typeof q === 'string' ? q.trim() : '';
          this.showCreate = term.length >= 2;
          if (!term) {
            this.options = [];
            return of({ items: [] as Customer[], totalCount: 0, page: 1, pageSize: 20 });
          }
          return this.api.list({ page: 1, pageSize: 20, search: term });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((r) => {
        this.options = r.items ?? [];
      });

    // Hydrate display if control already has an id
    const id = this.control().value;
    if (id) {
      this.api.get(id).subscribe({
        next: (c) => {
          this.selected = c;
          this.search.setValue(this.displayFn(c), { emitEvent: false });
        },
        error: () => undefined
      });
    }
  }

  displayFn = (value: Customer | string | null): string => {
    if (!value) return '';
    if (typeof value === 'string') return this.selected ? `${this.selected.nameEn} / ${this.selected.nameAr}` : value;
    return `${value.nameEn} / ${value.nameAr}`;
  };

  onSelected(value: Customer | string): void {
    if (value === CREATE_TOKEN) {
      const preset = typeof this.search.value === 'string' ? this.search.value : '';
      this.dialog
        .open(CustomerQuickCreateDialogComponent, { data: { presetName: preset }, width: '480px' })
        .afterClosed()
        .subscribe((created: Customer | null) => {
          if (!created) return;
          this.applyCustomer(created);
        });
      return;
    }
    if (value && typeof value === 'object') this.applyCustomer(value);
  }

  private applyCustomer(c: Customer): void {
    this.selected = c;
    this.control().setValue(c.id);
    this.control().markAsDirty();
    this.search.setValue(this.displayFn(c), { emitEvent: false });
    this.changed.emit(c);
  }
}
