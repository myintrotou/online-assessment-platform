import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { apiErrorMessage } from '../../core/auth.interceptor';
import { AttemptResultView, HelpKind } from '../../core/models';
import { MarkdownPipe } from '../../shared/markdown.pipe';

interface HelpState {
  loading: boolean;
  content?: string;
  error?: string;
  cached?: boolean;
}

@Component({
  selector: 'app-attempt-result',
  imports: [RouterLink, MarkdownPipe],
  template: `
    <div class="container">
      @if (loading()) {
        <div class="loading"><span class="spinner"></span> Loading result…</div>
      } @else if (error()) {
        <div class="alert">{{ error() }}</div>
      } @else if (result()) {
        @let r = result()!;
        <div class="between" style="margin-bottom: 4px">
          <div>
            <span class="badge">{{ r.subject }}</span>
            <h1 style="margin-top: 8px">{{ r.quizTitle }}</h1>
          </div>
          <div class="scorebox" [class.scorebox--low]="r.scorePercent < 50">
            <div class="scorebox__pct">{{ r.scorePercent }}%</div>
            <div class="scorebox__sub">{{ r.correctCount }} / {{ r.totalQuestions }} correct</div>
          </div>
        </div>
        <p class="small muted">
          Submitted {{ formatDate(r.submittedAt) }} · took {{ minutes(r.durationSeconds) }}
        </p>

        <div class="card stack" style="margin: 16px 0">
          <div class="between">
            <h3 style="margin: 0">AI study plan</h3>
            @if (!guidance().content) {
              <button class="btn btn--subtle btn--sm" (click)="loadGuidance()" [disabled]="guidance().loading">
                @if (guidance().loading) {
                  <span class="spinner"></span> Thinking…
                } @else {
                  Get my study plan
                }
              </button>
            }
          </div>
          @if (guidance().error) {
            <div class="alert">{{ guidance().error }}</div>
          }
          @if (guidance().content) {
            <div class="prose" [innerHTML]="guidance().content | md"></div>
          } @else if (!guidance().loading) {
            <p class="small muted" style="margin: 0">
              A short, personalised plan based on which topics you got wrong.
            </p>
          }
        </div>

        <div class="stack">
          @for (q of r.questions; track q.questionId; let i = $index) {
            <div class="card stack">
              <div class="row" style="justify-content: space-between">
                <h3 style="margin: 0">{{ i + 1 }}. {{ q.text }}</h3>
                @if (q.isCorrect) {
                  <span class="badge badge--good">Correct</span>
                } @else {
                  <span class="badge badge--bad">
                    {{ q.selectedOptionIndex == null ? 'Skipped' : 'Wrong' }}
                  </span>
                }
              </div>

              <div class="options">
                @for (opt of q.options; track $index) {
                  <div
                    class="opt"
                    [class.opt--correct]="$index === q.correctOptionIndex"
                    [class.opt--wrong]="$index === q.selectedOptionIndex && $index !== q.correctOptionIndex"
                  >
                    <span class="opt__key">{{ letter($index) }}</span>
                    <span>{{ opt }}</span>
                    @if ($index === q.correctOptionIndex) {
                      <span class="opt__tag">correct answer</span>
                    } @else if ($index === q.selectedOptionIndex) {
                      <span class="opt__tag">your answer</span>
                    }
                  </div>
                }
              </div>

              @if (q.explanation) {
                <p class="small" style="margin: 0"><strong>Why:</strong> {{ q.explanation }}</p>
              }
              @if (q.topic) {
                <span class="badge">{{ q.topic }}</span>
              }

              <div class="row help-row">
                @for (kind of kinds; track kind) {
                  <button
                    class="btn btn--ghost btn--sm"
                    (click)="loadHelp(r.attemptId, q.questionId, kind)"
                    [disabled]="stateFor(q.questionId, kind).loading"
                  >
                    @if (stateFor(q.questionId, kind).loading) {
                      <span class="spinner"></span>
                    }
                    {{ kindLabel(kind) }}
                  </button>
                }
              </div>

              @for (kind of kinds; track kind) {
                @if (stateFor(q.questionId, kind); as s) {
                  @if (s.error) {
                    <div class="alert">{{ s.error }}</div>
                  } @else if (s.content) {
                    <div class="help-panel">
                      <div class="between">
                        <strong class="small">{{ kindLabel(kind) }}</strong>
                        @if (s.cached) {
                          <span class="badge">cached</span>
                        }
                      </div>
                      <div class="prose small" [innerHTML]="s.content | md"></div>
                    </div>
                  }
                }
              }
            </div>
          }
        </div>

        <div style="margin-top: 20px">
          <a class="btn btn--ghost" routerLink="/history">Back to history</a>
          <a class="btn btn--ghost" [routerLink]="['/quizzes', r.quizId, 'take']">Retake this quiz</a>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .scorebox {
        text-align: center;
        background: var(--good-wash);
        border-radius: var(--radius);
        padding: 12px 22px;
      }
      .scorebox--low {
        background: var(--bad-wash);
      }
      .scorebox__pct {
        font-size: 1.9rem;
        font-weight: 750;
        color: var(--ink);
        line-height: 1;
      }
      .scorebox__sub {
        font-size: 0.8rem;
        color: var(--muted);
        margin-top: 4px;
      }
      .options {
        display: flex;
        flex-direction: column;
        gap: 6px;
      }
      .opt {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 8px 11px;
        border: 1px solid var(--border);
        border-radius: var(--radius-sm);
        font-size: 0.92rem;
      }
      .opt__key {
        font-weight: 700;
        color: var(--faint);
        width: 1.2em;
      }
      .opt--correct {
        background: var(--good-wash);
        border-color: transparent;
      }
      .opt--wrong {
        background: var(--bad-wash);
        border-color: transparent;
      }
      .opt__tag {
        margin-left: auto;
        font-size: 0.72rem;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--muted);
      }
      .help-row {
        gap: 8px;
      }
      .help-panel {
        background: var(--surface-2);
        border: 1px solid var(--border);
        border-radius: var(--radius-sm);
        padding: 12px 14px;
        display: flex;
        flex-direction: column;
        gap: 6px;
      }
    `,
  ],
})
export class AttemptResultComponent implements OnInit {
  @Input() id!: string;

  private readonly api = inject(ApiService);

  readonly kinds: HelpKind[] = ['Solution', 'Theory', 'Guidance'];
  readonly result = signal<AttemptResultView | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly guidance = signal<HelpState>({ loading: false });
  readonly help = signal<Record<string, HelpState>>({});

  ngOnInit(): void {
    this.api.attemptResult(this.id).subscribe({
      next: (r) => {
        this.result.set(r);
        if (r.studyGuidance) this.guidance.set({ loading: false, content: r.studyGuidance });
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.loading.set(false);
      },
    });
  }

  letter(i: number): string {
    return String.fromCharCode(65 + i);
  }

  kindLabel(k: HelpKind): string {
    return k === 'Solution' ? 'Step-by-step' : k === 'Theory' ? 'The theory' : 'Study tip';
  }

  stateFor(questionId: string, kind: HelpKind): HelpState {
    return this.help()[`${questionId}|${kind}`] ?? { loading: false };
  }

  loadGuidance(): void {
    if (this.guidance().loading) return;
    this.guidance.set({ loading: true });
    this.api.studyGuidance(this.id).subscribe({
      next: (r) => this.guidance.set({ loading: false, content: r.guidance }),
      error: (err) => this.guidance.set({ loading: false, error: apiErrorMessage(err) }),
    });
  }

  loadHelp(attemptId: string, questionId: string, kind: HelpKind): void {
    const key = `${questionId}|${kind}`;
    if (this.help()[key]?.loading) return;
    this.setHelp(key, { loading: true });
    this.api.questionHelp(attemptId, questionId, kind).subscribe({
      next: (r) => this.setHelp(key, { loading: false, content: r.content, cached: r.wasCached }),
      error: (err) => this.setHelp(key, { loading: false, error: apiErrorMessage(err) }),
    });
  }

  private setHelp(key: string, state: HelpState): void {
    this.help.update((h) => ({ ...h, [key]: state }));
  }

  minutes(seconds: number): string {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return m > 0 ? `${m}m ${s}s` : `${s}s`;
  }

  formatDate(iso?: string): string {
    return iso ? new Date(iso).toLocaleString() : '—';
  }
}
