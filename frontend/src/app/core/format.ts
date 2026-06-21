import { Aggregation } from './models';

/** Compact human number: 1.2K, 3.4M, 5.6B. Keeps small values readable. */
export function compact(value: number, locale = 'en-US'): string {
  const abs = Math.abs(value);
  if (abs >= 1_000_000_000) return trim(value / 1_000_000_000, locale) + 'B';
  if (abs >= 1_000_000) return trim(value / 1_000_000, locale) + 'M';
  if (abs >= 1_000) return trim(value / 1_000, locale) + 'K';
  return value.toLocaleString(locale, { maximumFractionDigits: 2 });
}

function trim(value: number, locale: string): string {
  return value.toLocaleString(locale, { maximumFractionDigits: 1 });
}

/**
 * Formats a KPI scalar. An average of a 0..1 column reads as a percentage (e.g. boolean conversion),
 * otherwise it's a compact number.
 */
export function formatKpi(value: number | null | undefined, aggregation: Aggregation, locale = 'en-US'): string {
  if (value == null) return '—';
  if (aggregation === 'Avg' && Math.abs(value) <= 1) return `${(value * 100).toFixed(1)}%`;
  return compact(value, locale);
}
