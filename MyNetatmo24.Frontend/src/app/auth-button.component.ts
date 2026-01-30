import { Component, Inject, DOCUMENT } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { AsyncPipe } from '@angular/common';

@Component({
    selector: 'app-auth-button',
    template: `
    @if (auth.isAuthenticated$ | async) {
      <button (click)="auth.logout({ logoutParams: { returnTo: document.location.origin } })">
        Log out
      </button>
    } @else {
      <button (click)="auth.loginWithRedirect()">Log in</button>
    }
    
    `,
    imports: [AsyncPipe]
})
export class AuthButtonComponent {
  constructor(@Inject(DOCUMENT) public document: Document, public auth: AuthService) {}
}
