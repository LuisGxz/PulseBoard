import { Injectable, computed, signal } from '@angular/core';
import { AGG_LABEL, COPY, Lang, ROLE_LABEL, STATUS_LABEL, TYPE_LABEL, WIDGET_LABEL } from './i18n';

const LANG_KEY = 'pulseboard.lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  readonly lang = signal<Lang>(readStoredLang());

  /** App-wide copy for the active language. */
  readonly t = computed(() => COPY[this.lang()]);
  readonly isEs = computed(() => this.lang() === 'es');
  readonly dateLocale = computed(() => (this.lang() === 'es' ? 'es' : 'en-US'));

  set(lang: Lang): void {
    this.lang.set(lang);
    try { localStorage.setItem(LANG_KEY, lang); } catch { /* ignore */ }
    document.documentElement.lang = lang;
  }

  toggle(): void { this.set(this.lang() === 'es' ? 'en' : 'es'); }

  role(value: string): string { return ROLE_LABEL[this.lang()][value] ?? value; }
  widget(value: string): string { return WIDGET_LABEL[this.lang()][value] ?? value; }
  agg(value: string): string { return AGG_LABEL[this.lang()][value] ?? value; }
  type(value: string): string { return TYPE_LABEL[this.lang()][value] ?? value; }
  status(value: string): string { return STATUS_LABEL[this.lang()][value] ?? value; }
}

function readStoredLang(): Lang {
  try {
    const stored = localStorage.getItem(LANG_KEY);
    if (stored === 'en' || stored === 'es') return stored;
  } catch { /* ignore */ }
  return navigator.language?.toLowerCase().startsWith('es') ? 'es' : 'en';
}
