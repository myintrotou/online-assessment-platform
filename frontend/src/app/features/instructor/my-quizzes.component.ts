import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { apiErrorMessage } from '../../core/auth.interceptor';
import { QuizListItem } from '../../core/models';

@Component({
  selector: 'app-my-quizzes',
  imports: [RouterLink],
  template: `
    <div class="container">
      <div class="between" style="margin-bottom: 18px">
        <div>
          <h1>My quizzes</h1>
          <p class="muted">Create quizzes, draft questions with AI, and track results.</p>
        </div>
        <a class="btn" routerLink="/teach/new">New quiz</a>
      </div>

      @if (loading()) {
        <div class="loading"><span class="spinner"></span> Loading…</div>
      } @else if (error()) {
        <div class="alert">{{ error() }}</div>
      } @else if (quizzes().length === 0) {
        <div class="empty">
          No quizzes yet. <a routerLink="/teach/new">Create your first one.</a>
        </div>
      } @else {
        <div class="stack">
          @for (q of quizzes(); track q.id) {
            <div class="card between">
              <div class="grow">
                <div class="row" style="gap: 8px">
                  <h3 style="margin: 0">{{ q.title }}</h3>
                  @if (q.isPublished) {
                    <span class="badge badge--good">Published</span>
                  } @else {
                    <span class="badge badge--warn">Draft</span>
                  }
                </div>
                <div class="small muted" style="margin-top: 3px">
                  {{ q.subject }} · {{ q.questionCount }} questions · {{ q.attemptCount }} attempts
                </div>
              </div>
              <div class="row">
                <a class="btn btn--ghost btn--sm" [routerLink]="['/teach', q.id]">Edit</a>
                <a class="btn btn--subtle btn--sm" [routerLink]="['/teach', q.id, 'results']">Results</a>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class MyQuizzesComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly quizzes = signal<QuizListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.api.myQuizzes().subscribe({
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
