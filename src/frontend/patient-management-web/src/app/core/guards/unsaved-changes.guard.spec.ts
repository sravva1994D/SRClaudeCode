import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { HasUnsavedChanges, unsavedChangesGuard } from './unsaved-changes.guard';

const ROUTE = {} as ActivatedRouteSnapshot;
const STATE = {} as RouterStateSnapshot;

function page(unsaved: boolean): HasUnsavedChanges {
  return { hasUnsavedChanges: () => unsaved };
}

describe('unsavedChangesGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    TestBed.resetTestingModule();
  });

  it('lets the user leave without asking when nothing is unsaved', () => {
    const confirm = vi.spyOn(window, 'confirm');

    const allowed = TestBed.runInInjectionContext(() =>
      unsavedChangesGuard(page(false), ROUTE, STATE, STATE),
    );

    expect(allowed).toBe(true);
    expect(confirm).not.toHaveBeenCalled();
  });

  it('keeps the user on the page when they cancel the prompt about unsaved edits', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    const allowed = TestBed.runInInjectionContext(() =>
      unsavedChangesGuard(page(true), ROUTE, STATE, STATE),
    );

    expect(allowed).toBe(false);
  });

  it('lets the user leave, discarding the edits, when they confirm the prompt', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    const allowed = TestBed.runInInjectionContext(() =>
      unsavedChangesGuard(page(true), ROUTE, STATE, STATE),
    );

    expect(allowed).toBe(true);
  });
});
