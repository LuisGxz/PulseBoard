// ── Auth ─────────────────────────────────────────────────────────────────────
export type AppRole = 'Admin' | 'Member';
export type DashboardRole = 'Owner' | 'Editor' | 'Viewer';

export interface UserDto { id: string; email: string; displayName: string; role: AppRole; }
export interface AuthResponse { accessToken: string; refreshToken: string; expiresInSeconds: number; user: UserDto; }

// ── Datasets ──────────────────────────────────────────────────────────────────
export type ColumnType = 'String' | 'Number' | 'Date' | 'Boolean';
export type DatasetStatus = 'Processing' | 'Ready' | 'Failed';

export interface DatasetSummary {
  id: string; name: string; slug: string; description: string;
  status: DatasetStatus; rowCount: number; columnCount: number; updatedAt: string;
}
export interface DatasetColumn {
  name: string; label: string; type: ColumnType; position: number;
  nullCount: number; distinctCount: number; minNumeric: number | null; maxNumeric: number | null;
  sampleValues: string[];
}
export interface DatasetDetail {
  id: string; name: string; slug: string; description: string;
  status: DatasetStatus; rowCount: number; columns: DatasetColumn[];
}
export interface DatasetRows {
  total: number; page: number; pageSize: number;
  columns: DatasetColumn[]; rows: Record<string, unknown>[];
}

// ── Widgets / aggregation ───────────────────────────────────────────────────────
export type WidgetType = 'Kpi' | 'Line' | 'Bar' | 'Donut' | 'Heatmap' | 'Table';
export type Aggregation = 'Sum' | 'Avg' | 'Count' | 'CountDistinct' | 'Min' | 'Max';

export interface WidgetDto {
  id: string; type: WidgetType; title: string; position: number;
  gridX: number; gridY: number; gridW: number; gridH: number;
  metricColumn: string | null; aggregation: Aggregation;
  dimensionColumn: string | null; secondaryDimensionColumn: string | null;
  dateGranularity: string | null; limit: number | null; filtersJson: string | null;
}

export interface SeriesPoint { key: string; value: number; }
export interface MatrixCell { x: string; y: string; value: number; }
export interface QueryResult {
  kind: 'scalar' | 'series' | 'matrix';
  scalar: number | null;
  points: SeriesPoint[] | null;
  matrix: MatrixCell[] | null;
}
export interface WidgetWithData { widget: WidgetDto; data: QueryResult | null; error: string | null; }

export interface AggregationSpec {
  datasetId: string;
  metricColumn?: string | null;
  aggregation: Aggregation;
  dimensionColumn?: string | null;
  secondaryDimensionColumn?: string | null;
  dateGranularity?: string | null;
  limit?: number | null;
  filtersJson?: string | null;
  dateColumn?: string | null;
  from?: string | null;
  to?: string | null;
}

export interface SaveWidgetRequest {
  type: WidgetType; title: string;
  gridX: number; gridY: number; gridW: number; gridH: number;
  metricColumn?: string | null; aggregation: Aggregation;
  dimensionColumn?: string | null; secondaryDimensionColumn?: string | null;
  dateGranularity?: string | null; limit?: number | null; filtersJson?: string | null;
}

// ── Dashboards ───────────────────────────────────────────────────────────────────
export interface DashboardSummary {
  id: string; name: string; slug: string; description: string;
  datasetId: string; datasetName: string; role: DashboardRole; widgetCount: number; updatedAt: string;
}
export interface DashboardMember { userId: string; email: string; displayName: string; role: DashboardRole; }
export interface DashboardDetail {
  id: string; name: string; slug: string; description: string;
  datasetId: string; datasetName: string; role: DashboardRole;
  widgets: WidgetWithData[]; members: DashboardMember[];
}
export interface SaveDashboardRequest { name: string; description: string; datasetId: string; }

// ── Filters (shared widget/query filter clause) ─────────────────────────────────
export type FilterOp = 'eq' | 'ne' | 'gt' | 'gte' | 'lt' | 'lte' | 'contains' | 'in';
export interface FilterClause { column: string; op: FilterOp; value: string | string[]; }
