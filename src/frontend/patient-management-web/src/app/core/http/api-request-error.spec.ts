import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';

import { toApiRequestError } from './api-request-error';

const translate = (key: string): string => `text:${key}`;

function httpError(status: number, error: unknown = null): HttpErrorResponse {
  return new HttpErrorResponse({ status, error, url: '/api/v1/test' });
}

describe('toApiRequestError', () => {
  it('maps a missing response to a network error with a translated message', () => {
    const result = toApiRequestError(httpError(0), translate);

    expect(result.kind).toBe('network');
    expect(result.message).toBe('text:errors.network');
  });

  it('maps a 422 to a business error and keeps the server message and code', () => {
    const body = {
      code: 'appointment_in_past',
      message: 'The appointment is in the past.',
      correlationId: 'abc-123',
    };

    const result = toApiRequestError(httpError(422, body), translate);

    expect(result.kind).toBe('business');
    expect(result.code).toBe('appointment_in_past');
    expect(result.message).toBe('The appointment is in the past.');
    expect(result.correlationId).toBe('abc-123');
  });

  it('maps a 500 to a server error with a generic message and no server text', () => {
    const body = { code: 'internal_error', message: 'Stack detail', correlationId: 'xyz' };

    const result = toApiRequestError(httpError(500, body), translate);

    expect(result.kind).toBe('server');
    expect(result.message).toBe('text:errors.server');
    expect(result.correlationId).toBe('xyz');
  });

  it('maps another 4xx without a body message to the generic request message', () => {
    const result = toApiRequestError(httpError(404), translate);

    expect(result.kind).toBe('request');
    expect(result.message).toBe('text:errors.request');
  });

  it('maps a non-HTTP failure to an unknown error', () => {
    const result = toApiRequestError(new Error('boom'), translate);

    expect(result.kind).toBe('unknown');
    expect(result.message).toBe('text:errors.unknown');
  });

  it('ignores a body that is not an object', () => {
    const result = toApiRequestError(httpError(400, 'plain text'), translate);

    expect(result.code).toBeNull();
    expect(result.correlationId).toBeNull();
  });
});
