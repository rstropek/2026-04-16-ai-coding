import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faCheck } from '@fortawesome/free-solid-svg-icons';
import { QuestionnaireService } from '../services/questionnaire.service';
import { QuestionnaireDto } from '../api/models/questionnaire-dto';

@Component({
  selector: 'app-answer-questionnaire',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, FontAwesomeModule],
  templateUrl: './answer-questionnaire.html',
  styleUrl: './answer-questionnaire.css',
})
export class AnswerQuestionnaire {
  private readonly service = inject(QuestionnaireService);
  private readonly router = inject(Router);

  readonly id = input.required<string>();
  readonly questionnaire = signal<QuestionnaireDto | null>(null);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly submitted = signal(false);
  readonly error = signal<string | null>(null);

  form = new FormGroup<Record<string, FormControl>>({});

  protected readonly faCheck = faCheck;

  constructor() {
    effect(() => {
      const id = this.id();
      if (id) {
        this.loadQuestionnaire(id);
      }
    });
  }

  private async loadQuestionnaire(id: string) {
    this.loading.set(true);
    this.error.set(null);
    try {
      const q = await this.service.getQuestionnaire(id);
      this.questionnaire.set(q);
      this.buildForm(q);
    } catch {
      this.error.set('Failed to load questionnaire.');
    } finally {
      this.loading.set(false);
    }
  }

  private buildForm(q: QuestionnaireDto) {
    const controls: Record<string, FormControl> = {};
    for (const question of q.questions) {
      if (question.type === 'Boolean') {
        controls[question.id] = new FormControl(false, { nonNullable: true });
      } else {
        const validators = question.isRequired ? [Validators.required] : [];
        controls[question.id] = new FormControl('', { nonNullable: true, validators });
      }
    }
    this.form = new FormGroup(controls);
  }

  async onSubmit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const q = this.questionnaire();
    if (!q) return;

    this.submitting.set(true);
    this.error.set(null);
    try {
      const formValues = this.form.getRawValue();
      const answers = q.questions.map(question => ({
        questionId: question.id,
        value: question.type === 'Boolean' ? String(formValues[question.id]) : formValues[question.id],
      }));

      await this.service.submitAnswers(q.id, Number(q.version), { answers });
      this.submitted.set(true);
    } catch {
      this.error.set('Failed to submit answers. Please check your responses and try again.');
    } finally {
      this.submitting.set(false);
    }
  }
}
