import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { ALLOWED_UPLOAD_EXTENSIONS, MAX_UPLOAD_BYTES } from '../models/enums';
import { fileExtension } from './file-size';

export function requiredIf(predicate: () => boolean): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!predicate()) {
      return null;
    }
    const value = control.value as unknown;
    if (value === null || value === undefined || value === '') {
      return { required: true };
    }
    return null;
  };
}

export function scoreRange(min = 1, max = 5): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const n = Number(control.value);
    if (control.value === null || control.value === '') {
      return { required: true };
    }
    if (!Number.isFinite(n) || n < min || n > max) {
      return { scoreRange: { min, max } };
    }
    return null;
  };
}

export function positiveMoney(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const n = Number(control.value);
    if (control.value === null || control.value === '') {
      return { required: true };
    }
    if (!Number.isFinite(n) || n <= 0) {
      return { positiveMoney: true };
    }
    return null;
  };
}

export function deadlineOrder(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const keys = [
      'qualificationDeadline',
      'inquiriesDeadline',
      'estimatedCostDeadline',
      'internalDeadline',
      'submissionDeadline'
    ];
    const dates = keys
      .map((k) => group.get(k)?.value as string | null)
      .filter((v): v is string => !!v)
      .map((v) => new Date(v).getTime());
    for (let i = 1; i < dates.length; i++) {
      if (dates[i] < dates[i - 1]) {
        return { deadlineOrder: true };
      }
    }
    return null;
  };
}

export function allowedFile(file: File | null): ValidationErrors | null {
  if (!file) {
    return { required: true };
  }
  if (file.size > MAX_UPLOAD_BYTES) {
    return { maxSize: { max: MAX_UPLOAD_BYTES } };
  }
  const ext = fileExtension(file.name);
  if (!(ALLOWED_UPLOAD_EXTENSIONS as readonly string[]).includes(ext)) {
    return { fileType: { allowed: ALLOWED_UPLOAD_EXTENSIONS } };
  }
  return null;
}

export function matchFields(source: string, confirm: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const a = group.get(source)?.value as string;
    const b = group.get(confirm)?.value as string;
    if (!a || !b || a === b) {
      return null;
    }
    return { mismatch: true };
  };
}
