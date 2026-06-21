import { HttpErrorResponse } from '@angular/common/http';
import { Lang } from './i18n';

interface ErrCopy { network: string; rate: string; server: string; }
const ERR: Record<Lang, ErrCopy> = {
  en: {
    network: 'Network error — the server is unreachable. It may be waking up; please try again.',
    rate: 'Too many attempts. Please wait a moment and try again.',
    server: 'The server had a problem handling that. Please try again shortly.',
  },
  es: {
    network: 'Error de red — el servidor no responde. Puede estar despertando; inténtalo de nuevo.',
    rate: 'Demasiados intentos. Espera un momento y vuelve a intentar.',
    server: 'El servidor tuvo un problema al procesar eso. Inténtalo de nuevo en un momento.',
  },
};

/**
 * Turns an API error into human-readable messages. Parses RFC 7807 ProblemDetails (`errors` per
 * field + `detail`); infrastructure failures (status 0 / 5xx / 429) get an honest message — never
 * "check your input". Server-sent validation text comes through verbatim.
 */
export function parseApiError(error: unknown, fallback: string, lang: Lang = 'en'): string[] {
  const copy = ERR[lang];
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return [copy.network];
    if (error.status === 429) return [copy.rate];

    const problem = error.error;
    if (problem && typeof problem === 'object') {
      const messages: string[] = [];
      if (problem.errors && typeof problem.errors === 'object') {
        for (const field of Object.values(problem.errors as Record<string, string[]>))
          if (Array.isArray(field)) messages.push(...field);
      }
      if (messages.length === 0 && typeof problem.detail === 'string') messages.push(problem.detail);
      if (messages.length === 0 && typeof problem.title === 'string' && error.status < 500) messages.push(problem.title);
      if (messages.length > 0) return messages;
    }

    if (error.status >= 500) return [copy.server];
  }
  return [fallback];
}
