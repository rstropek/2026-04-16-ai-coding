import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-questionnaires',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Questionnaires</h1>
    <p>Manage your questionnaires here.</p>
  `,
  styles: `
    :host {
      display: block;
      padding: 2rem 0;
    }

    h1 {
      font-size: 1.75rem;
      font-weight: 600;
      margin-bottom: 0.5rem;
      color: var(--color-text);
    }

    p {
      color: var(--color-text-muted);
    }
  `,
})
export class Questionnaires {}
