import { Component, Input, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { apiErrorMessage } from '../../core/auth.interceptor';
import { QuizResultsSummary } from '../../core/models';
import { BarChartComponent } from '../../shared/bar-chart.component';

@Component({
  selector: 'app-quiz-results',
  imports: [RouterLink, BarChartComponent],
  template: `
    <div class="container">
      <a routerLink="/teach" class="small muted">← My quizzes</a>

      @if (loading()) {
        <div class="loading"><span class="spinner"></span> Loading results…</div>
      } @else if (error()) {
        <div class="alert">{{ error() }}</div>
      } @else if (data()) {
        @let d = data()!;
        <h1 style="margin-top: 8px">{{ d.title }} — results</h1>

        @if (d.attemptCount === 0) {
          <div class="empty">No submitted attempts yet.</div>
        } @else {
          <div class="row stats">
            <div class="stat">
              <div class="stat__value">{{ d.attemptCount }}</div>
              <div class="stat__label">Attempts</div>
            </div>
            <div class="stat">
              <div class="stat__value">{{ d.averageScore }}%</div>
              <div class="stat__label">Average score</div>
            </div>
            <div class="stat">
              <div class="stat__value">{{ hardestAccuracy() }}%</div>
              <div class="stat__label">Hardest question</div>
            </div>
          </div>

          <div class="card" style="margin: 16px 0">
            <h3>Accuracy by question</h3>
            <app-bar-chart
              [labels]="questionLabels()"
              [values]="questionValues()"
              label="Accuracy"
              suffix="%"
            />
          </div>

          @if (topicRows().length > 1) {
            <div class="card" style="margin-bottom: 16px">
              <h3>Accuracy by topic</h3>
              <app-bar-chart [labels]="topicLabels()" [values]="topicValues()" label="Accuracy" suffix="%" />
            </div>
          }

          <div class="card">
            <h3>Attempts</h3>
            <div class="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Student</th>
                    <th>Score</th>
                    <th>Correct</th>
                    <th>Submitted</th>
                  </tr>
                </thead>
                <tbody>
                  @for (a of d.attempts; track a.attemptId) {
                    <tr>
                      <td>{{ a.studentName }}</td>
                      <td>
                        <span class="badge" [class.badge--good]="a.scorePercent >= 50" [class.badge--bad]="a.scorePercent < 50">
                          {{ a.scorePercent }}%
                        </span>
                      </td>
                      <td class="mono">{{ a.correctCount }} / {{ a.totalQuestions }}</td>
                      <td class="small muted">{{ formatDate(a.submittedAt) }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        }
      }
    </div>
  `,
  styles: [
    `
      .stats {
        gap: 12px;
        margin-top: 16px;
      }
      .stat {
        flex: 1;
        min-width: 120px;
        background: var(--surface);
        border: 1px solid var(--border);
        border-radius: var(--radius);
        padding: 14px 16px;
      }
      .stat__value {
        font-size: 1.6rem;
        font-weight: 750;
        color: var(--ink);
        line-height: 1;
      }
      .stat__label {
        font-size: 0.8rem;
        color: var(--muted);
        margin-top: 5px;
      }
      .table-wrap {
        overflow-x: auto;
      }
      table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.9rem;
      }
      th {
        text-align: left;
        font-size: 0.78rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--muted);
        padding: 8px 10px;
        border-bottom: 1px solid var(--border);
      }
      td {
        padding: 9px 10px;
        border-bottom: 1px solid var(--border);
      }
      tr:last-child td {
        border-bottom: 0;
      }
    `,
  ],
})
export class QuizResultsComponent implements OnInit {
  @Input() id!: string;

  private readonly api = inject(ApiService);

  readonly data = signal<QuizResultsSummary | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  readonly questionLabels = computed(() =>
    (this.data()?.questionBreakdown ?? []).map((_, i) => `Q${i + 1}`),
  );
  readonly questionValues = computed(() =>
    (this.data()?.questionBreakdown ?? []).map((q) => q.accuracyPercent),
  );
  readonly hardestAccuracy = computed(() => {
    const vals = this.questionValues();
    return vals.length ? Math.min(...vals) : 0;
  });

  readonly topicRows = computed(() => {
    const map = new Map<string, { correct: number; answered: number }>();
    for (const q of this.data()?.questionBreakdown ?? []) {
      const key = q.topic || 'General';
      const cur = map.get(key) ?? { correct: 0, answered: 0 };
      cur.correct += q.correct;
      cur.answered += q.answered;
      map.set(key, cur);
    }
    return [...map.entries()].map(([topic, v]) => ({
      topic,
      accuracy: v.answered ? Math.round((v.correct / v.answered) * 100) : 0,
    }));
  });
  readonly topicLabels = computed(() => this.topicRows().map((r) => r.topic));
  readonly topicValues = computed(() => this.topicRows().map((r) => r.accuracy));

  ngOnInit(): void {
    this.api.results(this.id).subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.loading.set(false);
      },
    });
  }

  formatDate(iso?: string): string {
    return iso ? new Date(iso).toLocaleString() : '—';
  }
}
