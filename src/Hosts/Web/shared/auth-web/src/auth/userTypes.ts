/**
 * Narrow facade over generated users types.
 * Re-exports only the types consumed by account-web and sea-web.
 */
export type { UserProfileResponse, UserSummaryResponse, WebAuthnCredentialDto } from "../api/generated/users/model";
