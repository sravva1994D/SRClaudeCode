import { HttpErrorResponse } from '@angular/common/http';

export type ApiErrorKind = 'network' | 'server' | 'business' | 'request' | 'unknown';

/** The one error type services receive. Raw HttpClient errors never leave the interceptor (CLAUDE.md 8.6). */
export class ApiRequestError extends Error {
  constructor(
    readonly kind: ApiErrorKind,
    readonly status: number,
    readonly code: string | null,
    message: string,
    readonly correlationId: string | null,
  ) {
    super(message);
    this.name = 'ApiRequestError';
  }
}

interface ErrorBodyFields {
  readonly code: string | null;
  readonly message: string | null;
  readonly correlationId: string | null;
}

/** Maps any thrown value to an ApiRequestError with a user-safe, translated message. */
export function toApiRequestError(
  error: unknown,
  translate: (key: string) => string,
): ApiRequestError {
  if (!(error instanceof HttpErrorResponse)) {
    return new ApiRequestError('unknown', 0, null, translate('errors.unknown'), null);
  }

  if (error.status === 0) {
    return new ApiRequestError('network', 0, null, translate('errors.network'), null);
  }

  const body = readErrorBody(error.error);
  if (error.status >= 500) {
    return new ApiRequestError(
      'server',
      error.status,
      body.code,
      translate('errors.server'),
      body.correlationId,
    );
  }

  const kind: ApiErrorKind = error.status === 422 ? 'business' : 'request';
  const message = body.message ?? translate('errors.request');
  return new ApiRequestError(kind, error.status, body.code, message, body.correlationId);
}

function readErrorBody(value: unknown): ErrorBodyFields {
  if (typeof value !== 'object' || value === null) {
    return { code: null, message: null, correlationId: null };
  }

  return {
    code: readString(value, 'code'),
    message: readString(value, 'message'),
    correlationId: readString(value, 'correlationId'),
  };
}

function readString(source: object, field: string): string | null {
  const value: unknown = Reflect.get(source, field);
  return typeof value === 'string' && value.length > 0 ? value : null;
}
