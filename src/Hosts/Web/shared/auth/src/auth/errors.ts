import { ApiError } from "@vitarish/api-client";

export const authenticationErrorCodes = {
  invalidInput: "invalid_input",
  invalidCredentials: "invalid_credentials",
  challengeNotFound: "challenge_not_found",
  challengeExpired: "challenge_expired",
  challengeAlreadyUsed: "challenge_already_used",
  challengeAttemptsExhausted: "challenge_attempts_exhausted",
  invalidVerificationCode: "invalid_verification_code",
  invalidChallengePurpose: "invalid_challenge_purpose",
  lockedOut: "locked_out",
  notAllowed: "not_allowed",
  tooManyAttempts: "too_many_attempts",
  additionalStepRequired: "additional_step_required",
  unauthorized: "unauthorized",
  providerFailure: "provider_failure",
  unknownCredential: "unknown_credential",
  invalidCredential: "invalid_credential",
  accountDisabled: "account_disabled",
  noAccess: "no_access",
  logoutContextUnavailable: "logout_context_unavailable",
} as const;

export type AuthenticationErrorCode =
  (typeof authenticationErrorCodes)[keyof typeof authenticationErrorCodes];

export interface AuthError {
  code: AuthenticationErrorCode;
  message: string;
  status: number;
  challengeId?: string;
  usesEmailSecondFactor?: boolean;
  maskedDestination?: string;
  challengeExpiresAt?: string;
  remainingAttempts?: number;
}

/** The entered verification code was rejected; the message belongs on the code field. */
export function isVerificationCodeError(code: AuthenticationErrorCode | null | undefined): boolean {
  return code === authenticationErrorCodes.invalidVerificationCode;
}

export function isInvalidCredentialsError(error: unknown): boolean {
  return error instanceof ApiError && error.code === authenticationErrorCodes.invalidCredentials;
}

export function isExpiredOrExhaustedChallenge(code: AuthenticationErrorCode | null | undefined): boolean {
  return code === authenticationErrorCodes.challengeExpired ||
    code === authenticationErrorCodes.challengeAttemptsExhausted;
}

/**
 * The server refused the account itself, so retrying or resending the code cannot
 * succeed; a sign-in surface ends the code step and offers another identity instead.
 */
export function isAccountAccessError(code: AuthenticationErrorCode | null | undefined): boolean {
  return code === authenticationErrorCodes.noAccess ||
    code === authenticationErrorCodes.accountDisabled ||
    code === authenticationErrorCodes.lockedOut;
}

export function parseAuthError(error: unknown): AuthError {
  if (error instanceof ApiError) {
    return {
      code: (error.code as AuthenticationErrorCode) ?? authenticationErrorCodes.invalidInput,
      message: error.message,
      status: error.status,
      challengeId: error.problem.challengeId,
      usesEmailSecondFactor:
        typeof error.problem.usesEmailSecondFactor === "boolean"
          ? error.problem.usesEmailSecondFactor
          : undefined,
      maskedDestination:
        typeof error.problem.maskedDestination === "string"
          ? error.problem.maskedDestination
          : undefined,
      challengeExpiresAt:
        typeof error.problem.challengeExpiresAt === "string"
          ? error.problem.challengeExpiresAt
          : undefined,
      remainingAttempts:
        Number.isSafeInteger(error.problem.remainingAttempts) &&
        (error.problem.remainingAttempts ?? -1) >= 0
          ? error.problem.remainingAttempts
          : undefined,
    };
  }

  if (error instanceof Error) {
    return {
      code: authenticationErrorCodes.invalidInput,
      message: error.message,
      status: 500,
    };
  }

  return {
    code: authenticationErrorCodes.invalidInput,
    message: "An unexpected error occurred",
    status: 500,
  };
}

export function getErrorMessage(error: AuthError): string {
  const messages: Record<string, string> = {
    [authenticationErrorCodes.invalidCredentials]: "Invalid username or password",
    [authenticationErrorCodes.lockedOut]: "Account is locked. Please try again later",
    [authenticationErrorCodes.challengeExpired]: "Code has expired. Please request a new one",
    [authenticationErrorCodes.challengeAttemptsExhausted]: "Too many failed attempts. Please start over",
    [authenticationErrorCodes.invalidVerificationCode]: "Invalid verification code",
    [authenticationErrorCodes.tooManyAttempts]: "Too many attempts. Please wait before trying again",
    [authenticationErrorCodes.providerFailure]: "Service temporarily unavailable. Please try again",
    [authenticationErrorCodes.notAllowed]: "You do not have permission to perform this action",
    [authenticationErrorCodes.unauthorized]: "Please sign in to continue",
    [authenticationErrorCodes.unknownCredential]: "Passkey not recognized",
    [authenticationErrorCodes.invalidCredential]: "Passkey verification failed",
    [authenticationErrorCodes.accountDisabled]: "This account is disabled. Contact a platform administrator.",
    [authenticationErrorCodes.noAccess]: "This account has no access level. Contact a platform administrator.",
    [authenticationErrorCodes.logoutContextUnavailable]: "Session context unavailable",
  };

  return messages[error.code] ?? error.message;
}
