import { Injectable, computed, signal } from '@angular/core';

export interface TourStep {
  selector: string;     // CSS selector of the element to highlight
  title: string;
  body: string;
}

const SEEN_KEY = 'pulseboard.tourSeen';

/** Minimal coachmark tour: a list of steps, each anchored to an element by CSS selector. */
@Injectable({ providedIn: 'root' })
export class TourService {
  private readonly steps = signal<TourStep[]>([]);
  readonly index = signal(0);
  readonly active = signal(false);

  readonly step = computed(() => this.steps()[this.index()] ?? null);
  readonly count = computed(() => this.steps().length);

  start(steps: TourStep[]): void {
    if (steps.length === 0) return;
    this.steps.set(steps);
    this.index.set(0);
    this.active.set(true);
  }

  /** Starts the tour only once ever (per browser). Returns true if it started. */
  startOnce(steps: TourStep[]): boolean {
    try { if (localStorage.getItem(SEEN_KEY)) return false; } catch { /* ignore */ }
    this.markSeen();
    this.start(steps);
    return true;
  }

  next(): void {
    if (this.index() + 1 >= this.count()) this.stop();
    else this.index.update(i => i + 1);
  }
  prev(): void { this.index.update(i => Math.max(0, i - 1)); }
  stop(): void { this.active.set(false); }

  private markSeen(): void { try { localStorage.setItem(SEEN_KEY, '1'); } catch { /* ignore */ } }
}
