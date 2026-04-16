import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faPlus,
  faPenToSquare,
  faReply,
  faTrash,
  faTrashArrowUp,
} from '@fortawesome/free-solid-svg-icons';
import { QuestionnaireService } from '../services/questionnaire.service';

@Component({
  selector: 'app-questionnaires',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, FontAwesomeModule],
  templateUrl: './questionnaires.html',
  styleUrl: './questionnaires.css',
})
export class Questionnaires implements OnInit {
  protected readonly service = inject(QuestionnaireService);

  protected readonly faPlus = faPlus;
  protected readonly faPenToSquare = faPenToSquare;
  protected readonly faReply = faReply;
  protected readonly faTrash = faTrash;
  protected readonly faTrashArrowUp = faTrashArrowUp;

  ngOnInit() {
    this.service.loadQuestionnaires();
  }

  onToggleDeleted() {
    this.service.includeDeleted.update(v => !v);
    this.service.loadQuestionnaires();
  }

  onDelete(id: string) {
    this.service.deleteQuestionnaire(id);
  }

  onRestore(id: string) {
    this.service.restoreQuestionnaire(id);
  }
}
