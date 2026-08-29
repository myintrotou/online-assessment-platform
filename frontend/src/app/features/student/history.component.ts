import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { apiErrorMessage } from '../../core/auth.interceptor';
import { AttemptHistoryItem } from '../../core/models';

@Component({
  selector: 'app-history',
  imports: [RouterLink],
  template: `
    <div class="container">
      <h1>Attempt history</h1>
      <p class="muted">Every attempt is kept. Open one to revise it with the AI helper.</p>

      @if (loading()) {
        <div class="loading"><span class="spinner"></span> Loading…</div>
      } @else if (error()) {
        <div class="alert">{{ error() }}</div>
      } @else if (items().length === 0) {
        <div class="empty">
          No attempts yet. <a routerLink="/quizzes">Browse quizzes</a> to get started.
        </div>
      } @else {
        <div class="stack" style="margin-top: 14px">
          @for (a of items(); track a.attemptId) {
            <a class="card row hist" [routerLink]="['/attempts', a.attemptId]">
              <div class="grow">
                <h3 style="margin: 0 0 2px">{{ a.quizTitle }}</h3>
                <div class="small muted">
                  {{ a.subject }} · {{ formatDate(a.submittedAt) }}
                </div>
              </div>
              <div class="hist__score" [class.hist__score--low]="a.scorePercent < 50">
                {{ a.scorePercent }}%
                <span class="small faint">{{ a.correctCount }}/{{ a.totalQuestions }}</span>
              </div>
            </a>
          }
        </div>
      }
    </div>
  `,
  styles: [
    `
      .hist {
        text-decoration: none;
        transition: box-shadow 0.12s ease, border-color 0.12s ease;
      }
      .hist:hover {
        text-decoration: none;
        border-color: var(--border-strong);
        box-shadow: var(--shadow-sm);
      }
      .hist__score {
        font-weight: 750;
        font-size: 1.1rem;
        color: var(--good);
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        line-height: 1.1;
      }
      .hist__score--low {
        color: var(--bad);
      }
    `,
  ],
})
export class HistoryComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly items = signal<AttemptHistoryItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.api.history().subscribe({
      next: (list) => {
        this.items.set(list);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.loading.set(false);
      },
    });
  }

  formatDate(iso?: string): string {
    return iso ? new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' }) : '—';
  }
}
