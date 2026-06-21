import { ApexOptions } from 'ng-apexcharts';

/** Command-center palette — data is the only color on the graphite canvas. */
export const CHART_COLORS = ['#22d3ee', '#a78bfa', '#a3e635', '#fb7185', '#fbbf24', '#67e8f9'];
const GRID = '#1f2530';
const TEXT = '#6e7c94';
const MONO = "'JetBrains Mono', ui-monospace, monospace";

/** Shared ApexCharts defaults so every chart inherits the dark theme, mono numerals and no chrome. */
export function baseChartOptions(): Partial<ApexOptions> {
  return {
    chart: {
      type: 'line', // sensible default; every concrete chart overrides this
      fontFamily: "'Inter', sans-serif",
      foreColor: TEXT,
      toolbar: { show: false },
      zoom: { enabled: false },
      animations: { enabled: true, speed: 400, animateGradually: { enabled: false } },
      background: 'transparent',
    },
    colors: CHART_COLORS,
    grid: { borderColor: GRID, strokeDashArray: 0, padding: { left: 8, right: 8 } },
    dataLabels: { enabled: false },
    tooltip: { theme: 'dark', style: { fontFamily: MONO } },
    legend: { labels: { colors: TEXT }, fontSize: '12px', markers: { strokeWidth: 0 } },
    xaxis: { axisBorder: { color: GRID }, axisTicks: { color: GRID }, labels: { style: { fontFamily: MONO, fontSize: '11px' } } },
    yaxis: { labels: { style: { fontFamily: MONO, fontSize: '11px' } } },
    stroke: { curve: 'smooth', width: 2 },
  };
}
