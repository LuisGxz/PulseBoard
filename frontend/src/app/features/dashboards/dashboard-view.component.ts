import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { parseApiError } from '../../core/api-error';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { LanguageService } from '../../core/language.service';
import { DashboardDetail, DatasetColumn, SaveWidgetRequest, WidgetDto, WidgetWithData } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { TourService } from '../../core/tour.service';
import { WidgetCardComponent } from '../../shared/widget-card.component';
import { WidgetEditorComponent } from './widget-editor.component';

@Component({
  selector: 'pb-dashboard-view',
  standalone: true,
  imports: [RouterLink, FormsModule, LucideAngularModule, DragDropModule, WidgetCardComponent, WidgetEditorComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="max-w-7xl mx-auto px-4 sm:px-6 py-8">
      <a routerLink="/dashboards" class="inline-flex items-center gap-1.5 text-sm text-gr-300 hover:text-white transition-colors mb-5">
        <lucide-icon name="arrow-left" class="w-4 h-4"></lucide-icon>{{ t().dashboards.title }}
      </a>

      @if (loading()) {
        <div class="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
          @for (i of [1,2,3,4]; track i) { <div class="widget p-5 h-28 skeleton"></div> }
        </div>
      } @else if (error()) {
        <div class="widget p-8 text-center">
          <lucide-icon name="alert-triangle" class="w-7 h-7 text-rose-400 mx-auto mb-2"></lucide-icon>
          <p class="text-gr-200">{{ error() }}</p>
          <button class="btn-secondary mt-4" (click)="load()">{{ t().common.retry }}</button>
        </div>
      } @else if (board(); as b) {
        <div class="flex flex-wrap items-end justify-between gap-3 mb-6">
          <div>
            <h1 class="text-xl font-semibold text-white">{{ b.name }}</h1>
            <p class="text-xs text-gr-300 mt-0.5">
              {{ b.description }} · <span class="num text-cy-400">{{ b.datasetName }}</span>
            </p>
          </div>

          <div class="flex flex-wrap items-center gap-2">
            <button class="btn-ghost" (click)="startTour()" aria-label="Guided demo">
              <lucide-icon name="sparkles" class="w-4 h-4"></lucide-icon><span class="hidden sm:inline">Demo</span>
            </button>
            <div data-tour="range" class="flex items-center gap-1.5 chip !py-1.5">
              <lucide-icon name="calendar" class="w-3.5 h-3.5 text-gr-400"></lucide-icon>
              <input type="date" class="bg-transparent text-xs num text-gr-100 outline-none w-[7.5rem]" [(ngModel)]="from" (change)="applyRange()">
              <span class="text-gr-500">→</span>
              <input type="date" class="bg-transparent text-xs num text-gr-100 outline-none w-[7.5rem]" [(ngModel)]="to" (change)="applyRange()">
              @if (from || to) {
                <button class="text-gr-400 hover:text-white" (click)="clearRange()"><lucide-icon name="x" class="w-3.5 h-3.5"></lucide-icon></button>
              }
            </div>

            @if (canEdit()) {
              <button data-tour="edit" class="btn-secondary" [class.!border-cy-400]="editing()" [class.!text-cy-400]="editing()" (click)="toggleEdit()">
                <lucide-icon [name]="editing() ? 'check' : 'settings-2'" class="w-4 h-4"></lucide-icon>
                {{ editing() ? t().common.save : 'Edit' }}
              </button>
              @if (editing()) {
                <button class="btn-primary" (click)="openEditor(null)"><lucide-icon name="plus" class="w-4 h-4"></lucide-icon>Add widget</button>
              }
            }
            <span class="chip">{{ lang.role(b.role) }}</span>
          </div>
        </div>

        <div data-tour="grid" class="grid grid-cols-2 lg:grid-cols-12 gap-4 items-start"
             cdkDropList cdkDropListOrientation="mixed" [cdkDropListDisabled]="!editing()" (cdkDropListDropped)="onDrop($event)">
          @for (w of widgets(); track w.widget.id) {
            <div class="widget p-5 group relative" [class]="spanClass(w.widget)" [attr.data-tour]="$first ? 'widget' : null"
                 cdkDrag [cdkDragDisabled]="!editing()">
              <div class="flex items-center justify-between mb-3 gap-2">
                <div class="flex items-center gap-2 min-w-0">
                  @if (editing()) {
                    <button class="text-gr-500 hover:text-gr-200 cursor-grab active:cursor-grabbing" cdkDragHandle>
                      <lucide-icon name="grip-vertical" class="w-4 h-4"></lucide-icon>
                    </button>
                  }
                  <p class="text-sm font-medium text-gr-100 truncate">{{ w.widget.title }}</p>
                </div>
                <div class="flex items-center gap-1 shrink-0">
                  @if (editing()) {
                    <button class="btn-ghost !px-1.5" (click)="openEditor(w.widget)" aria-label="Edit widget">
                      <lucide-icon name="pencil" class="w-4 h-4"></lucide-icon></button>
                    <button class="btn-ghost !px-1.5 text-rose-400" (click)="removeWidget(w.widget)" aria-label="Delete widget">
                      <lucide-icon name="trash-2" class="w-4 h-4"></lucide-icon></button>
                  } @else {
                    <button class="btn-ghost !px-1.5 opacity-0 group-hover:opacity-100 transition-opacity"
                            (click)="exportWidget(w.widget)" [attr.aria-label]="t().common.export">
                      <lucide-icon name="download" class="w-4 h-4"></lucide-icon></button>
                    <span class="chip text-[10px]">{{ lang.widget(w.widget.type) }}</span>
                  }
                </div>
              </div>
              <pb-widget-card [widget]="w" />
            </div>
          }
        </div>

        @if (widgets().length === 0) {
          <div class="widget p-10 text-center mt-4">
            <lucide-icon name="bar-chart-3" class="w-8 h-8 text-gr-500 mx-auto mb-3"></lucide-icon>
            <p class="text-gr-200">{{ t().common.empty }}</p>
            @if (canEdit()) { <button class="btn-primary mt-4" (click)="enterEditAndAdd()">Add your first widget</button> }
          </div>
        }
      }
    </section>

    @if (editorOpen()) {
      <pb-widget-editor
        [datasetId]="board()!.datasetId" [columns]="columns()" [existing]="editorWidget()"
        (save)="saveWidget($event)" (cancel)="editorOpen.set(false)" />
    }
  `,
})
export class DashboardViewComponent implements OnInit {
  readonly id = input.required<string>();
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly tour = inject(TourService);
  protected readonly lang = inject(LanguageService);
  protected readonly t = this.lang.t;

  protected readonly board = signal<DashboardDetail | null>(null);
  protected readonly columns = signal<DatasetColumn[]>([]);
  protected readonly widgets = signal<WidgetWithData[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly editing = signal(false);
  protected readonly editorOpen = signal(false);
  protected readonly editorWidget = signal<WidgetDto | null>(null);
  protected from = '';
  protected to = '';

  protected readonly canEdit = computed(() => {
    const r = this.board()?.role;
    return r === 'Owner' || r === 'Editor';
  });

  ngOnInit(): void { this.load(); }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const board = await this.api.getDashboard(this.id(), this.from || undefined, this.to || undefined);
      this.board.set(board);
      this.widgets.set(board.widgets);
      if (this.columns().length === 0) {
        const ds = await this.api.getDataset(board.datasetId);
        this.columns.set(ds.columns);
      }
      // Offer the guided demo once, after the first board has rendered.
      if (this.widgets().length > 0) setTimeout(() => this.tour.startOnce(this.tourSteps()), 700);
    } catch (err) {
      this.error.set(parseApiError(err, 'Could not load dashboard.', this.lang.lang())[0]);
    } finally {
      this.loading.set(false);
    }
  }

  protected applyRange(): void { this.load(); }
  protected clearRange(): void { this.from = ''; this.to = ''; this.load(); }

  protected startTour(): void { this.tour.start(this.tourSteps()); }

  private tourSteps() {
    const steps = [
      { selector: '[data-tour=widget]', title: 'Live widgets', body: 'Every widget renders a server-side aggregation over the dataset — KPI, line, donut, bar or heatmap.' },
      { selector: '[data-tour=range]', title: 'Date range', body: 'Filter the whole board by a date range; every chart recomputes against it.' },
    ];
    if (this.canEdit()) steps.push(
      { selector: '[data-tour=edit]', title: 'Build it your way', body: 'Editors can add and configure widgets with a live preview, and drag to reorder.' });
    return steps;
  }

  protected spanClass(w: WidgetDto): string {
    switch (w.type) {
      case 'Kpi': return 'col-span-1 lg:col-span-3';
      case 'Line': case 'Heatmap': return 'col-span-2 lg:col-span-8';
      default: return 'col-span-2 lg:col-span-4';
    }
  }

  protected toggleEdit(): void { this.editing.update(v => !v); }
  protected enterEditAndAdd(): void { this.editing.set(true); this.openEditor(null); }

  protected openEditor(widget: WidgetDto | null): void {
    this.editorWidget.set(widget);
    this.editorOpen.set(true);
  }

  protected async saveWidget(body: SaveWidgetRequest): Promise<void> {
    const dashId = this.id();
    const existing = this.editorWidget();
    try {
      if (existing) await this.api.updateWidget(dashId, existing.id, body);
      else await this.api.createWidget(dashId, body);
      this.editorOpen.set(false);
      this.toast.success(existing ? 'Widget updated.' : 'Widget added.');
      await this.load();
    } catch (err) {
      this.toast.error(parseApiError(err, 'Could not save widget.', this.lang.lang())[0]);
    }
  }

  protected async removeWidget(widget: WidgetDto): Promise<void> {
    if (!confirm(`Delete "${widget.title}"?`)) return;
    try {
      await this.api.deleteWidget(this.id(), widget.id);
      this.widgets.update(list => list.filter(w => w.widget.id !== widget.id));
      this.toast.success('Widget deleted.');
    } catch (err) {
      this.toast.error(parseApiError(err, 'Could not delete.', this.lang.lang())[0]);
    }
  }

  protected async onDrop(event: CdkDragDrop<WidgetWithData[]>): Promise<void> {
    if (event.previousIndex === event.currentIndex) return;
    const list = [...this.widgets()];
    moveItemInArray(list, event.previousIndex, event.currentIndex);
    this.widgets.set(list);
    try {
      await this.api.reorderWidgets(this.id(), list.map((w, i) => ({
        widgetId: w.widget.id, position: i,
        gridX: w.widget.gridX, gridY: w.widget.gridY, gridW: w.widget.gridW, gridH: w.widget.gridH,
      })));
    } catch (err) {
      this.toast.error(parseApiError(err, 'Could not reorder.', this.lang.lang())[0]);
      this.load();
    }
  }

  protected async exportWidget(widget: WidgetDto): Promise<void> {
    try {
      const { blob, filename } = await this.api.exportWidget(this.id(), widget.id, this.from || undefined, this.to || undefined);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url; a.download = filename;
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      this.toast.error(parseApiError(err, 'Could not export.', this.lang.lang())[0]);
    }
  }
}
