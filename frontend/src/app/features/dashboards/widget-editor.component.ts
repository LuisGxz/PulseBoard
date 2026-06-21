import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { ApiService } from '../../core/api.service';
import { LanguageService } from '../../core/language.service';
import {
  Aggregation, AggregationSpec, DatasetColumn, FilterClause, FilterOp,
  SaveWidgetRequest, WidgetDto, WidgetType, WidgetWithData,
} from '../../core/models';
import { WidgetCardComponent } from '../../shared/widget-card.component';

const TYPES: WidgetType[] = ['Kpi', 'Line', 'Bar', 'Donut', 'Heatmap', 'Table'];
const AGGS: Aggregation[] = ['Sum', 'Avg', 'Count', 'CountDistinct', 'Min', 'Max'];
const OPS: FilterOp[] = ['eq', 'ne', 'gt', 'gte', 'lt', 'lte', 'contains'];

/**
 * Config panel for creating/editing a widget, with a debounced live preview driven by the same
 * /api/query endpoint the saved widget will use — so what you build is exactly what you get.
 */
@Component({
  selector: 'pb-widget-editor',
  standalone: true,
  imports: [FormsModule, LucideAngularModule, WidgetCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="fixed inset-0 z-50 flex bg-gr-950/70 backdrop-blur-sm" (click)="cancel.emit()">
      <div class="ml-auto h-full w-full max-w-3xl bg-gr-900 border-l border-gr-700 flex flex-col" (click)="$event.stopPropagation()">
        <header class="flex items-center justify-between px-5 py-3.5 border-b border-gr-700 shrink-0">
          <h2 class="font-semibold text-white">{{ existing() ? edit : add }}</h2>
          <button class="btn-ghost" (click)="cancel.emit()"><lucide-icon name="x" class="w-4 h-4"></lucide-icon></button>
        </header>

        <div class="flex-1 overflow-auto grid lg:grid-cols-[1fr_1.1fr]">
          <!-- form -->
          <div class="p-5 space-y-4 border-b lg:border-b-0 lg:border-r border-gr-700">
            <div>
              <label class="label">{{ t().dashboards.nameLabel }}</label>
              <input class="input" [(ngModel)]="title" (ngModelChange)="touch()">
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="label">Type</label>
                <select class="select" [(ngModel)]="type" (ngModelChange)="touch()">
                  @for (ty of types; track ty) { <option [value]="ty">{{ lang.widget(ty) }}</option> }
                </select>
              </div>
              <div>
                <label class="label">Aggregation</label>
                <select class="select" [(ngModel)]="aggregation" (ngModelChange)="touch()">
                  @for (a of aggs; track a) { <option [value]="a">{{ lang.agg(a) }}</option> }
                </select>
              </div>
            </div>

            @if (needsMetric()) {
              <div>
                <label class="label">Metric column</label>
                <select class="select" [(ngModel)]="metricColumn" (ngModelChange)="touch()">
                  <option [ngValue]="null">—</option>
                  @for (c of numericColumns(); track c.name) { <option [value]="c.name">{{ c.label }}</option> }
                </select>
              </div>
            }

            @if (type !== 'Kpi') {
              <div>
                <label class="label">{{ type === 'Heatmap' ? 'Primary dimension (X)' : 'Group by' }}</label>
                <select class="select" [(ngModel)]="dimensionColumn" (ngModelChange)="onDimChange()">
                  <option [ngValue]="null">—</option>
                  @for (c of columns(); track c.name) { <option [value]="c.name">{{ c.label }}</option> }
                </select>
              </div>
            }

            @if (type === 'Heatmap') {
              <div>
                <label class="label">Secondary dimension (Y)</label>
                <select class="select" [(ngModel)]="secondaryDimensionColumn" (ngModelChange)="touch()">
                  <option [ngValue]="null">—</option>
                  @for (c of columns(); track c.name) { <option [value]="c.name">{{ c.label }}</option> }
                </select>
              </div>
            }

            <div class="grid grid-cols-2 gap-3">
              @if (isDateDim()) {
                <div>
                  <label class="label">Granularity</label>
                  <select class="select" [(ngModel)]="dateGranularity" (ngModelChange)="touch()">
                    <option [ngValue]="null">—</option>
                    <option value="day">Day</option><option value="week">Week</option><option value="month">Month</option>
                  </select>
                </div>
              }
              @if (type !== 'Kpi' && type !== 'Heatmap' && !isDateDim()) {
                <div>
                  <label class="label">Top N</label>
                  <input type="number" min="1" class="input" [(ngModel)]="limit" (ngModelChange)="touch()">
                </div>
              }
            </div>

            <!-- filters -->
            <div>
              <div class="flex items-center justify-between mb-1.5">
                <label class="label !mb-0">Filters</label>
                <button class="btn-ghost text-xs" (click)="addFilter()"><lucide-icon name="plus" class="w-3.5 h-3.5"></lucide-icon></button>
              </div>
              <div class="space-y-2">
                @for (f of filters(); track $index) {
                  <div class="flex items-center gap-1.5">
                    <select class="select !py-1.5 text-xs" [(ngModel)]="f.column" (ngModelChange)="touch()">
                      @for (c of columns(); track c.name) { <option [value]="c.name">{{ c.label }}</option> }
                    </select>
                    <select class="select !py-1.5 text-xs w-24" [(ngModel)]="f.op" (ngModelChange)="touch()">
                      @for (op of ops; track op) { <option [value]="op">{{ op }}</option> }
                    </select>
                    <input class="input !py-1.5 text-xs" [(ngModel)]="$any(f).value" (ngModelChange)="touch()">
                    <button class="btn-ghost text-rose-400 !px-1.5" (click)="removeFilter($index)">
                      <lucide-icon name="trash-2" class="w-3.5 h-3.5"></lucide-icon>
                    </button>
                  </div>
                }
              </div>
            </div>
          </div>

          <!-- live preview -->
          <div class="p-5 bg-gr-950/40">
            <p class="text-[11px] font-mono uppercase tracking-widest text-cy-400 mb-3">Live preview</p>
            <div class="widget p-5 min-h-48">
              <p class="text-sm font-medium text-gr-100 mb-3">{{ title || '—' }}</p>
              @if (previewLoading()) {
                <div class="h-40 skeleton"></div>
              } @else if (preview(); as pv) {
                <pb-widget-card [widget]="pv" />
              }
            </div>
          </div>
        </div>

        <footer class="flex items-center justify-end gap-2 px-5 py-3.5 border-t border-gr-700 shrink-0">
          <button class="btn-secondary" (click)="cancel.emit()">{{ t().common.cancel }}</button>
          <button class="btn-primary" (click)="emitSave()" [disabled]="!title">{{ t().common.save }}</button>
        </footer>
      </div>
    </div>
  `,
})
export class WidgetEditorComponent {
  readonly datasetId = input.required<string>();
  readonly columns = input.required<DatasetColumn[]>();
  readonly existing = input<WidgetDto | null>(null);

  readonly save = output<SaveWidgetRequest>();
  readonly cancel = output<void>();

  private readonly api = inject(ApiService);
  protected readonly lang = inject(LanguageService);
  protected readonly t = this.lang.t;
  protected readonly types = TYPES;
  protected readonly aggs = AGGS;
  protected readonly ops = OPS;
  protected readonly add = 'Add widget';
  protected readonly edit = 'Edit widget';

  protected type: WidgetType = 'Bar';
  protected title = '';
  protected aggregation: Aggregation = 'Count';
  protected metricColumn: string | null = null;
  protected dimensionColumn: string | null = null;
  protected secondaryDimensionColumn: string | null = null;
  protected dateGranularity: string | null = null;
  protected limit: number | null = 8;
  protected readonly filters = signal<FilterClause[]>([]);

  protected readonly preview = signal<WidgetWithData | null>(null);
  protected readonly previewLoading = signal(false);
  private readonly dirty = signal(0);
  private debounce?: ReturnType<typeof setTimeout>;

  protected readonly numericColumns = computed(() => this.columns().filter(c => c.type === 'Number' || c.type === 'Boolean'));

  private seeded = false;

  constructor() {
    // Seed the form once, from an existing widget or sensible defaults for the dataset's columns.
    effect(() => {
      const w = this.existing();
      const cols = this.columns();
      if (this.seeded || (!w && cols.length === 0)) return;
      this.seeded = true;

      if (w) {
        this.type = w.type; this.title = w.title; this.aggregation = w.aggregation;
        this.metricColumn = w.metricColumn; this.dimensionColumn = w.dimensionColumn;
        this.secondaryDimensionColumn = w.secondaryDimensionColumn; this.dateGranularity = w.dateGranularity;
        this.limit = w.limit;
        this.filters.set(parseFilters(w.filtersJson));
      } else {
        // A new widget previews immediately: count by the first categorical column.
        this.title = 'New widget';
        this.type = 'Bar';
        this.aggregation = 'Count';
        this.dimensionColumn = cols.find(c => c.type === 'String')?.name ?? cols[0]?.name ?? null;
      }
      this.touch();
    });

    // Debounced live preview whenever the form changes.
    effect(() => {
      this.dirty();
      clearTimeout(this.debounce);
      this.debounce = setTimeout(() => this.runPreview(), 350);
    });
  }

  protected needsMetric(): boolean { return this.aggregation !== 'Count'; }
  protected isDateDim(): boolean {
    return this.columns().find(c => c.name === this.dimensionColumn)?.type === 'Date';
  }
  protected touch(): void { this.dirty.update(v => v + 1); }
  protected onDimChange(): void { if (!this.isDateDim()) this.dateGranularity = null; this.touch(); }

  protected addFilter(): void {
    const first = this.columns()[0]?.name ?? '';
    this.filters.update(list => [...list, { column: first, op: 'eq', value: '' }]);
    this.touch();
  }
  protected removeFilter(i: number): void { this.filters.update(list => list.filter((_, idx) => idx !== i)); this.touch(); }

  private buildSpec(): AggregationSpec {
    return {
      datasetId: this.datasetId(),
      metricColumn: this.metricColumn,
      aggregation: this.aggregation,
      dimensionColumn: this.type === 'Kpi' ? null : this.dimensionColumn,
      secondaryDimensionColumn: this.type === 'Heatmap' ? this.secondaryDimensionColumn : null,
      dateGranularity: this.dateGranularity,
      limit: this.limit,
      filtersJson: this.filtersJson(),
    };
  }

  private filtersJson(): string | null {
    const valid = this.filters().filter(f => f.column && `${f.value}` !== '');
    return valid.length ? JSON.stringify(valid) : null;
  }

  private async runPreview(): Promise<void> {
    this.previewLoading.set(true);
    const spec = this.buildSpec();
    const widget: WidgetDto = {
      id: 'preview', type: this.type, title: this.title, position: 0,
      gridX: 0, gridY: 0, gridW: 4, gridH: 1,
      metricColumn: spec.metricColumn ?? null, aggregation: this.aggregation,
      dimensionColumn: spec.dimensionColumn ?? null, secondaryDimensionColumn: spec.secondaryDimensionColumn ?? null,
      dateGranularity: spec.dateGranularity ?? null, limit: spec.limit ?? null, filtersJson: spec.filtersJson ?? null,
    };
    try {
      const data = await this.api.runQuery(spec);
      this.preview.set({ widget, data, error: null });
    } catch (err) {
      this.preview.set({ widget, data: null, error: errMessage(err) });
    } finally {
      this.previewLoading.set(false);
    }
  }

  protected emitSave(): void {
    const w = this.existing();
    this.save.emit({
      type: this.type, title: this.title.trim() || 'Widget',
      gridX: w?.gridX ?? 0, gridY: w?.gridY ?? 0, gridW: w?.gridW ?? defaultW(this.type), gridH: w?.gridH ?? 1,
      metricColumn: this.needsMetric() ? this.metricColumn : null,
      aggregation: this.aggregation,
      dimensionColumn: this.type === 'Kpi' ? null : this.dimensionColumn,
      secondaryDimensionColumn: this.type === 'Heatmap' ? this.secondaryDimensionColumn : null,
      dateGranularity: this.dateGranularity,
      limit: this.type === 'Kpi' || this.type === 'Heatmap' ? null : this.limit,
      filtersJson: this.filtersJson(),
    });
  }
}

function defaultW(type: WidgetType): number {
  return type === 'Kpi' ? 3 : type === 'Line' || type === 'Heatmap' ? 8 : 4;
}

function parseFilters(json: string | null): FilterClause[] {
  if (!json) return [];
  try {
    const arr = JSON.parse(json);
    return Array.isArray(arr) ? arr.map(f => ({ column: f.column, op: f.op, value: f.value })) : [];
  } catch { return []; }
}

function errMessage(err: unknown): string {
  const e = err as { error?: { detail?: string; title?: string } };
  return e?.error?.detail ?? e?.error?.title ?? 'Query failed.';
}
