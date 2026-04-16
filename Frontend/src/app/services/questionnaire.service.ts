import { Injectable, inject, signal } from '@angular/core';
import { Api } from '../api/api';
import { listQuestionnaires } from '../api/fn/questionnaires/list-questionnaires';
import { createQuestionnaire } from '../api/fn/questionnaires/create-questionnaire';
import { getQuestionnaire } from '../api/fn/questionnaires/get-questionnaire';
import { updateQuestionnaire } from '../api/fn/questionnaires/update-questionnaire';
import { deleteQuestionnaire } from '../api/fn/questionnaires/delete-questionnaire';
import { restoreQuestionnaire } from '../api/fn/questionnaires/restore-questionnaire';
import { submitAnswers } from '../api/fn/answers/submit-answers';
import { QuestionnaireDto } from '../api/models/questionnaire-dto';
import { CreateQuestionnaireDto } from '../api/models/create-questionnaire-dto';
import { UpdateQuestionnaireDto } from '../api/models/update-questionnaire-dto';
import { SubmitAnswersDto } from '../api/models/submit-answers-dto';
import { AnswerSubmissionDto } from '../api/models/answer-submission-dto';

@Injectable({ providedIn: 'root' })
export class QuestionnaireService {
  private readonly api = inject(Api);

  readonly questionnaires = signal<QuestionnaireDto[]>([]);
  readonly includeDeleted = signal(false);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  async loadQuestionnaires(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const result = await this.api.invoke(listQuestionnaires, {
        includeDeleted: this.includeDeleted(),
      });
      this.questionnaires.set(result);
    } catch {
      this.error.set('Failed to load questionnaires.');
    } finally {
      this.loading.set(false);
    }
  }

  async getQuestionnaire(id: string): Promise<QuestionnaireDto> {
    return this.api.invoke(getQuestionnaire, { id });
  }

  async createQuestionnaire(dto: CreateQuestionnaireDto): Promise<QuestionnaireDto> {
    return this.api.invoke(createQuestionnaire, { body: dto });
  }

  async updateQuestionnaire(id: string, dto: UpdateQuestionnaireDto): Promise<QuestionnaireDto> {
    return this.api.invoke(updateQuestionnaire, { id, body: dto });
  }

  async deleteQuestionnaire(id: string): Promise<void> {
    await this.api.invoke(deleteQuestionnaire, { id });
    await this.loadQuestionnaires();
  }

  async restoreQuestionnaire(id: string): Promise<void> {
    await this.api.invoke(restoreQuestionnaire, { id });
    await this.loadQuestionnaires();
  }

  async submitAnswers(id: string, version: number, dto: SubmitAnswersDto): Promise<AnswerSubmissionDto> {
    return this.api.invoke(submitAnswers, { id, version, body: dto });
  }
}
