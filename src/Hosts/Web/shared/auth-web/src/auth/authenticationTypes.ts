/**
 * Narrow facade over generated authentication types.
 * Re-exports only the types consumed by auth-web, account-web, and test handlers.
 */
export type { CurrentAuthenticationUserDto } from "../api/generated/authentication/model";
export type {
  AuthenticationRedirectResultDto,
  AuthenticationChallengeDto,
  AuthenticationSessionDto,
  LogoutResultDto,
  LogoutContextDto,
} from "../api/generated/authentication/model";
