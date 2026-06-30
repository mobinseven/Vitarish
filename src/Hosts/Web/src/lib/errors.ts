import { ApiError } from "@/api/client/apiFetch";

export type AuthenticationErrorCode =
  | "invalid_input"
  | "invalid_credentials"
  | "challenge_not_found"
  | "challenge_expired"
  | "challenge_already_used"
  | "challenge_attempts_exhausted"
  | "invalid_verification_code"
  | "invalid_challenge_purpose"
  | "locked_out"
  | "not_allowed"
  | "too_many_attempts"
  | "additional_step_required"
  | "unauthorized"
  | "provider_failure"
  | "unknown_credential"
  | "invalid_credential"
  | "logout_context_unavailable";

export interface AuthError {
  code: AuthenticationErrorCode;
  message: string;
  status: number;
}

export function parseAuthError(error: unknown): AuthError {
  if (error instanceof ApiError) {
    return {
      code: (error.code as AuthenticationErrorCode) ?? "invalid_input",
      message: error.message,
      status: error.status,
    };
  }

  if (error instanceof Error) {
    return {
      code: "invalid_input",
      message: error.message,
      status: 500,
    };
  }

  return {
    code: "invalid_input",
    message: "An unexpected error occurred",
    status: 500,
  };
}

export function getErrorMessage(error: AuthError): string {
  const messages: Record<string, string> = {
    invalid_credentials: "Invalid username or password",
    locked_out: "Account is locked. Please try again later",
    challenge_expired: "Code has expired. Please request a new one",
    challenge_attempts_exhausted: "Too many failed attempts. Please start over",
    invalid_verification_code: "Invalid verification code",
    too_many_attempts: "Too many attempts. Please wait before trying again",
    provider_failure: "Service temporarily unavailable. Please try again",
    not_allowed: "You do not have permission to perform this action",
    unauthorized: "Please sign in to continue",
    unknown_credential: "Passkey not recognized",
    invalid_credential: "Passkey verification failed",
    logout_context_unavailable: "Session context unavailable",
  };

  return messages[error.code] ?? error.message;
}
