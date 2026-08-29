import { Component, Input, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { apiErrorMessage } from '../../core/auth.interceptor';
import { ActiveAttemptView } from '../../core/models';

@Component({
  selector: 'app-take-quiz',
  template: `
    <div class="container container--narrow">
      @if (loading()) {
        <div class="loading"><span class="spinner"></span> Starting attempt…</div>
      } @else if (error()) {
        <div class="alert">{{ error() }}</div>
      } @else if (attempt()) {
        @let a = attempt()!;
        <div class="between quizhead">
          <div>
            <span class="badge">{{ a.subject }}</span>
            <h1 style="margin-top: 8px">{{ a.quizTitle }}</h1>
          </div>
          @if (a.timeLimitMinutes > 0) {
            <div class="timer" [class.timer--low]="secondsLeft() <= 30">
              {{ clock() }}
            </div>
          }
        </div>

        <p class="muted small">{{ answeredCount() }} of {{ a.questions.length }} answered</p>

        <div class="stack" style="margin-top: 8px">
          @for (q of a.questions; track q.questionId; let i = $index) {
            <div class="card stack">
              <h3>{{ i + 1 }}. {{ q.text }}</h3>
              <div class="options">
                @for (opt of q.options; track $index) {
                  <label class="option" [class.option--picked]="answers()[q.questionId] === $index">
                    <input
                      type="radio"
                      [name]="q.questionId"
                      [checked]="answers()[q.questionId] === $index"
                      (change)="pick(q.questionId, $index)"
                    />
                    <span>{{ letter($index) }}. {{ opt }}</span>
                  </label>
                }
              </div>
            </div>
          }
        </div>

        <div class="between" style="margin-top: 20px">
          <span class="small muted">You can change answers until you submit.</span>
          <button class="btn" (click)="submit()" [disabled]="submitting()">
            @if (submitting()) {
              <span class="spinner"></span> Submitting…
            } @else {
              Submit attempt
            }
          </button>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .quizhead {
        margin-bottom: 6px;
      }
      .timer {
        font-variant-numeric: tabular-nums;
        font-weight: 700;
        font-size: 1.15rem;
        color: var(--primary-ink);
        background: var(--primary-wash);
        padding: 6px 12px;
        border-radius: var(--radius-sm);
      }
      .timer--low {
        color: var(--bad);
        background: var(--bad-wash);
      }
      .options {
        display: flex;
        flex-direction: column;
        gap: 8px;
      }
      .option {
        display: flex;
        align-items: flex-start;
        gap: 10px;
        padding: 10px 12px;
        border: 1px solid var(--border-strong);
        border-radius: var(--radius-sm);
        cursor: pointer;
        transition: background 0.1s ease, border-color 0.1s ease;
      }
      .option:hover {
        background: var(--surface-2);
      }
      .option--picked {
        border-color: var(--primary);
        background: var(--primary-wash);
      }
      .option input {
        margin-top: 3px;
      }
    `,
  ],
})
export class TakeQuizComponent implements OnInit, OnDestroy {
  @Input() id!: string;

  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  readonly attempt = signal<ActiveAttemptView | null>(null);
  readonly answers = signal<Record<string, number>>({});
  readonly loading = signal(true);
  readonly error = signal('');
  readonly submitting = signal(false);
  readonly secondsLeft = signal(0);

  readonly answeredCount = computed(() => Object.keys(this.answers()).length);
  readonly clock = computed(() => {
    const s = Math.max(0, this.secondsLeft());
    const m = Math.floor(s / 60);
    return `${m}:${String(s % 60).padStart(2, '0')}`;
  });

  private ticker?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.api.startAttempt(this.id).subscribe({
      next: (a) => {
        this.attempt.set(a);
        this.loading.set(false);
        if (a.timeLimitMinutes > 0) this.startTimer(a);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.loading.set(false);
      },
    });
  }

  ngOnDestroy(): void {
    clearInterval(this.ticker);
  }

  pick(questionId: string, optionIndex: number): void {
    this.answers.update((a) => ({ ...a, [questionId]: optionIndex }));
  }

  letter(i: number): string {
    return String.fromCharCode(65 + i);
  }

  submit(): void {
    const a = this.attempt();
    if (!a || this.submitting()) return;
    this.submitting.set(true);
    clearInterval(this.ticker);

    const picks = this.answers();
    const answers = a.questions.map((q) => ({
      questionId: q.questionId,
      selectedOptionIndex: q.questionId in picks ? picks[q.questionId] : null,
    }));

    this.api.submitAttempt(a.attemptId, answers).subscribe({
      next: (res) => this.router.navigate(['/attempts', res.attemptId]),
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.submitting.set(false);
      },
    });
  }

  private startTimer(a: ActiveAttemptView): void {
    const deadline = new Date(a.startedAt).getTime() + a.timeLimitMinutes * 60_000;
    const tick = () => {
      const left = Math.round((deadline - Date.now()) / 1000);
      this.secondsLeft.set(left);
      if (left <= 0) {
        clearInterval(this.ticker);
        if (!this.submitting()) this.submit();
      }
    };
    tick();
    this.ticker = setInterval(tick, 1000);
  }
}
