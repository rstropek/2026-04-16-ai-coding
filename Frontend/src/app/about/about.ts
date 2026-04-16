import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-about',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>About</h1>
    <p>Questionaire application for creating and managing surveys.</p>
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
export class About {}
