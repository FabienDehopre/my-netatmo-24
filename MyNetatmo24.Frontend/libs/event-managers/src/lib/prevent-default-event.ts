import { EventManagerPlugin } from '@angular/platform-browser';

export class PreventDefaultEventPlugin extends EventManagerPlugin {
  supports(eventName: string): boolean {
    return eventName.endsWith('.preventDefault');
  }

  addEventListener(element: HTMLElement, eventName: string, handler: (event: Event) => void): () => void {
    const [actualEventName] = eventName.split('.');

    const wrappedHandler = (event: Event) => {
      event.preventDefault();
      handler(event);
    };

    return this.manager.addEventListener(element, actualEventName, wrappedHandler) as () => void;
  }
}
