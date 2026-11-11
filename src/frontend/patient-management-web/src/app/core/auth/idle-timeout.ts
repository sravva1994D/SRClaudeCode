import { fromEvent, map, merge, Observable, startWith, switchMap, timer } from 'rxjs';

/** The client signs out after 30 minutes without user input (CLAUDE.md 10.1). */
export const IDLE_TIMEOUT_MS = 30 * 60 * 1000;

const ACTIVITY_EVENTS = ['pointerdown', 'keydown', 'touchstart'] as const;

/**
 * Emits once each time the user has been idle for the full timeout. Any input restarts the countdown.
 * Subscribe with takeUntilDestroyed so the listeners are removed with the component.
 */
export function idleTimeouts(
  target: EventTarget,
  timeoutMs: number = IDLE_TIMEOUT_MS,
): Observable<void> {
  const activity = merge(...ACTIVITY_EVENTS.map((name) => fromEvent(target, name))).pipe(
    startWith(undefined),
  );
  return activity.pipe(switchMap(() => timer(timeoutMs).pipe(map(() => undefined))));
}
