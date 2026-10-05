import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { GeneratedEmail } from '../../../../core/models/contract';
import { EmailsService } from '../../../../core/services/emails.service';
import { ToastService } from '../../../../core/services/toast.service';
import { copyText, htmlToPlain, triggerDownload } from '../../../../core/utils/browser';
import { MATERIAL_IMPORTS } from '../../../../shared/material';
import { LocalDateTimePipe } from '../../../../shared/pipes/local-date-time.pipe';
import { EnumLabelPipe } from '../../../../shared/pipes/enum-label.pipe';

@Component({
  selector: 'crm-emails-tab',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, LocalDateTimePipe, EnumLabelPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-stack">
      @for (email of emails(); track email.id) {
        <article class="crm-card crm-stack">
          <h3>{{ email.subject }}</h3>
          <p class="crm-muted">{{ email.to }} · {{ email.status | enumLabel }} · {{ email.generatedAtUtc | localDateTime }}</p>
          <div class="body" [innerHTML]="email.bodyHtml"></div>
          <div class="crm-actions">
            <button mat-stroked-button type="button" (click)="copy(email.subject, 'emails.copiedSubject')">{{ 'emails.copySubject' | translate }}</button>
            <button mat-stroked-button type="button" (click)="copy(email.bodyHtml, 'emails.copiedHtml')">{{ 'emails.copyHtml' | translate }}</button>
            <button mat-stroked-button type="button" (click)="copy(plain(email.bodyHtml), 'emails.copiedPlain')">{{ 'emails.copyPlain' | translate }}</button>
            <button mat-stroked-button type="button" (click)="eml(email)">{{ 'emails.downloadEml' | translate }}</button>
            <button mat-flat-button color="primary" type="button" (click)="markSent(email)">{{ 'emails.markSent' | translate }}</button>
          </div>
        </article>
      }
    </div>
  `,
  styles: `.body { border: 1px solid var(--crm-border); padding: 0.75rem; border-radius: 8px; overflow: auto; }`
})
export class EmailsTabComponent implements OnInit {
  private readonly api = inject(EmailsService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly emails = signal<GeneratedEmail[]>([]);

  ngOnInit(): void {
    const id = this.route.parent?.snapshot.paramMap.get('id');
    if (id) this.api.forOpportunity(id).subscribe((rows) => this.emails.set(rows));
  }

  plain(html: string): string {
    return htmlToPlain(html);
  }

  async copy(text: string, key: string): Promise<void> {
    await copyText(text);
    this.toast.success(key);
  }

  eml(email: GeneratedEmail): void {
    this.api.downloadEml(email.id).subscribe((blob) => triggerDownload(blob, `${email.templateCode}.eml`));
  }

  markSent(email: GeneratedEmail): void {
    this.api.markSent(email.id).subscribe(() => this.toast.success('emails.markedSent'));
  }
}
