export type Lang = 'en' | 'es';

/** Per-language label maps for enum-ish values coming from the API. */
export const ROLE_LABEL: Record<Lang, Record<string, string>> = {
  en: { Owner: 'Owner', Editor: 'Editor', Viewer: 'Viewer', Admin: 'Admin', Member: 'Member' },
  es: { Owner: 'Propietario', Editor: 'Editor', Viewer: 'Lector', Admin: 'Administrador', Member: 'Miembro' },
};

export const WIDGET_LABEL: Record<Lang, Record<string, string>> = {
  en: { Kpi: 'KPI', Line: 'Line', Bar: 'Bar', Donut: 'Donut', Heatmap: 'Heatmap', Table: 'Table' },
  es: { Kpi: 'KPI', Line: 'Línea', Bar: 'Barras', Donut: 'Dona', Heatmap: 'Mapa de calor', Table: 'Tabla' },
};

export const AGG_LABEL: Record<Lang, Record<string, string>> = {
  en: { Sum: 'Sum', Avg: 'Average', Count: 'Count', CountDistinct: 'Distinct count', Min: 'Min', Max: 'Max' },
  es: { Sum: 'Suma', Avg: 'Promedio', Count: 'Conteo', CountDistinct: 'Conteo distinto', Min: 'Mínimo', Max: 'Máximo' },
};

export const TYPE_LABEL: Record<Lang, Record<string, string>> = {
  en: { String: 'Text', Number: 'Number', Date: 'Date', Boolean: 'Boolean' },
  es: { String: 'Texto', Number: 'Número', Date: 'Fecha', Boolean: 'Booleano' },
};

export const STATUS_LABEL: Record<Lang, Record<string, string>> = {
  en: { Processing: 'Processing', Ready: 'Ready', Failed: 'Failed' },
  es: { Processing: 'Procesando', Ready: 'Listo', Failed: 'Falló' },
};

export interface Copy {
  brand: string; brandSub: string;
  nav: { dashboards: string; datasets: string; about: string; signIn: string; signOut: string };
  common: {
    loading: string; back: string; cancel: string; save: string; create: string; delete: string;
    search: string; retry: string; close: string; empty: string; confirm: string; export: string;
    rows: string; columns: string; updated: string; all: string; apply: string; reset: string;
  };
  auth: {
    title: string; subtitle: string; email: string; password: string; displayName: string;
    login: string; register: string; noAccount: string; haveAccount: string; createAccount: string;
    demoAccounts: string; useAccount: string; ownerDemo: string; editorDemo: string; viewerDemo: string;
    signingIn: string;
  };
  dashboards: {
    title: string; subtitle: string; empty: string; emptyCta: string; newDashboard: string;
    widgets: string; openBuilder: string; createTitle: string; nameLabel: string; descLabel: string;
    datasetLabel: string; namePlaceholder: string; descPlaceholder: string; deleteConfirm: string;
  };
  datasets: { title: string; subtitle: string; empty: string; viewTable: string; rowsCols: string };
  about: {
    title: string; lead: string;
    howTitle: string;
    ingestTitle: string; ingestBody: string;
    orchestrateTitle: string; orchestrateBody: string;
    visualizeTitle: string; visualizeBody: string;
    featuresTitle: string; features: string[];
    stackTitle: string; demoTitle: string; demoBody: string;
  };
}

export const COPY: Record<Lang, Copy> = {
  en: {
    brand: 'PulseBoard',
    brandSub: 'analytics command center',
    nav: { dashboards: 'Dashboards', datasets: 'Datasets', about: 'About', signIn: 'Sign in', signOut: 'Sign out' },
    common: {
      loading: 'Loading…', back: 'Back', cancel: 'Cancel', save: 'Save', create: 'Create', delete: 'Delete',
      search: 'Search', retry: 'Retry', close: 'Close', empty: 'Nothing here yet.', confirm: 'Confirm', export: 'Export',
      rows: 'rows', columns: 'columns', updated: 'Updated', all: 'All', apply: 'Apply', reset: 'Reset',
    },
    auth: {
      title: 'Sign in to PulseBoard', subtitle: 'Build dashboards over your data.',
      email: 'Email', password: 'Password', displayName: 'Display name',
      login: 'Sign in', register: 'Create account', noAccount: 'No account?', haveAccount: 'Already have an account?',
      createAccount: 'Create one', demoAccounts: 'Demo accounts', useAccount: 'Use',
      ownerDemo: 'Owner — full control', editorDemo: 'Editor — build widgets', viewerDemo: 'Viewer — read only',
      signingIn: 'Signing in…',
    },
    dashboards: {
      title: 'Dashboards', subtitle: 'Configurable boards over your datasets.',
      empty: 'No dashboards yet.', emptyCta: 'Create your first dashboard', newDashboard: 'New dashboard',
      widgets: 'widgets', openBuilder: 'Open builder', createTitle: 'New dashboard',
      nameLabel: 'Name', descLabel: 'Description', datasetLabel: 'Dataset',
      namePlaceholder: 'e.g. Product revenue · Q3', descPlaceholder: 'What this board is for (optional)',
      deleteConfirm: 'Delete this dashboard and all its widgets?',
    },
    datasets: {
      title: 'Datasets', subtitle: 'Sources powering your dashboards.',
      empty: 'No datasets yet.', viewTable: 'Explore', rowsCols: 'rows · columns',
    },
    about: {
      title: 'About PulseBoard',
      lead: 'A configurable analytics platform built as a microservice architecture: a Python ETL service ingests CSVs, a .NET API orchestrates and aggregates, and an Angular front end renders drag-and-drop dashboards.',
      howTitle: 'How it works',
      ingestTitle: '1 · Ingest',
      ingestBody: 'A Python (FastAPI + pandas) microservice parses uploaded CSVs, infers column types, profiles them and writes the rows into PostgreSQL as JSONB.',
      orchestrateTitle: '2 · Orchestrate',
      orchestrateBody: 'A .NET 9 API (Clean Architecture) owns auth, dashboards and widgets, orchestrates the ETL, and runs aggregation queries over the JSONB rows with filters, date ranges and drill-down.',
      visualizeTitle: '3 · Visualize',
      visualizeBody: 'An Angular 20 front end renders the results as live ApexCharts widgets on a drag-and-drop grid, with a builder, dataset table and CSV export.',
      featuresTitle: 'Highlights',
      features: [
        'Configurable dashboards with a 6-type widget palette (KPI, line, bar, donut, heatmap, table)',
        'Drag-and-drop builder with a live preview driven by the real aggregation endpoint',
        'A JSONB aggregation engine: group-by, top-N, date granularity, filters and a 2-axis heatmap',
        'Per-dashboard RBAC (Owner / Editor / Viewer) on top of JWT auth with rotating refresh tokens',
        'CSV upload orchestrated end-to-end through the Python ETL microservice',
        'Date-range filtering, drill-down and CSV report export',
      ],
      stackTitle: 'Stack',
      demoTitle: 'Try it',
      demoBody: 'Sign in with a demo account — owner, editor or viewer — to explore each role.',
    },
  },
  es: {
    brand: 'PulseBoard',
    brandSub: 'centro de mando analítico',
    nav: { dashboards: 'Tableros', datasets: 'Datasets', about: 'Acerca de', signIn: 'Entrar', signOut: 'Salir' },
    common: {
      loading: 'Cargando…', back: 'Atrás', cancel: 'Cancelar', save: 'Guardar', create: 'Crear', delete: 'Eliminar',
      search: 'Buscar', retry: 'Reintentar', close: 'Cerrar', empty: 'Aún no hay nada aquí.', confirm: 'Confirmar', export: 'Exportar',
      rows: 'filas', columns: 'columnas', updated: 'Actualizado', all: 'Todos', apply: 'Aplicar', reset: 'Limpiar',
    },
    auth: {
      title: 'Entra a PulseBoard', subtitle: 'Crea tableros sobre tus datos.',
      email: 'Correo', password: 'Contraseña', displayName: 'Nombre visible',
      login: 'Entrar', register: 'Crear cuenta', noAccount: '¿Sin cuenta?', haveAccount: '¿Ya tienes cuenta?',
      createAccount: 'Crea una', demoAccounts: 'Cuentas de demo', useAccount: 'Usar',
      ownerDemo: 'Propietario — control total', editorDemo: 'Editor — crea widgets', viewerDemo: 'Lector — solo lectura',
      signingIn: 'Entrando…',
    },
    dashboards: {
      title: 'Tableros', subtitle: 'Tableros configurables sobre tus datasets.',
      empty: 'Aún no hay tableros.', emptyCta: 'Crea tu primer tablero', newDashboard: 'Nuevo tablero',
      widgets: 'widgets', openBuilder: 'Abrir constructor', createTitle: 'Nuevo tablero',
      nameLabel: 'Nombre', descLabel: 'Descripción', datasetLabel: 'Dataset',
      namePlaceholder: 'p. ej. Ingresos del producto · Q3', descPlaceholder: 'Para qué sirve este tablero (opcional)',
      deleteConfirm: '¿Eliminar este tablero y todos sus widgets?',
    },
    datasets: {
      title: 'Datasets', subtitle: 'Fuentes que alimentan tus tableros.',
      empty: 'Aún no hay datasets.', viewTable: 'Explorar', rowsCols: 'filas · columnas',
    },
    about: {
      title: 'Acerca de PulseBoard',
      lead: 'Una plataforma de analítica configurable construida como arquitectura de microservicios: un servicio ETL en Python ingiere CSVs, una API .NET orquesta y agrega, y un front Angular renderiza tableros con arrastrar y soltar.',
      howTitle: 'Cómo funciona',
      ingestTitle: '1 · Ingesta',
      ingestBody: 'Un microservicio Python (FastAPI + pandas) parsea los CSV subidos, infiere el tipo de cada columna, las perfila y escribe las filas en PostgreSQL como JSONB.',
      orchestrateTitle: '2 · Orquestación',
      orchestrateBody: 'Una API .NET 9 (Clean Architecture) gestiona auth, tableros y widgets, orquesta el ETL y ejecuta consultas de agregación sobre el JSONB con filtros, rangos de fecha y drill-down.',
      visualizeTitle: '3 · Visualización',
      visualizeBody: 'Un front Angular 20 renderiza los resultados como widgets ApexCharts en vivo sobre una grilla con arrastrar y soltar, con constructor, tabla de dataset y export a CSV.',
      featuresTitle: 'Destacados',
      features: [
        'Tableros configurables con paleta de 6 tipos de widget (KPI, línea, barras, dona, heatmap, tabla)',
        'Constructor drag & drop con preview en vivo usando el endpoint de agregación real',
        'Motor de agregación sobre JSONB: group-by, top-N, granularidad de fecha, filtros y heatmap de 2 ejes',
        'RBAC por tablero (Propietario / Editor / Lector) sobre auth JWT con refresh rotativo',
        'Subida de CSV orquestada de extremo a extremo con el microservicio ETL en Python',
        'Filtro por rango de fecha, drill-down y export de reportes en CSV',
      ],
      stackTitle: 'Stack',
      demoTitle: 'Pruébalo',
      demoBody: 'Entra con una cuenta de demo — propietario, editor o lector — para explorar cada rol.',
    },
  },
};
