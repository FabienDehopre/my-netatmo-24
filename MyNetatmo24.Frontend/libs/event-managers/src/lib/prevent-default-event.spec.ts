import { ChangeDetectionStrategy, Component } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import { userEvent } from '@testing-library/user-event';
import { describe, expect, test, vi } from 'vitest';

import { PreventDefaultEventPlugin } from './prevent-default-event';
import { provideEventPlugins } from '.';

@Component({
  selector: 'app-test',
  standalone: true,
  template: ` <a href="https://example.com" (click.preventDefault)="handleClick()"> Click me </a> `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
class TestComponent {
  handleClick = vi.fn();
}

describe('preventDefaultEvent Manager', () => {
  test('prevents default behavior and calls handler', async () => {
    const user = userEvent.setup();
    const { fixture } = await render(TestComponent, { providers: [provideEventPlugins()] });

    const component = fixture.componentInstance;
    const link = screen.getByRole('link', { name: 'Click me' });

    await user.click(link);

    expect(component.handleClick).toHaveBeenCalledTimes(1);
  });

  test('plugin supports .preventDefault modifier', () => {
    const plugin = new PreventDefaultEventPlugin({});
    expect(plugin.supports('click.preventDefault')).toBeTruthy();
    expect(plugin.supports('submit.preventDefault')).toBeTruthy();
    expect(plugin.supports('click')).toBeFalsy();
  });
});
