import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { ApexOptions } from 'ng-apexcharts';
import { CHART_COLORS } from '../core/chart-theme';
import { compact, formatKpi } from '../core/format';
import { LanguageService } from '../core/language.service';
import { QueryResult, WidgetWithData } from '../core/models';
import { ChartComponent } from './chart.component';

/**
 * Renders one widget's computed data as the right ApexChart (or a KPI number / table). Shared by the
 * dashboard view and the builder's live preview, so the two always look identical.
 */
@Component({
  selector: 'pb-widget-card',
  standalone: true,
  imports: [ChartComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @let w = widget();
    @if (w.error) {
      <p class="text-xs text-rose-400">{{ w.error }}</p>
    } @else if (w.data; as d) {
      @switch (w.widget.type) {
        @case ('Kpi') {
          <p class="num text-3xl font-bold text-white leading-tight">{{ kpi() }}</p>
          <p class="text-[11px] text-gr-400 mt-1">{{ lang.agg(w.widget.aggregation) }}<!--
            -->{{ w.widget.metricColumn ? ' · ' + w.widget.metricColumn : '' }}</p>
        }
        @case ('Table') {
          <div class="overflow-auto max-h-72 -mx-1">
            <table class="w-full text-xs">
              <tbody>
                @for (p of (d.points || []); track p.key) {
                  <tr class="border-b border-gr-800 last:border-0">
                    <td class="py-1.5 px-1 text-gr-200 truncate max-w-0">{{ p.key }}</td>
                    <td class="py-1.5 px-1 text-right num text-cy-300">{{ fmt(p.value) }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
        @default {
          @if (hasData()) {
            <pb-chart [options]="options()" />
          } @else {
            <p class="text-xs text-gr-400 py-8 text-center">{{ lang.t().common.empty }}</p>
          }
        }
      }
    }
  `,
})
export class WidgetCardComponent {
  readonly widget = input.required<WidgetWithData>();
  protected readonly lang = inject(LanguageService);

  protected readonly kpi = computed(() => {
    const w = this.widget();
    return formatKpi(w.data?.scalar ?? null, w.widget.aggregation, this.lang.dateLocale());
  });

  protected readonly hasData = computed(() => {
    const d = this.widget().data;
    return (d?.points?.length ?? 0) > 0 || (d?.matrix?.length ?? 0) > 0;
  });

  protected fmt = (v: number) => compact(v, this.lang.dateLocale());

  protected readonly options = computed<Partial<ApexOptions>>(() => {
    const w = this.widget();
    const data = w.data;
    if (!data) return {};
    switch (w.widget.type) {
      case 'Line': return this.lineOptions(data);
      case 'Bar': return this.barOptions(data);
      case 'Donut': return this.donutOptions(data);
      case 'Heatmap': return this.heatmapOptions(data);
      default: return {};
    }
  });

  private lineOptions(d: QueryResult): Partial<ApexOptions> {
    const points = d.points ?? [];
    return {
      chart: { type: 'area', height: 240, sparkline: { enabled: false } },
      series: [{ name: this.widget().widget.title, data: points.map(p => round(p.value)) }],
      xaxis: { categories: points.map(p => p.key), tickAmount: 6 },
      fill: { type: 'gradient', gradient: { shadeIntensity: 0.4, opacityFrom: 0.35, opacityTo: 0.02 } },
      stroke: { curve: 'smooth', width: 2 },
    };
  }

  private barOptions(d: QueryResult): Partial<ApexOptions> {
    const points = d.points ?? [];
    return {
      chart: { type: 'bar', height: 240 },
      series: [{ name: this.widget().widget.title, data: points.map(p => round(p.value)) }],
      xaxis: { categories: points.map(p => p.key) },
      plotOptions: { bar: { horizontal: true, borderRadius: 3, distributed: true, barHeight: '70%' } },
      legend: { show: false },
      colors: CHART_COLORS,
    };
  }

  private donutOptions(d: QueryResult): Partial<ApexOptions> {
    const points = d.points ?? [];
    return {
      chart: { type: 'donut', height: 260 },
      series: points.map(p => round(p.value)),
      labels: points.map(p => p.key),
      legend: { position: 'bottom' },
      stroke: { width: 0 },
      plotOptions: { pie: { donut: { size: '68%' } } },
    };
  }

  private heatmapOptions(d: QueryResult): Partial<ApexOptions> {
    const cells = d.matrix ?? [];
    // Group by the secondary axis (y) into one series per row, columns ordered by primary axis (x).
    const xs = [...new Set(cells.map(c => c.x))];
    const ys = [...new Set(cells.map(c => c.y))].sort((a, b) => Number(a) - Number(b) || a.localeCompare(b));
    const byKey = new Map(cells.map(c => [`${c.x}|${c.y}`, c.value]));
    const series = ys.map(y => ({
      name: y,
      data: xs.map(x => ({ x, y: round(byKey.get(`${x}|${y}`) ?? 0) })),
    }));
    return {
      chart: { type: 'heatmap', height: 260 },
      series,
      colors: ['#22d3ee'],
      plotOptions: {
        heatmap: { radius: 2, enableShades: true, shadeIntensity: 0.7, useFillColorAsStroke: false },
      },
      stroke: { width: 2, colors: ['#101318'] },
      legend: { show: false },
    };
  }
}

function round(v: number): number {
  return Math.round(v * 100) / 100;
}
