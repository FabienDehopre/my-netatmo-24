import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Authentication } from '~domains/authentication/authentication';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly authentication = inject(Authentication);
  protected readonly title = signal('MyNetatmo24').asReadonly();
  protected readonly user = this.authentication.user;

  protected login(): void {
    this.authentication.login('/');
  }

  protected logout(): void {
    this.authentication.logout('/');
  }
}
