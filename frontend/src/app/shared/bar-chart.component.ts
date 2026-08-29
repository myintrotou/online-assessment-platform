import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  ViewChild,
} from '@angular/core';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-bar-chart',
  template: `<canvas #canvas></canvas>`,
  styles: [
    `
      :host {
        display: block;
        position: relative;
        height: var(--chart-height, 280px);
      }
      canvas {
        max-width: 100%;
      }
    `,
  ],
})
export class BarChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() labels: string[] = [];
  @Input() values: number[] = [];
  @Input() label = 'Value';
  @Input() suffix = '';

  @ViewChild('canvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

  private chart?: Chart;
  private viewReady = false;

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.render();
  }

  ngOnChanges(): void {
    if (this.viewReady) this.render();
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private render(): void {
    this.chart?.destroy();
    const style = getComputedStyle(document.documentElement);
    const primary = style.getPropertyValue('--primary').trim() || '#5a49d6';
    const grid = style.getPropertyValue('--border').trim() || '#e7e5ee';
    const text = style.getPropertyValue('--muted').trim() || '#706b80';

    this.chart = new Chart(this.canvasRef.nativeElement, {
      type: 'bar',
      data: {
        labels: this.labels,
        datasets: [
          {
            label: this.label,
            data: this.values,
            backgroundColor: primary,
            borderRadius: 6,
            maxBarThickness: 46,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx) => ` ${ctx.parsed.y}${this.suffix}`,
            },
          },
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: { color: text, font: { size: 11 } },
          },
          y: {
            beginAtZero: true,
            grid: { color: grid },
            ticks: { color: text, font: { size: 11 }, callback: (v) => `${v}${this.suffix}` },
          },
        },
      },
    });
  }
}
