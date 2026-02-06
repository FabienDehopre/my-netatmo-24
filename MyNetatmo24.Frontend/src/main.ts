import { bootstrapApplication } from '@angular/platform-browser';

import { App } from './app/app';
import { APP_CONFIG } from './app/app.config';

if (navigator.userAgent.includes('iPhone')) {
  document.querySelector('[name="viewport"]')?.setAttribute('content', 'width=device-width, initial-scale=1.0, maximum-scale=1.0');
}

bootstrapApplication(App, APP_CONFIG)
  .catch(console.error);
