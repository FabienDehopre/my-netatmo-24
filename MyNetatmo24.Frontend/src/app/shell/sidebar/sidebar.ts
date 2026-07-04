import type { Signal } from '@angular/core';
import type { UrlTree } from '@angular/router';

import { NgOptimizedImage, NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, untracked } from '@angular/core';
import { isActive, Router, RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideBomb,
  lucideChevronRight,
  lucideHouse,
  lucideLogIn,
  lucideLogOut,
  lucideUserPlus
} from '@ng-icons/lucide';

import { Anonymous } from '@app/shared/ui-auth/anonymous';
import { Authenticated } from '@app/shared/ui-auth/authenticated';
import { Authentication } from '@app/shared/util-auth/authentication';
import { HlmAvatarImports } from '@spartan-ng/helm/avatar';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
import { HlmSidebarImports, HlmSidebarService, HlmSidebarWrapper } from '@spartan-ng/helm/sidebar';

interface MenuItem {
  title: string;
  icon: string;
  url: UrlTree;
  isActive: Signal<boolean>;
}

@Component({
  selector: 'app-sidebar',
  imports: [HlmSidebarImports, NgIcon, Anonymous, Authenticated, HlmAvatarImports, HlmDropdownMenuImports, RouterLink, NgTemplateOutlet, NgOptimizedImage],
  templateUrl: './sidebar.html',
  viewProviders: [provideIcons({
    lucideBomb,
    lucideChevronRight,
    lucideHouse,
    lucideLogIn,
    lucideLogOut,
    lucideUserPlus,
  })],
  hostDirectives: [HlmSidebarWrapper],
})
export class Sidebar {
  readonly #authentication = inject(Authentication);
  readonly #router = inject(Router);
  readonly #sidebarMenu = inject(HlmSidebarService);
  protected readonly menuSide = computed(() => (this.#sidebarMenu.isMobile() ? 'top' : 'right'));
  // protected readonly menuAlign = computed(() => (this.#sidebarMenu.isMobile() ? 'end' : 'start'));
  protected readonly menuOpen = this.#sidebarMenu.open;
  protected readonly userName = computed(() => {
    const user = this.#authentication.user();
    return untracked(() => {
      const preferredName = user?.claims.find((claim) => claim.type === 'auth0:preferred_username')?.value;
      // const firstName = user?.claims.find((claim) => claim.type === 'auth0:given_name')?.value;
      // const lastName = user?.claims.find((claim) => claim.type === 'auth0:family_name')?.value;
      return preferredName ?? user?.name ?? 'User';
    });
  });

  protected readonly email = computed(() => {
    const user = this.#authentication.user();
    return untracked(() => user?.claims.find((claim) => claim.type === 'email')?.value ?? 'john.doe@example.com');
  });

  protected readonly avatarUrl = computed(() => {
    const user = this.#authentication.user();
    return untracked(() => user?.claims.find((claim) => claim.type === 'picture')?.value ?? '/fake.png');
  });

  protected readonly menuItems: readonly MenuItem[] = [
    {
      title: 'Home',
      icon: 'lucideHouse',
      url: this.#router.createUrlTree(['/', 'home']),
      isActive: isActive(this.#router.createUrlTree(['/', 'home']), this.#router),
    },
    {
      title: 'Fake',
      icon: 'lucideBomb',
      url: this.#router.createUrlTree(['/', 'fake']),
      isActive: isActive(this.#router.createUrlTree(['/', 'fake']), this.#router),
    },
  ];

  protected signIn(): void {
    this.#authentication.login('/');
  }

  protected signUp(): void {
    // eslint-disable-next-line no-alert
    alert('Will redirect to signup');
  }

  protected signOut(): void {
    this.#authentication.logout('/');
  }

  protected async navigateToHome(): Promise<void> {
    await this.#router.navigate(['/', 'home']);
  }
}
