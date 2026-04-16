import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Api } from './api/api';
import { ping } from './api/functions';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main>
      <h1>Questionaire</h1>
      <p>Ping response: {{ pingResponse() }}</p>
    </main>
  `,
  styleUrl: './app.css',
})
export class App {
  private readonly api = inject(Api);
  protected readonly pingResponse = signal('loading...');

  constructor() {
    this.api.invoke(ping).then(
      (response) => this.pingResponse.set(response),
      () => this.pingResponse.set('error'),
    );
  }
}
