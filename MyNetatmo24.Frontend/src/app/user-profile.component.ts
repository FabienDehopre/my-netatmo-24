import { Component } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import {AsyncPipe, JsonPipe, NgIf, NgOptimizedImage} from "@angular/common";

@Component({
    selector: 'app-user-profile',
    template: `
    @if (auth.user$ | async; as user) {
      <ul>
        <li>{{ user.nickname }}</li>
        <li>{{ user.email }}</li>
      </ul>
      @if (user.picture) {
        <img [ngSrc]="user.picture" width="100" height="100"/>
      }
    }`,
    imports: [AsyncPipe, NgIf, JsonPipe, NgOptimizedImage]
})
export class UserProfileComponent {
  constructor(public auth: AuthService) {}
}
