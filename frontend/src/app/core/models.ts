export type UserRole = 'Student' | 'Instructor';
export type QuestionSource = 'Manual' | 'AiGenerated';
export type HelpKind = 'Solution' | 'Theory' | 'Guidance';

export interface UserSummary {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: UserSummary;
}

export interface PublishedQuizItem {
  id: string;
  title: string;
  subject: string;
  description?: string;
  timeLimitMinutes: number;
  questionCount: number;
  myAttempts: number;
  myBestScore?: number;
}

export interface QuizListItem {
  id: string;
  title: string;
  subject: string;
  isPublished: boolean;
  questionCount: number;
  attemptCount: number;
  createdAt: string;
}

export interface ManageQuestion {
  id: string;
  text: string;
  options: string[];
  correctOptionIndex: number;
  explanation?: string;
  topic?: string;
  source: QuestionSource;
  orderIndex: number;
}

export interface QuizManageDetail {
  id: string;
  title: string;
  subject: string;
  description?: string;
  timeLimitMinutes: number;
  isPublished: boolean;
  createdAt: string;
  publishedAt?: string;
  questions: ManageQuestion[];
}

export interface GeneratedQuestionDto {
  text: string;
  options: string[];
  correctOptionIndex: number;
  explanation: string;
  topic: string;
}

/** Local editing shape for a question before it is saved. */
export interface QuestionDraft {
  text: string;
  options: string[];
  correctOptionIndex: number;
  explanation?: string;
  topic?: string;
  source: QuestionSource;
  keep: boolean;
}

export interface AttemptQuestionView {
  questionId: string;
  text: string;
  options: string[];
  orderIndex: number;
}

export interface ActiveAttemptView {
  attemptId: string;
  quizId: string;
  quizTitle: string;
  subject: string;
  timeLimitMinutes: number;
  startedAt: string;
  questions: AttemptQuestionView[];
}

export interface ReviewedQuestion {
  questionId: string;
  text: string;
  options: string[];
  correctOptionIndex: number;
  selectedOptionIndex?: number;
  isCorrect: boolean;
  explanation?: string;
  topic?: string;
}

export interface AttemptResultView {
  attemptId: string;
  quizId: string;
  quizTitle: string;
  subject: string;
  scorePercent: number;
  correctCount: number;
  totalQuestions: number;
  startedAt: string;
  submittedAt?: string;
  durationSeconds: number;
  questions: ReviewedQuestion[];
  studyGuidance?: string;
}

export interface AttemptHistoryItem {
  attemptId: string;
  quizId: string;
  quizTitle: string;
  subject: string;
  scorePercent: number;
  correctCount: number;
  totalQuestions: number;
  submittedAt?: string;
}

export interface QuestionHelpView {
  kind: HelpKind;
  content: string;
  wasCached: boolean;
}

export interface QuestionAccuracy {
  questionId: string;
  text: string;
  topic?: string;
  answered: number;
  correct: number;
  accuracyPercent: number;
}

export interface AttemptRow {
  attemptId: string;
  studentName: string;
  scorePercent: number;
  correctCount: number;
  totalQuestions: number;
  submittedAt?: string;
}

export interface QuizResultsSummary {
  quizId: string;
  title: string;
  attemptCount: number;
  averageScore: number;
  questionBreakdown: QuestionAccuracy[];
  attempts: AttemptRow[];
}

export interface ApiError {
  error: string;
}
