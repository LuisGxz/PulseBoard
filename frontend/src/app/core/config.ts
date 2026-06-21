declare global {
  interface Window { PULSEBOARD_API_BASE?: string; }
}

// Overridable at deploy time (GitHub Pages injects window.PULSEBOARD_API_BASE in index.html).
export const API_BASE = window.PULSEBOARD_API_BASE ?? 'http://localhost:5180';
export const API_URL = `${API_BASE}/api`;
