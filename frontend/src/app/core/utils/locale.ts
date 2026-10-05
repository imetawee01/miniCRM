export type AppLang = 'en' | 'ar';

export const LANG_STORAGE_KEY = 'crm.lang';

export function isArabic(lang?: string | null): boolean {
  return (lang ?? '').toLowerCase().startsWith('ar');
}

export function directionFor(lang?: string | null): 'ltr' | 'rtl' {
  return isArabic(lang) ? 'rtl' : 'ltr';
}

export function pickLocalized(
  lang: string | null | undefined,
  en?: string | null,
  ar?: string | null,
  fallback = ''
): string {
  const value = isArabic(lang) ? ar || en : en || ar;
  return (value || fallback).trim();
}

export function applyDocumentLanguage(lang: AppLang): 'ltr' | 'rtl' {
  const dir = directionFor(lang);
  document.documentElement.lang = lang;
  document.documentElement.dir = dir;
  return dir;
}

/** SUBMISSION / (app.scope) / response_development → readable words */
export function humanizeCode(value: string): string {
  const leaf = value.trim().replace(/^[(\[]|[)\]]$/g, '').split('.').pop() ?? value;
  return leaf
    .split(/[._\s-]+/)
    .filter(Boolean)
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ');
}

function isRawCode(value: string, code?: string | null): boolean {
  const normalized = value.trim().replace(/[()[\]]/g, '');
  if (!normalized) {
    return true;
  }
  if (code && normalized.toLowerCase() === code.replace(/[()[\]]/g, '').toLowerCase()) {
    return true;
  }
  return /^[A-Z][A-Z0-9_]*$/.test(normalized);
}

export function translateCode(
  i18n: { instant: (key: string) => unknown },
  code: string | null | undefined,
  prefixes: string[] = ['stages', 'statuses', 'enums']
): string {
  if (!code) {
    return '';
  }
  const clean = code.trim().replace(/[()[\]]/g, '');
  for (const prefix of prefixes) {
    const key = `${prefix}.${clean}`;
    const translated = i18n.instant(key);
    if (typeof translated === 'string' && translated && translated !== key) {
      return translated;
    }
  }
  return humanizeCode(clean);
}

export function resolveLabel(
  i18n: { instant: (key: string) => unknown; getCurrentLang: () => string | null },
  code: string | null | undefined,
  en?: string | null,
  ar?: string | null,
  prefixes: string[] = ['stages', 'statuses', 'enums']
): string {
  const localized = pickLocalized(i18n.getCurrentLang(), en, ar);
  if (localized && !isRawCode(localized, code)) {
    return localized;
  }
  return translateCode(i18n, code || localized, prefixes);
}
