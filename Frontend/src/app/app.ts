import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faClipboardList, faCircleInfo } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FontAwesomeModule],
  template: `
    <nav class="nav-bar" aria-label="Main navigation">
      <div class="nav-content">
        <span class="nav-brand">Questionaire</span>
        <ul class="nav-links" role="list">
          <li>
            <a routerLink="/questionnaires" routerLinkActive="active" aria-current="page">
              <fa-icon [icon]="faClipboardList" aria-hidden="true" />
              Questionnaires
            </a>
          </li>
          <li>
            <a routerLink="/about" routerLinkActive="active" aria-current="page">
              <fa-icon [icon]="faCircleInfo" aria-hidden="true" />
              About
            </a>
          </li>
        </ul>
      </div>
    </nav>
    <main class="content">
      <router-outlet />
    </main>
  `,
  styleUrl: './app.css',
})
export class App {
  protected readonly faClipboardList = faClipboardList;
  protected readonly faCircleInfo = faCircleInfo;
}
