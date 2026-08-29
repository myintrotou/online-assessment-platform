import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { apiErrorMessage } from '../../core/auth.interceptor';
import { PublishedQuizItem } from '../../core/models';

@Component({
  selector: 'app-quiz-list',
  imports: [RouterLink],
  template: `
    <div class="container">
      <div class="between" style="margin-bottom: 18px">
        <div>
          <h1>Quizzes</h1>
          <p class="muted">Pick one to attempt. Timed once you start.</p>
        </div>
      </div>

      @if (loading()) {
        <div class="loading"><span class="spinner"></span> Loading quizzes…</div>
      } @else if (error()) {
        <div class="alert">{{ error() }}</div>
      } @else if (quizzes().length === 0) {
        <div class="empty">No published quizzes yet. Check back soon.</div>
      } @else {
        <div class="grid">
          @for (q of quizzes(); track q.id) {
            <div class="card stack">
              <div class="between">
                <span class="badge">{{ q.subject }}</span>
                @if (q.myBestScore != null) {
                  <span class="badge badge--good">Best {{ q.myBestScore }}%</span>
                }
              </div>
              <div>
                <h3>{{ q.title }}</h3>
                @if (q.description) {
                  <p class="small muted">{{ q.description }}</p>
                }
              </div>
              <div class="row small muted">
                <span>{{ q.questionCount }} questions</span>
                <span>·</span>
                <span>{{ q.timeLimitMinutes ? q.timeLimitMinutes + ' min' : 'Untimed' }}</span>
                @if (q.myAttempts > 0) {
                  <span>·</span>
                  <span>{{ q.myAttempts }} attempt{{ q.myAttempts === 1 ? '' : 's' }}</span>
                }
              </div>
              <a class="btn btn--block" [routerLink]="['/quizzes', q.id, 'take']">
                {{ q.myAttempts > 0 ? 'Retake' : 'Start' }}
              </a>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [
    `
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
        gap: 16px;
      }
    `,
  ],
})
export class QuizListComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly quizzes = signal<PublishedQuizItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.api.publishedQuizzes().subscribe({
      next: (list) => {
        this.quizzes.set(list);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.loading.set(false);
      },
    });
  }
}
