import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { apiErrorMessage } from '../../core/auth.interceptor';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="container container--narrow">
      <div class="card card--pad-lg stack">
        <div>
          <h1>Sign in</h1>
          <p class="muted">Use your account, or one of the demo logins below.</p>
        </div>

        @if (error()) {
          <div class="alert">{{ error() }}</div>
        }

        <form class="stack" (ngSubmit)="submit()">
          <div class="field">
            <label for="email">Email</label>
            <input id="email" type="email" name="email" [(ngModel)]="email" required autocomplete="email" />
          </div>
          <div class="field">
            <label for="password">Password</label>
            <input
              id="password"
              type="password"
              name="password"
              [(ngModel)]="password"
              required
              autocomplete="current-password"
            />
          </div>
          <button class="btn btn--block" type="submit" [disabled]="loading()">
            @if (loading()) {
              <span class="spinner"></span> Signing in…
            } @else {
              Sign in
            }
          </button>
        </form>

        <hr class="divider" />

        <div class="stack small">
          <div class="between">
            <span class="muted">Demo instructor</span>
            <button class="btn btn--subtle btn--sm" (click)="fill('instructor@demo.com')">Use</button>
          </div>
          <div class="between">
            <span class="muted">Demo student</span>
            <button class="btn btn--subtle btn--sm" (click)="fill('student@demo.com')">Use</button>
          </div>
          <p class="faint">Password for both: <span class="mono">Password123!</span></p>
        </div>

        <p class="small muted">No account? <a routerLink="/register">Create one</a></p>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  email = '';
  password = '';
  readonly loading = signal(false);
  readonly error = signal('');

  fill(email: string): void {
    this.email = email;
    this.password = 'Password123!';
  }

  submit(): void {
    if (!this.email || !this.password) return;
    this.loading.set(true);
    this.error.set('');
    this.auth.login(this.email.trim(), this.password).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.loading.set(false);
      },
    });
  }
}
