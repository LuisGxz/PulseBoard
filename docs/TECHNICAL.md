# PulseBoard — technical notes

A deeper look at the design decisions behind PulseBoard. For setup and features see the
[README](../README.md).

## 1. Why a microservice split

The brief is "data / BI with a touch of Python". The honest way to show that is to let each runtime
do what it's best at:

- **Python (FastAPI + pandas)** owns ingestion. pandas is the right tool to parse arbitrary CSVs,
  infer column types and profile them.
- **.NET (Clean Architecture)** owns the transactional domain — auth, dashboards, widgets — and the
  aggregation queries that power the charts.
- They communicate over a small HTTP contract and **share one PostgreSQL database**. Sharing the DB
  (rather than shuttling thousands of rows back over HTTP) keeps ingestion a single write path and
  keeps the API's aggregation queries close to the data.

## 2. The analytics store: JSONB

Uploaded datasets have an unknown, per-file schema, so rows are stored schema-agnostically in
`dataset_rows.data` as **JSONB**, with the inferred schema/profile in `dataset_columns`. This trades
a little query ergonomics for the ability to ingest any CSV without DDL per dataset.

Values are normalized at ingest: numbers as JSON numbers, booleans as `true/false`, dates as ISO
`YYYY-MM-DD` strings (so they sort chronologically as text), everything else as strings.

## 3. The aggregation engine

`AggregationSqlBuilder` (Infrastructure) turns an `AggregationSpec` into **parameterized SQL** over
the JSONB. It's a pure function — `(spec, columnTypes) → (sql, params)` — so it's unit-tested without
a database (12 of the 36 backend tests cover it, including an injection attempt).

Key rules:

- **Casts by column type**: a numeric metric becomes `(data->>'col')::numeric`; a boolean becomes
  `((data->>'col')::boolean)::int::numeric` — which is what makes "average of `is_paid`" read as a
  conversion rate.
- **Dimensions**: a plain group-by is `data->>'col'`; with a granularity it becomes
  `to_char(date_trunc('month', (data->>'col')::date), 'YYYY-MM-DD')`.
- **Shape from dimensions, not chart type**: no dimension → `scalar` (KPI); one → `series`; two →
  `matrix` (heatmap). The chart type only drives rendering on the front end.
- **Ordering**: date/granularity series sort chronologically; categorical series sort by value
  desc and honor a top-N `LIMIT`.
- **Safety**: every interpolated identifier is validated against the dataset's known columns *and* a
  `^[a-z0-9_]+$` pattern; all values are parameters. Aggregation functions, operators and
  granularities are whitelists.

The same engine backs both saved widgets (built from a `Widget`) and the builder's ad-hoc preview
(`POST /api/query`), so the preview is exact.

## 4. Backend layering (Clean Architecture)

- **Domain** — entities + invariants (lockout policy on `User`, refresh-token rotation on
  `RefreshToken`). No framework dependencies.
- **Application** — use cases as MediatR handlers, FluentValidation validators, and the interfaces
  it depends on (`IAppDbContext`, `IAnalyticsQueryService`, `IEtlClient`, `IClock`, …).
- **Infrastructure** — EF Core `DbContext` (implements `IAppDbContext`), the SQL builder + Npgsql
  executor, JWT/BCrypt, the typed ETL `HttpClient`, the CSV exporter.
- **Api** — minimal-API endpoint groups, JWT bearer, a `GlobalExceptionHandler` that maps app
  exceptions to RFC 7807 ProblemDetails, rate limiting on auth.

## 5. Auth & RBAC

- JWT access tokens (15 min) + **rotating** refresh tokens (hashed at rest; rotation revokes the
  prior token and records its replacement for reuse detection).
- Account **lockout** after 5 failed logins for 15 minutes.
- Two-level authorization: a global `AppRole` (Admin/Member) and a per-dashboard `DashboardRole`
  (Owner > Editor > Viewer). `DashboardAuthorizationService` resolves the effective role (an Admin is
  implicitly Owner everywhere) and enforces a minimum role per operation.

## 6. The ETL contract

`POST /ingest` (multipart: `dataset_id` + `file`, header `X-ETL-Key`) — the API first creates the
dataset row as `Processing`, then calls the ETL, which parses/profiles the CSV, writes
`dataset_columns` + `dataset_rows` in one transaction, and flips the dataset to `Ready` (or `Failed`
with a message). `POST /preview` does the same parsing without persisting; `GET /health` for probes.
Enum-as-string values written by Python (`Ready`, `Number`, …) match the .NET enum member names
exactly, since EF maps those enums with `HasConversion<string>()`.

## 7. Front end

- Angular 20 standalone components with **signals**; lazy-loaded feature routes; a startup
  `appInitializer` restores the session so guarded views carry a token on reload.
- `pb-widget-card` maps a `QueryResult` to the right ApexChart; it's shared between the dashboard and
  the builder preview so they can't drift.
- Drag-and-drop reorder via Angular CDK; the order is persisted with one `PUT …/widgets/reorder`.
- Strict E2E (Playwright) walks the full journey and **asserts zero console errors**.

## 8. Testing

| Suite | Count | What it covers |
|-------|-------|----------------|
| Backend (xUnit) | 36 | SQL builder (incl. injection), auth/JWT, lockout, RBAC, CSV exporter |
| ETL (pytest) | 8 | CSV parse, type inference, profiling, slug collisions, limits |
| Front end (Playwright) | 5 | login → board charts → builder CRUD → dataset filter → i18n, 0 console errors |
