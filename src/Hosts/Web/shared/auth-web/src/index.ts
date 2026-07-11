// Session
export { resolveUserDisplayProfile } from "./session/userDisplayProfile";
export type { UserDisplayProfile } from "./session/userDisplayProfile";

// Hooks
export { useAuthSession } from "./hooks/useAuthSession";
export { useTenantAuthorization } from "./hooks/useTenantAuthorization";
export type { TenantRole } from "./hooks/useTenantAuthorization";
export { useOtpCodeCapture } from "./hooks/useOtpCodeCapture";
export { useOtpExpiry } from "./hooks/useOtpExpiry";
export { getRequestedReturnUrl } from "./hooks/requestedReturnUrl";

// Auth utilities
export { parseAuthError, getErrorMessage } from "./auth/errors";
export type { AuthError, AuthenticationErrorCode } from "./auth/errors";
export { normalizeOtpCode } from "./auth/otpCode";
export { getOtpExpiryPresentation } from "./auth/otpExpiry";
export type { OtpExpiryPresentation } from "./auth/otpExpiry";

// Authentication types (via narrow facade)
export type {
  CurrentAuthenticationUserDto,
  AuthenticationRedirectResultDto,
  AuthenticationChallengeDto,
  AuthenticationSessionDto,
  LogoutResultDto,
  LogoutContextDto,
} from "./auth/authenticationTypes";

// Users types (via narrow facade)
export type { UserProfileResponse, UserSummaryResponse, WebAuthnCredentialDto } from "./auth/userTypes";

// Generated authentication hooks
export {
  getAuthenticationSessionGetQueryKey,
  useAuthenticationLoginPassword,
  useAuthenticationLoginPhoneBegin,
  useAuthenticationLoginPhoneComplete,
  useAuthenticationLoginEmailBegin,
  useAuthenticationLoginEmailComplete,
  useAuthenticationLoginPasskeyBegin,
  useAuthenticationLoginPasskeyComplete,
  useAuthenticationLogoutPost,
  useAuthenticationLogoutContext,
} from "./api/generated/authentication/authentication";

// Generated users hooks
export {
  useUsersGetProfile,
  useUsersUpdateProfile,
  useUsersGetTheme,
  useUsersSetTheme,
  useUsersGetCulture,
  useUsersSetCulture,
  useUsersListWebAuthnCredentials,
  useUsersBeginWebAuthnRegister,
  useUsersCompleteWebAuthnRegister,
  useUsersDeleteWebAuthnCredential,
  usersSearchUsers,
  getUsersGetProfileQueryKey,
  getUsersGetCultureQueryKey,
  getUsersGetThemeQueryKey,
  getUsersListWebAuthnCredentialsQueryKey,
} from "./api/generated/users/users";
