import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_URL } from './config';
import {
  AggregationSpec, DashboardDetail, DashboardSummary, DatasetDetail, DatasetRows, DatasetSummary,
  QueryResult, SaveDashboardRequest, SaveWidgetRequest, WidgetDto,
} from './models';

type Params = Record<string, string | number | boolean | undefined | null>;

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  private get<T>(url: string, params?: Params): Promise<T> {
    let p = new HttpParams();
    for (const [k, v] of Object.entries(params ?? {})) if (v !== undefined && v !== null && v !== '') p = p.set(k, String(v));
    return firstValueFrom(this.http.get<T>(`${API_URL}${url}`, { params: p }));
  }
  private post<T>(url: string, body?: unknown): Promise<T> {
    return firstValueFrom(this.http.post<T>(`${API_URL}${url}`, body ?? {}));
  }
  private put<T>(url: string, body?: unknown): Promise<T> {
    return firstValueFrom(this.http.put<T>(`${API_URL}${url}`, body ?? {}));
  }
  private del(url: string): Promise<unknown> {
    return firstValueFrom(this.http.delete(`${API_URL}${url}`));
  }

  // ── Datasets ──
  getDatasets() { return this.get<DatasetSummary[]>('/datasets'); }
  getDataset(id: string) { return this.get<DatasetDetail>(`/datasets/${id}`); }
  getDatasetRows(id: string, page: number, pageSize: number, filters?: string) {
    return this.get<DatasetRows>(`/datasets/${id}/rows`, { page, pageSize, filters });
  }
  uploadDataset(name: string, file: File) {
    const form = new FormData();
    form.append('name', name);
    form.append('file', file);
    return firstValueFrom(this.http.post<DatasetSummary>(`${API_URL}/datasets/upload`, form));
  }

  // ── Dashboards ──
  getDashboards() { return this.get<DashboardSummary[]>('/dashboards'); }
  getDashboard(id: string, from?: string, to?: string) {
    return this.get<DashboardDetail>(`/dashboards/${id}`, { from, to });
  }
  createDashboard(body: SaveDashboardRequest) { return this.post<DashboardSummary>('/dashboards', body); }
  updateDashboard(id: string, body: SaveDashboardRequest) { return this.put<void>(`/dashboards/${id}`, body); }
  deleteDashboard(id: string) { return this.del(`/dashboards/${id}`); }

  // ── Widgets ──
  createWidget(dashboardId: string, body: SaveWidgetRequest) {
    return this.post<WidgetDto>(`/dashboards/${dashboardId}/widgets`, body);
  }
  updateWidget(dashboardId: string, widgetId: string, body: SaveWidgetRequest) {
    return this.put<WidgetDto>(`/dashboards/${dashboardId}/widgets/${widgetId}`, body);
  }
  deleteWidget(dashboardId: string, widgetId: string) {
    return this.del(`/dashboards/${dashboardId}/widgets/${widgetId}`);
  }
  reorderWidgets(dashboardId: string, positions: unknown[]) {
    return this.put<void>(`/dashboards/${dashboardId}/widgets/reorder`, { positions });
  }

  // ── Analytics ──
  runQuery(spec: AggregationSpec) { return this.post<QueryResult>('/query', spec); }
  runWidgetQuery(dashboardId: string, widgetId: string, from?: string, to?: string) {
    return this.get<QueryResult>(`/dashboards/${dashboardId}/widgets/${widgetId}/query`, { from, to });
  }
  exportWidgetUrl(dashboardId: string, widgetId: string, from?: string, to?: string): string {
    const qs = new URLSearchParams();
    if (from) qs.set('from', from);
    if (to) qs.set('to', to);
    const suffix = qs.toString() ? `?${qs}` : '';
    return `${API_URL}/dashboards/${dashboardId}/widgets/${widgetId}/export${suffix}`;
  }

  /** Downloads a widget's CSV through the auth interceptor (the bearer token can't ride a plain link). */
  async exportWidget(dashboardId: string, widgetId: string, from?: string, to?: string): Promise<{ blob: Blob; filename: string }> {
    const resp = await firstValueFrom(this.http.get(this.exportWidgetUrl(dashboardId, widgetId, from, to),
      { observe: 'response', responseType: 'blob' }));
    const cd = resp.headers.get('content-disposition') ?? '';
    const match = /filename=([^;]+)/i.exec(cd);
    return { blob: resp.body!, filename: match ? match[1].trim().replace(/"/g, '') : 'report.csv' };
  }
}
