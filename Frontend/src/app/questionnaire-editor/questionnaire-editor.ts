import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faPlus, faArrowUp, faArrowDown, faXmark } from '@fortawesome/free-solid-svg-icons';
import { QuestionnaireService } from '../services/questionnaire.service';
import { QuestionType } from '../api/models/question-type';

interface QuestionFormGroup {
  text: FormControl<string>;
  type: FormControl<QuestionType>;
  isRequired: FormControl<boolean>;
}

@Component({
  selector: 'app-questionnaire-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, FontAwesomeModule],
  templateUrl: './questionnaire-editor.html',
  styleUrl: './questionnaire-editor.css',
})
export class QuestionnaireEditor {
  private readonly service = inject(QuestionnaireService);
  private readonly router = inject(Router);

  readonly id = input<string>();
  readonly isEditMode = computed(() => !!this.id());
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);

  readonly form = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    questions: new FormArray<FormGroup<QuestionFormGroup>>([]),
  });

  protected readonly faPlus = faPlus;
  protected readonly faArrowUp = faArrowUp;
  protected readonly faArrowDown = faArrowDown;
  protected readonly faXmark = faXmark;

  get questionsArray() {
    return this.form.controls.questions;
  }

  constructor() {
    effect(() => {
      const id = this.id();
      if (id) {
        this.loadExisting(id);
      } else {
        this.addQuestion();
      }
    });
  }

  private async loadExisting(id: string) {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const q = await this.service.getQuestionnaire(id);
      this.form.controls.title.setValue(q.title);
      this.questionsArray.clear();
      for (const question of q.questions) {
        this.questionsArray.push(this.createQuestionGroup(question.text, question.type, question.isRequired));
      }
    } catch {
      this.loadError.set('Failed to load questionnaire.');
    } finally {
      this.loading.set(false);
    }
  }

  private createQuestionGroup(text = '', type: QuestionType = 'Text', isRequired = false): FormGroup<QuestionFormGroup> {
    return new FormGroup<QuestionFormGroup>({
      text: new FormControl(text, { nonNullable: true, validators: [Validators.required] }),
      type: new FormControl(type, { nonNullable: true }),
      isRequired: new FormControl(isRequired, { nonNullable: true }),
    });
  }

  addQuestion() {
    this.questionsArray.push(this.createQuestionGroup());
  }

  removeQuestion(index: number) {
    this.questionsArray.removeAt(index);
  }

  moveUp(index: number) {
    if (index <= 0) return;
    const current = this.questionsArray.at(index);
    this.questionsArray.removeAt(index);
    this.questionsArray.insert(index - 1, current);
  }

  moveDown(index: number) {
    if (index >= this.questionsArray.length - 1) return;
    const current = this.questionsArray.at(index);
    this.questionsArray.removeAt(index);
    this.questionsArray.insert(index + 1, current);
  }

  async onSave() {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading.set(true);
    try {
      const value = this.form.getRawValue();
      const dto = {
        title: value.title,
        questions: value.questions.map(q => ({
          text: q.text,
          type: q.type,
          isRequired: q.isRequired,
        })),
      };

      const id = this.id();
      if (id) {
        await this.service.updateQuestionnaire(id, dto);
      } else {
        await this.service.createQuestionnaire(dto);
      }
      this.router.navigate(['/questionnaires']);
    } catch {
      this.loadError.set('Failed to save questionnaire.');
    } finally {
      this.loading.set(false);
    }
  }

  onCancel() {
    this.router.navigate(['/questionnaires']);
  }
}
