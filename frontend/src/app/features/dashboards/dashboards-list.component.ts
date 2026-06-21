import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { parseApiError } from '../../core/api-error';
import { ApiService } from '../../core/api.service';
import { LanguageService } from '../../core/language.service';
import { DashboardSummary, DatasetSummary } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'pb-dashboards-list',
  standalone: true,
  imports: [FormsModule, RouterLink, LucideAngularModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="max-w-7xl mx-auto px-4 sm:px-6 py-8">
      <div class="flex flex-wrap items-end justify-between gap-4 mb-6">
        <div>
          <h1 class="text-xl font-semibold text-white">{{ t().dashboards.title }}</h1>
          <p class="text-sm text-gr-300 mt-0.5">{{ t().dashboards.subtitle }}</p>
        </div>
        <button class="btn-primary" (click)="openCreate()">
          <lucide-icon name="plus" class="w-4 h-4"></lucide-icon>{{ t().dashboards.newDashboard }}
        </button>
      </div>

      @if (loading()) {
        <div class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          @for (i of [1,2,3]; track i) { <div class="widget p-5 h-32 skeleton"></div> }
        </div>
      } @else if (dashboards().length === 0) {
        <div class="widget p-10 text-center">
          <lucide-icon name="layout-dashboard" class="w-8 h-8 text-gr-500 mx-auto mb-3"></lucide-icon>
          <p class="text-gr-200">{{ t().dashboards.empty }}</p>
          <button class="btn-primary mt-4" (click)="openCreate()">{{ t().dashboards.emptyCta }}</button>
        </div>
      } @else {
        <div class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          @for (d of dashboards(); track d.id) {
            <div class="widget p-5 flex flex-col group">
              <div class="flex items-start justify-between gap-2">
                <a [routerLink]="['/dashboards', d.id]" class="min-w-0">
                  <h2 class="font-semibold text-white truncate group-hover:text-cy-300 transition-colors">{{ d.name }}</h2>
                </a>
                <span class="chip shrink-0">{{ lang.role(d.role) }}</span>
              </div>
              <p class="text-xs text-gr-300 mt-1 line-clamp-2 min-h-[2rem]">{{ d.description || '—' }}</p>
              <div class="flex items-center gap-3 text-xs text-gr-400 mt-3 font-mono">
                <span class="inline-flex items-center gap-1"><lucide-icon name="database" class="w-3.5 h-3.5"></lucide-icon>{{ d.datasetName }}</span>
                <span>· {{ d.widgetCount }} {{ t().dashboards.widgets }}</span>
              </div>
              <div class="flex items-center justify-between mt-4 pt-3 border-t border-gr-700">
                <span class="text-[11px] text-gr-400">{{ t().common.updated }} {{ d.updatedAt | date:'mediumDate':'':lang.dateLocale() }}</span>
                <div class="flex items-center gap-1">
                  <a [routerLink]="['/dashboards', d.id]" class="btn-ghost text-xs">{{ t().dashboards.openBuilder }}
                    <lucide-icon name="chevron-right" class="w-3.5 h-3.5"></lucide-icon></a>
                  @if (d.role === 'Owner') {
                    <button class="btn-ghost text-rose-400" (click)="remove(d)" [attr.aria-label]="t().common.delete">
                      <lucide-icon name="trash-2" class="w-4 h-4"></lucide-icon>
                    </button>
                  }
                </div>
              </div>
            </div>
          }
        </div>
      }
    </section>

    @if (creating()) {
      <div class="fixed inset-0 z-50 grid place-items-center bg-gr-950/70 backdrop-blur-sm px-4" (click)="creating.set(false)">
        <div class="widget w-full max-w-md p-6" (click)="$event.stopPropagation()">
          <h2 class="text-lg font-semibold text-white mb-4">{{ t().dashboards.createTitle }}</h2>
          <form (ngSubmit)="create()" class="space-y-4">
            <div>
              <label class="label" for="dn">{{ t().dashboards.nameLabel }}</label>
              <input id="dn" name="dn" class="input" [(ngModel)]="form.name" [placeholder]="t().dashboards.namePlaceholder" required>
            </div>
            <div>
              <label class="label" for="dd">{{ t().dashboards.descLabel }}</label>
              <input id="dd" name="dd" class="input" [(ngModel)]="form.description" [placeholder]="t().dashboards.descPlaceholder">
            </div>
            <div>
              <label class="label" for="ds">{{ t().dashboards.datasetLabel }}</label>
              <select id="ds" name="ds" class="select" [(ngModel)]="form.datasetId" required>
                @for (ds of datasets(); track ds.id) { <option [value]="ds.id">{{ ds.name }}</option> }
              </select>
            </div>
            @if (error()) { <p class="field-error">{{ error() }}</p> }
            <div class="flex justify-end gap-2 pt-2">
              <button type="button" class="btn-secondary" (click)="creating.set(false)">{{ t().common.cancel }}</button>
              <button type="submit" class="btn-primary" [disabled]="saving() || !form.name || !form.datasetId">
                {{ t().common.create }}
              </button>
            </div>
          </form>
        </div>
      </div>
    }
  `,
})
export class DashboardsListComponent {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  protected readonly lang = inject(LanguageService);
  protected readonly t = this.lang.t;

  protected readonly dashboards = signal<DashboardSummary[]>([]);
  protected readonly datasets = signal<DatasetSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly creating = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected form = { name: '', description: '', datasetId: '' };

  constructor() { this.load(); }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.dashboards.set(await this.api.getDashboards());
    } catch (err) {
      this.toast.error(parseApiError(err, 'Could not load dashboards.', this.lang.lang())[0]);
    } finally {
      this.loading.set(false);
    }
  }

  protected async openCreate(): Promise<void> {
    this.error.set(null);
    this.form = { name: '', description: '', datasetId: '' };
    this.creating.set(true);
    if (this.datasets().length === 0) {
      try {
        const ds = await this.api.getDatasets();
        this.datasets.set(ds);
        if (ds.length) this.form.datasetId = ds[0].id;
      } catch { /* dataset list optional; user can retry */ }
    } else {
      this.form.datasetId = this.datasets()[0].id;
    }
  }

  protected async create(): Promise<void> {
    if (this.saving() || !this.form.name || !this.form.datasetId) return;
    this.saving.set(true);
    this.error.set(null);
    try {
      const created = await this.api.createDashboard({ ...this.form });
      this.dashboards.update(list => [created, ...list]);
      this.creating.set(false);
      this.toast.success('Dashboard created.');
    } catch (err) {
      this.error.set(parseApiError(err, 'Could not create dashboard.', this.lang.lang())[0]);
    } finally {
      this.saving.set(false);
    }
  }

  protected async remove(d: DashboardSummary): Promise<void> {
    if (!confirm(this.t().dashboards.deleteConfirm)) return;
    try {
      await this.api.deleteDashboard(d.id);
      this.dashboards.update(list => list.filter(x => x.id !== d.id));
      this.toast.success('Dashboard deleted.');
    } catch (err) {
      this.toast.error(parseApiError(err, 'Could not delete.', this.lang.lang())[0]);
    }
  }
}
