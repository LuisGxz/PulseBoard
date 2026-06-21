import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ApexOptions, NgApexchartsModule } from 'ng-apexcharts';
import { baseChartOptions } from '../core/chart-theme';

/**
 * Thin wrapper over ng-apexcharts that deep-merges per-chart options onto the shared command-center
 * theme. Feature components pass only what differs (series, chart type, labels).
 */
@Component({
  selector: 'pb-chart',
  standalone: true,
  imports: [NgApexchartsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <apx-chart
      [series]="merged().series!"
      [chart]="merged().chart!"
      [colors]="merged().colors!"
      [grid]="merged().grid!"
      [dataLabels]="merged().dataLabels!"
      [tooltip]="merged().tooltip!"
      [legend]="merged().legend!"
      [xaxis]="merged().xaxis!"
      [yaxis]="merged().yaxis!"
      [stroke]="merged().stroke!"
      [plotOptions]="merged().plotOptions!"
      [labels]="merged().labels!"
      [fill]="merged().fill!"
      [states]="merged().states!"></apx-chart>
  `,
})
export class ChartComponent {
  readonly options = input.required<Partial<ApexOptions>>();

  protected readonly merged = computed<Partial<ApexOptions>>(() => {
    const base = baseChartOptions();
    const o = this.options();
    return {
      ...base,
      ...o,
      chart: { ...base.chart, ...o.chart } as ApexOptions['chart'],
      grid: { ...base.grid, ...o.grid },
      tooltip: { ...base.tooltip, ...o.tooltip },
      legend: { ...base.legend, ...o.legend },
      xaxis: { ...base.xaxis, ...o.xaxis },
      yaxis: { ...base.yaxis, ...o.yaxis },
      stroke: { ...base.stroke, ...o.stroke },
      dataLabels: { ...base.dataLabels, ...o.dataLabels },
    };
  });
}
