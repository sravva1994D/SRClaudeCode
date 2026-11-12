import { HttpContextToken } from '@angular/common/http';

/**
 * Set on a request whose failure the page handles itself, such as "nothing recorded yet" (404). The error
 * interceptor then skips the snackbar, so the user is not told about an expected answer.
 */
export const SUPPRESS_ERROR_NOTIFICATION = new HttpContextToken<boolean>(() => false);
