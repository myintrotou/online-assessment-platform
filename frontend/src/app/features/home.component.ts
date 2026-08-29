import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  template: `
    <div class="container container--narrow">
      <div class="card card--pad-lg stack">
        <div>
          <h1>Welcome, {{ auth.user()?.fullName }}</h1>
          <p class="muted">
            @if (auth.isInstructor()) {
              Build quizzes, draft questions with AI, and see how your students are doing.
            } @else {
              Take a quiz, then use the AI helper to understand what you missed.
            }
          </p>
        </div>

        <div class="stack">
          @if (auth.isInstructor()) {
            <a class="btn btn--block" routerLink="/teach">Go to my quizzes</a>
            <a class="btn btn--ghost btn--block" routerLink="/teach/new">Create a new quiz</a>
          } @else {
            <a class="btn btn--block" routerLink="/quizzes">Browse quizzes</a>
            <a class="btn btn--ghost btn--block" routerLink="/history">My attempt history</a>
          }
        </div>
      </div>
    </div>
  `,
})
export class HomeComponent {
  readonly auth = inject(AuthService);
}
