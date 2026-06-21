import { ChangeDetectionStrategy, Component, HostListener, computed, effect, inject, signal } from '@angular/core';
import { LanguageService } from '../core/language.service';
import { TourService } from '../core/tour.service';

interface Box { top: number; left: number; width: number; height: number; }

/** Renders the active tour step: a dimmed overlay with a highlight ring and an anchored tooltip. */
@Component({
  selector: 'pb-tour',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (tour.active() && box(); as b) {
      <div class="fixed inset-0 z-[60]" (click)="tour.next()">
        <!-- highlight ring -->
        <div class="absolute rounded-xl ring-2 ring-cy-400 pointer-events-none transition-all duration-200"
             style="box-shadow: 0 0 0 9999px rgba(7,9,12,.72);"
             [style.top.px]="b.top - 6" [style.left.px]="b.left - 6"
             [style.width.px]="b.width + 12" [style.height.px]="b.height + 12"></div>

        <!-- tooltip -->
        <div class="absolute w-72 widget !bg-gr-850 p-4 transition-all duration-200"
             [style.top.px]="tipTop()" [style.left.px]="tipLeft()" (click)="$event.stopPropagation()">
          <p class="text-sm font-semibold text-white">{{ tour.step()?.title }}</p>
          <p class="text-xs text-gr-300 mt-1.5 leading-relaxed">{{ tour.step()?.body }}</p>
          <div class="flex items-center justify-between mt-4">
            <span class="text-[11px] num text-gr-400">{{ tour.index() + 1 }} / {{ tour.count() }}</span>
            <div class="flex gap-2">
              <button class="btn-ghost text-xs" (click)="tour.stop()">{{ t().common.close }}</button>
              @if (tour.index() > 0) { <button class="btn-secondary text-xs" (click)="tour.prev()">{{ t().common.back }}</button> }
              <button class="btn-primary text-xs" (click)="tour.next()">
                {{ tour.index() + 1 >= tour.count() ? t().common.confirm : 'Next' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    }
  `,
})
export class TourComponent {
  protected readonly tour = inject(TourService);
  private readonly lang = inject(LanguageService);
  protected readonly t = this.lang.t;

  protected readonly box = signal<Box | null>(null);

  constructor() {
    // Recompute the highlight box whenever the active step changes.
    effect(() => {
      this.tour.index();
      if (this.tour.active()) queueMicrotask(() => this.measure());
    });
  }

  @HostListener('window:resize') @HostListener('window:scroll')
  protected measure(): void {
    const step = this.tour.step();
    if (!step) { this.box.set(null); return; }
    const el = document.querySelector(step.selector);
    if (!el) { this.box.set(null); this.tour.next(); return; }
    el.scrollIntoView({ block: 'center', behavior: 'smooth' });
    const r = el.getBoundingClientRect();
    this.box.set({ top: r.top, left: r.left, width: r.width, height: r.height });
  }

  protected readonly tipTop = computed(() => {
    const b = this.box(); if (!b) return 0;
    const below = b.top + b.height + 14;
    return below + 180 > window.innerHeight ? Math.max(12, b.top - 190) : below;
  });
  protected readonly tipLeft = computed(() => {
    const b = this.box(); if (!b) return 0;
    return Math.min(Math.max(12, b.left), window.innerWidth - 300);
  });
}
