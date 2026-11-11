/** Login request body (POST /auth/login). */
export interface LoginRequest {
  username: string;
  password: string;
}

/** Access token response. The refresh token is only ever in its HTTP-only cookie. */
export interface AccessTokenResponse {
  accessToken: string;
  expiresInSeconds: number;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
