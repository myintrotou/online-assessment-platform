import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { apiErrorMessage } from '../../core/auth.interceptor';
import { UserRole } from '../../core/models';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="container container--narrow">
      <div class="card card--pad-lg stack">
        <div>
          <h1>Create an account</h1>
          <p class="muted">Pick a role — you can make separate accounts to try both sides.</p>
        </div>

        @if (error()) {
          <div class="alert">{{ error() }}</div>
        }

        <form class="stack" (ngSubmit)="submit()">
          <div class="field">
            <label for="name">Full name</label>
            <input id="name" name="name" type="text" [(ngModel)]="fullName" required />
          </div>
          <div class="field">
            <label for="email">Email</label>
            <input id="email" name="email" type="email" [(ngModel)]="email" required autocomplete="email" />
          </div>
          <div class="field">
            <label for="password">Password</label>
            <input
              id="password"
              name="password"
              type="password"
              [(ngModel)]="password"
              required
              minlength="8"
              autocomplete="new-password"
            />
            <span class="hint">At least 8 characters.</span>
          </div>
          <div class="field">
            <label for="role">I am a…</label>
            <select id="role" name="role" [(ngModel)]="role">
              <option value="Student">Student</option>
              <option value="Instructor">Instructor</option>
            </select>
          </div>
          <button class="btn btn--block" type="submit" [disabled]="loading()">
            @if (loading()) {
              <span class="spinner"></span> Creating…
            } @else {
              Create account
            }
          </button>
        </form>

        <p class="small muted">Already have one? <a routerLink="/login">Sign in</a></p>
      </div>
    </div>
  `,
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  fullName = '';
  email = '';
  password = '';
  role: UserRole = 'Student';
  readonly loading = signal(false);
  readonly error = signal('');

  submit(): void {
    if (!this.fullName || !this.email || this.password.length < 8) return;
    this.loading.set(true);
    this.error.set('');
    this.auth.register(this.email.trim(), this.fullName.trim(), this.password, this.role).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.loading.set(false);
      },
    });
  }
}
