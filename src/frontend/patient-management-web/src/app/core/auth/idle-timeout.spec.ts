import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { idleTimeouts } from './idle-timeout';

const ONE_MINUTE_MS = 60 * 1000;

describe('idleTimeouts', () => {
  let target: EventTarget;

  beforeEach(() => {
    vi.useFakeTimers();
    target = new EventTarget();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('emits_NoInputForTheFullTimeout_EmitsOnce', () => {
    let timeouts = 0;
    const subscription = idleTimeouts(target, 30 * ONE_MINUTE_MS).subscribe(() => timeouts++);

    vi.advanceTimersByTime(30 * ONE_MINUTE_MS);

    expect(timeouts).toBe(1);
    subscription.unsubscribe();
  });

  it('emits_InputBeforeTheTimeout_RestartsTheCountdown', () => {
    let timeouts = 0;
    const subscription = idleTimeouts(target, 30 * ONE_MINUTE_MS).subscribe(() => timeouts++);

    vi.advanceTimersByTime(29 * ONE_MINUTE_MS);
    target.dispatchEvent(new Event('keydown'));
    vi.advanceTimersByTime(29 * ONE_MINUTE_MS);

    expect(timeouts).toBe(0);
    subscription.unsubscribe();
  });

  it('emits_AfterUnsubscribe_DoesNotEmit', () => {
    let timeouts = 0;
    const subscription = idleTimeouts(target, 30 * ONE_MINUTE_MS).subscribe(() => timeouts++);
    subscription.unsubscribe();

    vi.advanceTimersByTime(60 * ONE_MINUTE_MS);

    expect(timeouts).toBe(0);
  });
});
