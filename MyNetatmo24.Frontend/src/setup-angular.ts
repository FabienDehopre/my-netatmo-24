import { NgModule } from '@angular/core';
import { ɵgetCleanupHook as getCleanupHook, getTestBed } from '@angular/core/testing';
import { BrowserTestingModule, platformBrowserTesting } from '@angular/platform-browser/testing';
import { afterEach, beforeEach } from 'vitest';

import providers from './test-providers';

beforeEach(getCleanupHook(false));
afterEach(getCleanupHook(true));

const ANGULAR_TESTBED_SETUP = Symbol.for('@angular/cli/testbed-setup');
type AngularGlobal = typeof globalThis & {
  [ANGULAR_TESTBED_SETUP]?: boolean;
};

if (!(globalThis as AngularGlobal)[ANGULAR_TESTBED_SETUP]) {
  (globalThis as AngularGlobal)[ANGULAR_TESTBED_SETUP] = true;

  @NgModule({
    providers: [...providers],
  })
  class TestModule {}

  getTestBed().initTestEnvironment([BrowserTestingModule, TestModule], platformBrowserTesting(), {
    errorOnUnknownElements: true,
    errorOnUnknownProperties: true,
    teardown: {
      destroyAfterEach: false,
    },
  });
}
