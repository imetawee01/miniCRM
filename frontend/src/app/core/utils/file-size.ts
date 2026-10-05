export function formatFileSize(bytes: number, locale = 'en'): string {
  if (bytes < 1024) {
    return `${bytes} ${locale.startsWith('ar') ? 'بايت' : 'B'}`;
  }
  const kb = bytes / 1024;
  if (kb < 1024) {
    return `${kb.toFixed(1)} KB`;
  }
  const mb = kb / 1024;
  return `${mb.toFixed(1)} MB`;
}

export function fileExtension(fileName: string): string {
  const idx = fileName.lastIndexOf('.');
  return idx >= 0 ? fileName.slice(idx).toLowerCase() : '';
}
