import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService, QuestionPayload } from '../../core/api.service';
import { apiErrorMessage } from '../../core/auth.interceptor';
import { QuizManageDetail } from '../../core/models';

interface Draft {
  text: string;
  options: string[];
  correctOptionIndex: number;
  explanation: string;
  topic: string;
  keep: boolean;
}

@Component({
  selector: 'app-quiz-editor',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="container">
      <a routerLink="/teach" class="small muted">← My quizzes</a>

      <!-- CREATE -->
      @if (!id) {
        <h1 style="margin-top: 8px">New quiz</h1>
        @if (error()) {
          <div class="alert">{{ error() }}</div>
        }
        <div class="card card--pad-lg stack" style="max-width: 520px">
          <div class="field">
            <label>Title</label>
            <input type="text" [(ngModel)]="title" name="title" placeholder="e.g. Arrays and Strings" />
          </div>
          <div class="field">
            <label>Subject</label>
            <input type="text" [(ngModel)]="subject" name="subject" placeholder="e.g. Computer Science" />
          </div>
          <div class="field">
            <label>Description <span class="hint">(optional)</span></label>
            <textarea [(ngModel)]="description" name="description" rows="2"></textarea>
          </div>
          <div class="field">
            <label>Time limit (minutes) <span class="hint">— 0 for untimed</span></label>
            <input type="number" [(ngModel)]="timeLimit" name="timeLimit" min="0" max="240" />
          </div>
          <button class="btn" (click)="create()" [disabled]="saving() || !title.trim() || !subject.trim()">
            @if (saving()) {
              <span class="spinner"></span> Creating…
            } @else {
              Create quiz
            }
          </button>
        </div>
      }

      <!-- EDIT -->
      @else {
        @if (loading()) {
          <div class="loading"><span class="spinner"></span> Loading…</div>
        } @else if (error() && !quiz()) {
          <div class="alert">{{ error() }}</div>
        } @else if (quiz()) {
          @let q = quiz()!;
          <div class="between" style="margin-top: 8px">
            <div>
              <div class="row" style="gap: 8px">
                <h1 style="margin: 0">{{ q.title }}</h1>
                @if (q.isPublished) {
                  <span class="badge badge--good">Published</span>
                } @else {
                  <span class="badge badge--warn">Draft</span>
                }
              </div>
              <p class="muted small" style="margin: 4px 0 0">
                {{ q.subject }} · {{ q.questions.length }} questions ·
                {{ q.timeLimitMinutes ? q.timeLimitMinutes + ' min' : 'untimed' }}
              </p>
            </div>
            <div class="row">
              <a class="btn btn--ghost btn--sm" [routerLink]="['/teach', q.id, 'results']">Results</a>
              <button
                class="btn btn--sm"
                [class.btn--subtle]="q.isPublished"
                (click)="togglePublish()"
                [disabled]="saving() || (!q.isPublished && q.questions.length === 0)"
              >
                {{ q.isPublished ? 'Unpublish' : 'Publish' }}
              </button>
            </div>
          </div>

          @if (error()) {
            <div class="alert" style="margin-top: 12px">{{ error() }}</div>
          }

          @if (q.isPublished) {
            <div class="alert alert--info" style="margin-top: 12px">
              Unpublish the quiz to add or remove questions.
            </div>
          }

          <!-- existing questions -->
          <h2 style="margin-top: 22px">Questions</h2>
          @if (q.questions.length === 0) {
            <div class="empty">No questions yet. Add some below.</div>
          } @else {
            <div class="stack">
              @for (question of q.questions; track question.id; let i = $index) {
                <div class="card stack">
                  <div class="between">
                    <h3 style="margin: 0">{{ i + 1 }}. {{ question.text }}</h3>
                    <div class="row">
                      @if (question.source === 'AiGenerated') {
                        <span class="badge badge--primary">AI</span>
                      }
                      @if (!q.isPublished) {
                        <button class="btn btn--danger btn--sm" (click)="remove(question.id)" [disabled]="saving()">
                          Remove
                        </button>
                      }
                    </div>
                  </div>
                  <ul class="opts">
                    @for (opt of question.options; track $index) {
                      <li [class.opts--correct]="$index === question.correctOptionIndex">
                        {{ letter($index) }}. {{ opt }}
                        @if ($index === question.correctOptionIndex) {
                          <span class="badge badge--good">correct</span>
                        }
                      </li>
                    }
                  </ul>
                  @if (question.topic) {
                    <span class="badge">{{ question.topic }}</span>
                  }
                </div>
              }
            </div>
          }

          @if (!q.isPublished) {
            <!-- AI generation -->
            <div class="card card--pad-lg stack" style="margin-top: 22px">
              <div>
                <h2 style="margin: 0">Draft questions with AI</h2>
                <p class="muted small" style="margin: 4px 0 0">
                  Give a topic; review what comes back before saving.
                </p>
              </div>
              <div class="row" style="align-items: flex-end">
                <div class="field grow">
                  <label>Topic</label>
                  <input type="text" [(ngModel)]="aiTopic" name="aiTopic" placeholder="e.g. Recursion" />
                </div>
                <div class="field" style="width: 90px">
                  <label>Count</label>
                  <input type="number" [(ngModel)]="aiCount" name="aiCount" min="1" max="20" />
                </div>
                <div class="field" style="width: 130px">
                  <label>Difficulty</label>
                  <select [(ngModel)]="aiDifficulty" name="aiDifficulty">
                    <option value="easy">Easy</option>
                    <option value="medium">Medium</option>
                    <option value="hard">Hard</option>
                  </select>
                </div>
                <button class="btn" (click)="generate()" [disabled]="aiLoading() || !aiTopic.trim()">
                  @if (aiLoading()) {
                    <span class="spinner"></span> Generating…
                  } @else {
                    Generate
                  }
                </button>
              </div>
              @if (aiError()) {
                <div class="alert">{{ aiError() }}</div>
              }

              @if (drafts().length > 0) {
                <hr class="divider" />
                <div class="between">
                  <strong class="small">{{ keepCount() }} of {{ drafts().length }} selected</strong>
                  <button class="btn btn--subtle btn--sm" (click)="saveDrafts()" [disabled]="saving() || keepCount() === 0">
                    Save selected
                  </button>
                </div>
                <div class="stack">
                  @for (d of drafts(); track $index; let di = $index) {
                    <div class="draft" [class.draft--out]="!d.keep">
                      <label class="draft__keep">
                        <input type="checkbox" [(ngModel)]="d.keep" [name]="'keep' + di" />
                        <span>{{ d.text }}</span>
                      </label>
                      <div class="draft__opts">
                        @for (opt of d.options; track $index) {
                          <label class="draft__opt" [class.draft__opt--correct]="d.correctOptionIndex === $index">
                            <input
                              type="radio"
                              [name]="'correct' + di"
                              [checked]="d.correctOptionIndex === $index"
                              (change)="d.correctOptionIndex = $index"
                            />
                            {{ letter($index) }}. {{ opt }}
                          </label>
                        }
                      </div>
                      @if (d.topic) {
                        <span class="badge">{{ d.topic }}</span>
                      }
                    </div>
                  }
                </div>
              }
            </div>

            <!-- manual add -->
            <div class="card card--pad-lg stack" style="margin-top: 16px">
              <h2 style="margin: 0">Add a question manually</h2>
              <div class="field">
                <label>Question</label>
                <textarea [(ngModel)]="mText" name="mText" rows="2"></textarea>
              </div>
              <div class="stack">
                @for (opt of mOptions; track $index; let oi = $index) {
                  <label class="draft__opt" [class.draft__opt--correct]="mCorrect === oi">
                    <input type="radio" name="mCorrect" [checked]="mCorrect === oi" (change)="mCorrect = oi" />
                    <input
                      type="text"
                      [(ngModel)]="mOptions[oi]"
                      [name]="'mOpt' + oi"
                      [placeholder]="'Option ' + letter(oi)"
                      style="flex: 1"
                    />
                  </label>
                }
              </div>
              <div class="row">
                <div class="field grow">
                  <label>Topic <span class="hint">(optional)</span></label>
                  <input type="text" [(ngModel)]="mTopic" name="mTopic" />
                </div>
              </div>
              <div class="field">
                <label>Explanation <span class="hint">(optional)</span></label>
                <textarea [(ngModel)]="mExplanation" name="mExplanation" rows="2"></textarea>
              </div>
              <button class="btn btn--ghost" (click)="addManual()" [disabled]="saving() || !manualValid()">
                Add question
              </button>
            </div>
          }
        }
      }
    </div>
  `,
  styles: [
    `
      .opts {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 4px;
        font-size: 0.9rem;
      }
      .opts li {
        padding: 6px 10px;
        border-radius: var(--radius-sm);
        background: var(--surface-2);
        display: flex;
        gap: 8px;
        align-items: center;
      }
      .opts--correct {
        background: var(--good-wash);
      }
      .draft {
        border: 1px solid var(--border-strong);
        border-radius: var(--radius-sm);
        padding: 12px 14px;
        display: flex;
        flex-direction: column;
        gap: 8px;
      }
      .draft--out {
        opacity: 0.5;
      }
      .draft__keep {
        display: flex;
        gap: 9px;
        font-weight: 600;
        color: var(--ink);
        cursor: pointer;
        align-items: flex-start;
      }
      .draft__opts {
        display: flex;
        flex-direction: column;
        gap: 5px;
      }
      .draft__opt {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 0.88rem;
        padding: 5px 8px;
        border-radius: 6px;
        cursor: pointer;
      }
      .draft__opt--correct {
        background: var(--good-wash);
      }
    `,
  ],
})
export class QuizEditorComponent implements OnInit {
  @Input() id?: string;

  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  // create form
  title = '';
  subject = '';
  description = '';
  timeLimit = 15;

  // edit state
  readonly quiz = signal<QuizManageDetail | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');

  // AI panel
  aiTopic = '';
  aiCount = 5;
  aiDifficulty = 'medium';
  readonly aiLoading = signal(false);
  readonly aiError = signal('');
  readonly drafts = signal<Draft[]>([]);

  /** Method, not computed: draft.keep is mutated in place by [(ngModel)]. */
  keepCount(): number {
    return this.drafts().filter((d) => d.keep).length;
  }

  // manual panel
  mText = '';
  mOptions = ['', '', '', ''];
  mCorrect = 0;
  mTopic = '';
  mExplanation = '';

  ngOnInit(): void {
    if (this.id) this.load();
  }

  letter(i: number): string {
    return String.fromCharCode(65 + i);
  }

  private load(): void {
    this.loading.set(true);
    this.api.manageQuiz(this.id!).subscribe({
      next: (q) => {
        this.quiz.set(q);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.loading.set(false);
      },
    });
  }

  create(): void {
    this.saving.set(true);
    this.error.set('');
    this.api
      .createQuiz({
        title: this.title.trim(),
        subject: this.subject.trim(),
        description: this.description.trim() || undefined,
        timeLimitMinutes: Number(this.timeLimit) || 0,
      })
      .subscribe({
        next: (q) => this.router.navigate(['/teach', q.id]),
        error: (err) => {
          this.error.set(apiErrorMessage(err));
          this.saving.set(false);
        },
      });
  }

  togglePublish(): void {
    const q = this.quiz();
    if (!q) return;
    this.saving.set(true);
    this.error.set('');
    const call = q.isPublished ? this.api.unpublish(q.id) : this.api.publish(q.id);
    call.subscribe({
      next: (updated) => {
        this.quiz.set(updated);
        this.saving.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.saving.set(false);
      },
    });
  }

  remove(questionId: string): void {
    const q = this.quiz();
    if (!q) return;
    this.saving.set(true);
    this.error.set('');
    this.api.deleteQuestion(q.id, questionId).subscribe({
      next: () => {
        this.quiz.update((cur) =>
          cur ? { ...cur, questions: cur.questions.filter((x) => x.id !== questionId) } : cur,
        );
        this.saving.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.saving.set(false);
      },
    });
  }

  generate(): void {
    const q = this.quiz();
    if (!q) return;
    this.aiLoading.set(true);
    this.aiError.set('');
    this.drafts.set([]);
    this.api
      .generateQuestions(q.id, {
        topic: this.aiTopic.trim(),
        count: Number(this.aiCount) || 5,
        difficulty: this.aiDifficulty,
      })
      .subscribe({
        next: (list) => {
          this.drafts.set(
            list.map((d) => ({
              text: d.text,
              options: d.options,
              correctOptionIndex: d.correctOptionIndex,
              explanation: d.explanation,
              topic: d.topic,
              keep: true,
            })),
          );
          this.aiLoading.set(false);
        },
        error: (err) => {
          this.aiError.set(apiErrorMessage(err));
          this.aiLoading.set(false);
        },
      });
  }

  saveDrafts(): void {
    const q = this.quiz();
    if (!q) return;
    const payload: QuestionPayload[] = this.drafts()
      .filter((d) => d.keep)
      .map((d) => ({
        text: d.text,
        options: d.options,
        correctOptionIndex: d.correctOptionIndex,
        explanation: d.explanation || undefined,
        topic: d.topic || undefined,
        source: 'AiGenerated',
      }));
    if (payload.length === 0) return;

    this.saving.set(true);
    this.error.set('');
    this.api.addQuestions(q.id, payload).subscribe({
      next: (updated) => {
        this.quiz.set(updated);
        this.drafts.set([]);
        this.aiTopic = '';
        this.saving.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err));
        this.saving.set(false);
      },
    });
  }

  manualValid(): boolean {
    return (
      this.mText.trim().length > 0 &&
      this.mOptions.filter((o) => o.trim().length > 0).length >= 2 &&
      !!this.mOptions[this.mCorrect]?.trim()
    );
  }

  addManual(): void {
    const q = this.quiz();
    if (!q || !this.manualValid()) return;
    const options = this.mOptions.map((o) => o.trim()).filter((o) => o.length > 0);

    this.saving.set(true);
    this.error.set('');
    this.api
      .addQuestions(q.id, [
        {
          text: this.mText.trim(),
          options,
          correctOptionIndex: this.mCorrect,
          explanation: this.mExplanation.trim() || undefined,
          topic: this.mTopic.trim() || undefined,
          source: 'Manual',
        },
      ])
      .subscribe({
        next: (updated) => {
          this.quiz.set(updated);
          this.mText = '';
          this.mOptions = ['', '', '', ''];
          this.mCorrect = 0;
          this.mTopic = '';
          this.mExplanation = '';
          this.saving.set(false);
        },
        error: (err) => {
          this.error.set(apiErrorMessage(err));
          this.saving.set(false);
        },
      });
  }
}
