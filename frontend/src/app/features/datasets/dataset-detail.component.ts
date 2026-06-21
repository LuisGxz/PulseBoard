import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { parseApiError } from '../../core/api-error';
import { ApiService } from '../../core/api.service';
import { LanguageService } from '../../core/language.service';
import { DatasetColumn, DatasetDetail, DatasetRows, FilterClause, FilterOp } from '../../core/models';

const OPS: FilterOp[] = ['eq', 'ne', 'gt', 'gte', 'lt', 'lte', 'contains'];

@Component({
  selector: 'pb-dataset-detail',
  standalone: true,
  imports: [RouterLink, FormsModule, LucideAngularModule, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="max-w-7xl mx-auto px-4 sm:px-6 py-8">
      <a routerLink="/datasets" class="inline-flex items-center gap-1.5 text-sm text-gr-300 hover:text-white transition-colors mb-5">
        <lucide-icon name="arrow-left" class="w-4 h-4"></lucide-icon>{{ t().datasets.title }}
      </a>

      @if (loading() && !detail()) {
        <div class="widget p-5 h-80 skeleton"></div>
      } @else if (error()) {
        <div class="widget p-8 text-center">
          <lucide-icon name="alert-triangle" class="w-7 h-7 text-rose-400 mx-auto mb-2"></lucide-icon>
          <p class="text-gr-200">{{ error() }}</p>
          <button class="btn-secondary mt-4" (click)="reload()">{{ t().common.retry }}</button>
        </div>
      } @else if (detail(); as d) {
        <div class="mb-5">
          <h1 class="text-xl font-semibold text-white">{{ d.name }}</h1>
          <p class="text-xs text-gr-300 mt-0.5 font-mono">
            <span class="num text-gr-200">{{ d.rowCount | number }}</span> {{ t().common.rows }} ·
            <span class="num text-gr-200">{{ d.columns.length }}</span> {{ t().common.columns }}
          </p>
        </div>

        <!-- filter bar -->
        <div class="widget p-4 mb-4">
          <div class="flex items-center justify-between mb-2">
            <span class="label !mb-0 inline-flex items-center gap-1.5"><lucide-icon name="filter" class="w-3.5 h-3.5"></lucide-icon>Filters</span>
            <button class="btn-ghost text-xs" (click)="addFilter()"><lucide-icon name="plus" class="w-3.5 h-3.5"></lucide-icon>Add</button>
          </div>
          @if (filters().length === 0) {
            <p class="text-xs text-gr-400">No filters — showing all rows.</p>
          } @else {
            <div class="space-y-2">
              @for (f of filters(); track $index) {
                <div class="flex items-center gap-1.5">
                  <select class="select !py-1.5 text-xs" [(ngModel)]="f.column">
                    @for (c of d.columns; track c.name) { <option [value]="c.name">{{ c.label }}</option> }
                  </select>
                  <select class="select !py-1.5 text-xs w-24" [(ngModel)]="f.op">
                    @for (op of ops; track op) { <option [value]="op">{{ op }}</option> }
                  </select>
                  <input class="input !py-1.5 text-xs" [(ngModel)]="$any(f).value" (keyup.enter)="apply()">
                  <button class="btn-ghost text-rose-400 !px-1.5" (click)="removeFilter($index)">
                    <lucide-icon name="trash-2" class="w-3.5 h-3.5"></lucide-icon></button>
                </div>
              }
            </div>
            <div class="flex justify-end gap-2 mt-3">
              <button class="btn-secondary text-xs" (click)="clear()">{{ t().common.reset }}</button>
              <button class="btn-primary text-xs" (click)="apply()">{{ t().common.apply }}</button>
            </div>
          }
        </div>

        <!-- table -->
        <div class="widget overflow-hidden">
          <div class="overflow-auto max-h-[60vh]">
            <table class="w-full text-sm">
              <thead class="sticky top-0 bg-gr-850 z-10">
                <tr class="border-b border-gr-700">
                  @for (c of d.columns; track c.name) {
                    <th class="text-left font-medium text-gr-200 px-3 py-2.5 whitespace-nowrap">
                      {{ c.label }}
                      <span class="text-[10px] text-gr-500 font-mono ml-1">{{ lang.type(c.type) }}</span>
                    </th>
                  }
                </tr>
              </thead>
              <tbody>
                @for (row of rows()?.rows || []; track $index) {
                  <tr class="border-b border-gr-800/70 hover:bg-gr-800/40 transition-colors">
                    @for (c of d.columns; track c.name) {
                      <td class="px-3 py-2 whitespace-nowrap"
                          [class.num]="c.type === 'Number' || c.type === 'Date'"
                          [class.text-cy-300]="c.type === 'Number'"
                          [class.text-gr-100]="c.type !== 'Number'">{{ cell(row[c.name]) }}</td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- pagination -->
          <div class="flex items-center justify-between gap-3 px-4 py-3 border-t border-gr-700 text-xs">
            <span class="text-gr-400 num">
              {{ rangeStart() }}–{{ rangeEnd() }} / {{ (rows()?.total || 0) | number }}
            </span>
            <div class="flex items-center gap-1">
              <button class="btn-secondary !py-1.5 !px-2" [disabled]="page() <= 1 || loading()" (click)="go(page() - 1)">
                <lucide-icon name="chevron-left" class="w-4 h-4"></lucide-icon></button>
              <span class="num text-gr-300 px-2">{{ page() }} / {{ totalPages() }}</span>
              <button class="btn-secondary !py-1.5 !px-2" [disabled]="page() >= totalPages() || loading()" (click)="go(page() + 1)">
                <lucide-icon name="chevron-right" class="w-4 h-4"></lucide-icon></button>
            </div>
          </div>
        </div>
      }
    </section>
  `,
})
export class DatasetDetailComponent implements OnInit {
  readonly id = input.required<string>();
  private readonly api = inject(ApiService);
  protected readonly lang = inject(LanguageService);
  protected readonly t = this.lang.t;
  protected readonly ops = OPS;

  protected readonly detail = signal<DatasetDetail | null>(null);
  protected readonly rows = signal<DatasetRows | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly filters = signal<FilterClause[]>([]);
  private appliedFilters = '';
  private readonly pageSize = 25;

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil((this.rows()?.total ?? 0) / this.pageSize)));
  protected readonly rangeStart = computed(() => ((this.page() - 1) * this.pageSize) + ((this.rows()?.rows.length ?? 0) ? 1 : 0));
  protected readonly rangeEnd = computed(() => (this.page() - 1) * this.pageSize + (this.rows()?.rows.length ?? 0));

  ngOnInit(): void { this.reload(); }

  protected async reload(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      if (!this.detail()) this.detail.set(await this.api.getDataset(this.id()));
      this.rows.set(await this.api.getDatasetRows(this.id(), this.page(), this.pageSize, this.appliedFilters || undefined));
    } catch (err) {
      this.error.set(parseApiError(err, 'Could not load dataset.', this.lang.lang())[0]);
    } finally {
      this.loading.set(false);
    }
  }

  protected go(p: number): void { this.page.set(p); this.reload(); }

  protected addFilter(): void {
    const first = this.detail()?.columns[0]?.name ?? '';
    this.filters.update(list => [...list, { column: first, op: 'eq', value: '' }]);
  }
  protected removeFilter(i: number): void {
    this.filters.update(list => list.filter((_, idx) => idx !== i));
    this.apply();
  }
  protected apply(): void {
    const valid = this.filters().filter(f => f.column && `${f.value}` !== '');
    this.appliedFilters = valid.length ? JSON.stringify(valid) : '';
    this.page.set(1);
    this.reload();
  }
  protected clear(): void { this.filters.set([]); this.appliedFilters = ''; this.page.set(1); this.reload(); }

  protected cell(value: unknown): string {
    if (value === null || value === undefined) return '—';
    if (typeof value === 'boolean') return value ? 'true' : 'false';
    if (typeof value === 'number') return value.toLocaleString(this.lang.dateLocale());
    return String(value);
  }
}
