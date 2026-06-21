import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { parseApiError } from '../../core/api-error';
import { ApiService } from '../../core/api.service';
import { LanguageService } from '../../core/language.service';
import { DatasetSummary } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'pb-datasets-list',
  standalone: true,
  imports: [LucideAngularModule, DatePipe, DecimalPipe, RouterLink, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="max-w-7xl mx-auto px-4 sm:px-6 py-8">
      <div class="flex flex-wrap items-end justify-between gap-4 mb-6">
        <div>
          <h1 class="text-xl font-semibold text-white">{{ t().datasets.title }}</h1>
          <p class="text-sm text-gr-300 mt-0.5">{{ t().datasets.subtitle }}</p>
        </div>
        <button class="btn-primary" (click)="openUpload()">
          <lucide-icon name="plus" class="w-4 h-4"></lucide-icon>Upload CSV
        </button>
      </div>

      @if (loading()) {
        <div class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          @for (i of [1,2,3]; track i) { <div class="widget p-5 h-32 skeleton"></div> }
        </div>
      } @else if (datasets().length === 0) {
        <div class="widget p-10 text-center text-gr-200">{{ t().datasets.empty }}</div>
      } @else {
        <div class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          @for (d of datasets(); track d.id) {
            <a [routerLink]="['/datasets', d.id]" class="widget p-5 block group">
              <div class="flex items-start justify-between gap-2">
                <h2 class="font-semibold text-white truncate group-hover:text-cy-300 transition-colors">{{ d.name }}</h2>
                <span class="chip shrink-0"
                      [class.text-li-400]="d.status === 'Ready'"
                      [class.text-amber-400]="d.status === 'Processing'"
                      [class.text-rose-400]="d.status === 'Failed'">{{ lang.status(d.status) }}</span>
              </div>
              <p class="text-xs text-gr-300 mt-1 line-clamp-2 min-h-[2rem]">{{ d.description }}</p>
              <div class="flex items-center gap-2 mt-3 text-xs text-gr-400 font-mono">
                <span class="num text-gr-200">{{ d.rowCount | number }}</span><span>{{ t().common.rows }}</span>
                <span>·</span>
                <span class="num text-gr-200">{{ d.columnCount }}</span><span>{{ t().common.columns }}</span>
              </div>
              <div class="flex items-center justify-between mt-3 pt-3 border-t border-gr-700">
                <span class="text-[11px] text-gr-400">{{ t().common.updated }} {{ d.updatedAt | date:'mediumDate':'':lang.dateLocale() }}</span>
                <span class="btn-ghost text-xs">{{ t().datasets.viewTable }}<lucide-icon name="chevron-right" class="w-3.5 h-3.5"></lucide-icon></span>
              </div>
            </a>
          }
        </div>
      }
    </section>

    @if (uploading()) {
      <div class="fixed inset-0 z-50 grid place-items-center bg-gr-950/70 backdrop-blur-sm px-4" (click)="uploading.set(false)">
        <div class="widget w-full max-w-md p-6" (click)="$event.stopPropagation()">
          <h2 class="text-lg font-semibold text-white mb-4">Upload CSV</h2>
          <div class="space-y-4">
            <div>
              <label class="label">Name</label>
              <input class="input" [(ngModel)]="name" placeholder="e.g. Regional visits Q3">
            </div>
            <div>
              <label class="label">CSV file</label>
              <label class="flex items-center gap-3 rounded-lg border border-dashed border-gr-600 px-4 py-6 cursor-pointer hover:border-cy-400 transition-colors">
                <lucide-icon name="database" class="w-5 h-5 text-gr-400"></lucide-icon>
                <span class="text-sm text-gr-200 truncate">{{ file?.name || 'Choose a .csv file…' }}</span>
                <input type="file" accept=".csv,text/csv" class="hidden" (change)="onFile($event)">
              </label>
            </div>
            @if (error()) { <p class="field-error">{{ error() }}</p> }
            <div class="flex justify-end gap-2 pt-1">
              <button class="btn-secondary" (click)="uploading.set(false)">{{ t().common.cancel }}</button>
              <button class="btn-primary" (click)="upload()" [disabled]="saving() || !name || !file">
                @if (saving()) { <lucide-icon name="loader-2" class="w-4 h-4 animate-spin"></lucide-icon>Processing… }
                @else { Upload }
              </button>
            </div>
          </div>
        </div>
      </div>
    }
  `,
})
export class DatasetsListComponent {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  protected readonly lang = inject(LanguageService);
  protected readonly t = this.lang.t;

  protected readonly datasets = signal<DatasetSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly uploading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected name = '';
  protected file: File | null = null;

  constructor() { this.load(); }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.datasets.set(await this.api.getDatasets());
    } catch (err) {
      this.toast.error(parseApiError(err, 'Could not load datasets.', this.lang.lang())[0]);
    } finally {
      this.loading.set(false);
    }
  }

  protected openUpload(): void {
    this.name = ''; this.file = null; this.error.set(null);
    this.uploading.set(true);
  }

  protected onFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file = input.files?.[0] ?? null;
    if (this.file && !this.name) this.name = this.file.name.replace(/\.csv$/i, '');
  }

  protected async upload(): Promise<void> {
    if (this.saving() || !this.name || !this.file) return;
    this.saving.set(true);
    this.error.set(null);
    try {
      const created = await this.api.uploadDataset(this.name.trim(), this.file);
      this.datasets.update(list => [created, ...list]);
      this.uploading.set(false);
      this.toast.success(`Dataset ready · ${created.rowCount} rows.`);
    } catch (err) {
      this.error.set(parseApiError(err, 'Upload failed.', this.lang.lang())[0]);
    } finally {
      this.saving.set(false);
    }
  }
}
