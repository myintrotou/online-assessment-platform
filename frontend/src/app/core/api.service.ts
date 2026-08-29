import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import {
  ActiveAttemptView,
  AttemptHistoryItem,
  AttemptResultView,
  GeneratedQuestionDto,
  HelpKind,
  PublishedQuizItem,
  QuestionHelpView,
  QuizListItem,
  QuizManageDetail,
  QuizResultsSummary,
} from './models';

export interface QuestionPayload {
  text: string;
  options: string[];
  correctOptionIndex: number;
  explanation?: string;
  topic?: string;
  source: 'Manual' | 'AiGenerated';
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  // ---- Student ----
  publishedQuizzes() {
    return this.http.get<PublishedQuizItem[]>(`${this.base}/api/quizzes/published`);
  }
  startAttempt(quizId: string) {
    return this.http.post<ActiveAttemptView>(`${this.base}/api/attempts/start/${quizId}`, {});
  }
  submitAttempt(attemptId: string, answers: { questionId: string; selectedOptionIndex: number | null }[]) {
    return this.http.post<AttemptResultView>(`${this.base}/api/attempts/${attemptId}/submit`, { answers });
  }
  attemptResult(attemptId: string) {
    return this.http.get<AttemptResultView>(`${this.base}/api/attempts/${attemptId}`);
  }
  history() {
    return this.http.get<AttemptHistoryItem[]>(`${this.base}/api/attempts/history`);
  }
  studyGuidance(attemptId: string) {
    return this.http.post<{ guidance: string }>(`${this.base}/api/attempts/${attemptId}/guidance`, {});
  }
  questionHelp(attemptId: string, questionId: string, kind: HelpKind) {
    return this.http.post<QuestionHelpView>(
      `${this.base}/api/attempts/${attemptId}/questions/${questionId}/help?kind=${kind}`,
      {},
    );
  }

  // ---- Instructor ----
  myQuizzes() {
    return this.http.get<QuizListItem[]>(`${this.base}/api/quizzes/mine`);
  }
  createQuiz(body: { title: string; subject: string; description?: string; timeLimitMinutes: number }) {
    return this.http.post<QuizManageDetail>(`${this.base}/api/quizzes`, body);
  }
  manageQuiz(id: string) {
    return this.http.get<QuizManageDetail>(`${this.base}/api/quizzes/${id}/manage`);
  }
  generateQuestions(id: string, body: { topic: string; count: number; difficulty: string }) {
    return this.http.post<GeneratedQuestionDto[]>(`${this.base}/api/quizzes/${id}/questions/generate`, body);
  }
  addQuestions(id: string, questions: QuestionPayload[]) {
    return this.http.post<QuizManageDetail>(`${this.base}/api/quizzes/${id}/questions`, { questions });
  }
  deleteQuestion(id: string, questionId: string) {
    return this.http.delete<void>(`${this.base}/api/quizzes/${id}/questions/${questionId}`);
  }
  publish(id: string) {
    return this.http.post<QuizManageDetail>(`${this.base}/api/quizzes/${id}/publish`, {});
  }
  unpublish(id: string) {
    return this.http.post<QuizManageDetail>(`${this.base}/api/quizzes/${id}/unpublish`, {});
  }
  results(id: string) {
    return this.http.get<QuizResultsSummary>(`${this.base}/api/quizzes/${id}/results`);
  }
}
