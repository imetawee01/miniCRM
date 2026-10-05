const MONTHS_EN = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const MONTHS_AR = ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];

function pad(value: number): string {
  return value.toString().padStart(2, '0');
}

export function parseUtc(value: string | Date | null | undefined): Date | null {
  if (!value) {
    return null;
  }
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value;
  }
  const raw = value.endsWith('Z') || value.includes('+') ? value : `${value}Z`;
  const date = new Date(raw);
  return Number.isNaN(date.getTime()) ? null : date;
}

export function formatLocalDateTime(value: string | Date | null | undefined, locale = 'en'): string {
  const date = parseUtc(value);
  if (!date) {
    return '';
  }
  const months = locale.startsWith('ar') ? MONTHS_AR : MONTHS_EN;
  return `${pad(date.getDate())} ${months[date.getMonth()]} ${date.getFullYear()}, ${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function formatUtcTooltip(value: string | Date | null | undefined): string {
  const date = parseUtc(value);
  if (!date) {
    return '';
  }
  return `${pad(date.getUTCDate())} ${MONTHS_EN[date.getUTCMonth()]} ${date.getUTCFullYear()}, ${pad(date.getUTCHours())}:${pad(date.getUTCMinutes())} UTC`;
}

export function timeAgo(value: string | Date | null | undefined, locale = 'en'): string {
  const date = parseUtc(value);
  if (!date) {
    return '';
  }
  const diffMs = Date.now() - date.getTime();
  const abs = Math.abs(diffMs);
  const minutes = Math.round(abs / 60000);
  const hours = Math.round(abs / 3600000);
  const days = Math.round(abs / 86400000);
  const isAr = locale.startsWith('ar');
  const future = diffMs < 0;
  const phrase = (n: number, enUnit: string, arUnit: string): string => {
    if (isAr) {
      return future ? `خلال ${n} ${arUnit}` : `منذ ${n} ${arUnit}`;
    }
    const unit = n === 1 ? enUnit.replace(/s$/, '') : enUnit;
    return future ? `in ${n} ${unit}` : `${n} ${unit} ago`;
  };
  if (minutes < 1) {
    return isAr ? 'الآن' : 'just now';
  }
  if (minutes < 60) {
    return phrase(minutes, 'minutes', 'دقيقة');
  }
  if (hours < 24) {
    return phrase(hours, 'hours', 'ساعة');
  }
  return phrase(days, 'days', 'يوم');
}

export function daysUntil(value: string | Date | null | undefined): number | null {
  const date = parseUtc(value);
  if (!date) {
    return null;
  }
  return Math.ceil((date.getTime() - Date.now()) / 86400000);
}

export function deadlineTone(value: string | Date | null | undefined): 'none' | 'ok' | 'soon' | 'overdue' {
  const days = daysUntil(value);
  if (days === null) {
    return 'none';
  }
  if (days < 0) {
    return 'overdue';
  }
  if (days <= 3) {
    return 'soon';
  }
  return 'ok';
}

export function toIsoUtc(local: Date | string | null | undefined): string | null {
  if (!local) {
    return null;
  }
  const date = local instanceof Date ? local : new Date(local);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}
