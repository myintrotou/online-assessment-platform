import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="topbar">
      <div class="topbar__inner">
        <a class="brand" routerLink="/">
          <span class="brand__mark">📝</span>
          <span>Assessment&nbsp;Platform</span>
        </a>

        @if (auth.isAuthenticated()) {
          <nav class="nav">
            @if (auth.isStudent()) {
              <a routerLink="/quizzes" routerLinkActive="is-active">Quizzes</a>
              <a routerLink="/history" routerLinkActive="is-active">History</a>
            }
            @if (auth.isInstructor()) {
              <a routerLink="/teach" routerLinkActive="is-active">My quizzes</a>
            }
          </nav>

          <div class="who">
            <span class="who__name">{{ auth.user()?.fullName }}</span>
            <span class="badge badge--primary">{{ auth.user()?.role }}</span>
            <button class="btn btn--ghost btn--sm" (click)="logout()">Sign out</button>
          </div>
        } @else {
          <div class="who">
            <a routerLink="/login">Sign in</a>
          </div>
        }
      </div>
    </header>

    <main>
      <router-outlet />
    </main>
  `,
  styles: [
    `
      .topbar {
        background: var(--surface);
        border-bottom: 1px solid var(--border);
        position: sticky;
        top: 0;
        z-index: 10;
      }
      .topbar__inner {
        max-width: 1080px;
        margin: 0 auto;
        padding: 12px 20px;
        display: flex;
        align-items: center;
        gap: 20px;
      }
      .brand {
        display: flex;
        align-items: center;
        gap: 9px;
        font-weight: 700;
        color: var(--ink);
        letter-spacing: -0.01em;
      }
      .brand:hover {
        text-decoration: none;
      }
      .brand__mark {
        font-size: 1.1rem;
      }
      .nav {
        display: flex;
        gap: 4px;
        margin-left: 8px;
      }
      .nav a {
        color: var(--muted);
        font-weight: 600;
        font-size: 0.9rem;
        padding: 6px 10px;
        border-radius: var(--radius-sm);
      }
      .nav a:hover {
        color: var(--ink);
        background: var(--surface-2);
        text-decoration: none;
      }
      .nav a.is-active {
        color: var(--primary-ink);
        background: var(--primary-wash);
      }
      .who {
        margin-left: auto;
        display: flex;
        align-items: center;
        gap: 10px;
      }
      .who__name {
        font-weight: 600;
        color: var(--ink);
        font-size: 0.9rem;
      }
      @media (max-width: 640px) {
        .who__name {
          display: none;
        }
        .topbar__inner {
          gap: 10px;
        }
      }
    `,
  ],
})
export class AppComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
