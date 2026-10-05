// Session
export { resolveUserDisplayProfile } from "./session/userDisplayProfile";
export type { UserDisplayProfile } from "./session/userDisplayProfile";

// Hooks
export { useAuthSession } from "./hooks/useAuthSession";
export { useTenantAuthorization } from "./hooks/useTenantAuthorization";
export { usePlatformAuthorization } from "./hooks/usePlatformAuthorization";
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
export type { UserProfileResponse, UserSummaryResponse, WebAuthnCredentialResponse } from "./auth/userTypes";

// Generated authentication hooks
export {
  getAuthenticationSessionPolicyGetQueryKey,
  useAuthenticationSessionPolicyGet,
  useAuthenticationSessionPolicyPut,
  getAuthenticationAuthenticationPolicyGetQueryKey,
  useAuthenticationAuthenticationPolicyGet,
  useAuthenticationAuthenticationPolicyPut,
  useAuthenticationUserInactivityPolicyGet,
  useAuthenticationUserInactivityPolicyPut,
  useAuthenticationUserSessionsRevoke,
  getAuthenticationSessionGetQueryKey,
  useAuthenticationProfileGet as useAuthenticationProfile,
  useAuthenticationLoginPassword,
  useAuthenticationLoginPhoneBegin,
  useAuthenticationLoginPhoneComplete,
  useAuthenticationLoginEmailBegin,
  useAuthenticationLoginEmailComplete,
  useAuthenticationLoginPasskeyBegin,
  useAuthenticationLoginPasskeyComplete,
  useAuthenticationPasswordRecoveryBegin,
  useAuthenticationPasswordRecoveryComplete,
  useAuthenticationLogoutPost,
  useAuthenticationLogoutContext,
  useAuthenticationAccessHistoryGet as useAuthenticationAccessHistory,
  useAuthenticationSessionNoticesGet as useAuthenticationSessionNotices,
} from "./api/generated/authentication/authentication";

export type {
  AuthenticationAccessHistoryDto,
  AuthenticationSessionNoticeDto,
  ChangeSecuritySessionPolicyRequest,
  SecuritySessionPolicyDto,
  ChangeSecurityAuthenticationPolicyRequest,
  SecurityAuthenticationPolicyDto,
  ChangeUserInactivityTimeoutRequest,
  UserInactivityTimeoutDto,
  UserSessionsRevocationDto,
} from "./api/generated/authentication/model";
export { AuthenticationSessionNoticeType } from "./api/generated/authentication/model";

// Generated users hooks
export {
  useUsersGetProfile,
  useUsersUpdateProfile,
  useUsersGetCulture,
  useUsersSetCulture,
  useUsersListWebAuthnCredentials,
  useUsersBeginWebAuthnRegister,
  useUsersCompleteWebAuthnRegister,
  useUsersDeleteWebAuthnCredential,
  usersSearchUsers,
  useUsersSearchUsers,
  useUsersCreateWithEmail,
  useUsersCreateWithPhone,
  useUsersAdministrationCreateWithEmail,
  useUsersAdministrationCreateWithPhone,
  useUsersAdministrationListTenantRoles,
  useUsersAdministrationGetUserSecurity,
  useUsersAdministrationUpdateUser,
  useUsersAddRole,
  useUsersAdministrationGrantTenantRole,
  useUsersAdministrationRevokeTenantRole,
  getUsersAdministrationListTenantRolesQueryKey,
  useUsersListTenantMembers,
  useUsersProvisionTenantMember,
  useUsersRemoveTenantMember,
  getUsersGetProfileQueryKey,
  getUsersGetCultureQueryKey,
  getUsersListWebAuthnCredentialsQueryKey,
} from "./api/generated/users/users";

export type {
  ChangeTenantRoleRequest,
  TenantRoleMembershipResponse,
  UsersListTenantMembersParams,
  ProvisionTenantMemberRequest,
  ProvisionTenantMemberResponse,
  TenantMemberResponse,
  UserSecurityDetailsResponse,
  UserSecurityEventResponse,
  UserSecurityHistoryResponse,
  UpdateManagedUserRequest,
  AddUserToRoleRequest,
} from "./api/generated/users/model";

// Components
export { AuthMethodSelector } from "./components/AuthMethodSelector";
export type { AuthMethodRoute } from "./components/AuthMethodSelector";
export { AuthorizationGuard } from "./components/AuthorizationGuard";
export { EmailOtpForm } from "./components/EmailOtpForm";
export { LoginForm } from "./components/LoginForm";
export { AuthenticationWebRoutes } from "./routes";
export { LoginPageCard } from "./components/LoginPageCard";
export { PasswordRecoveryForm } from "./components/PasswordRecoveryForm";
export { PasswordRecoveryPage } from "./components/PasswordRecoveryPage";
export { OtpCodeInput } from "./components/OtpCodeInput";
export { OtpVerificationStep } from "./components/OtpVerificationStep";
export { PasskeyButton } from "./components/PasskeyButton";
export { PhoneOtpForm } from "./components/PhoneOtpForm";
export { AuthenticatedRedirectBoundary, SessionRedirect, resolveSessionRedirectTarget } from "./components/SessionRedirect";

// Workflow hooks
export { usePasswordLogin } from "./hooks/usePasswordLogin";
export { usePasswordRecovery } from "./hooks/usePasswordRecovery";
export { usePhoneLogin } from "./hooks/usePhoneLogin";
export type { PhoneLoginStep } from "./hooks/usePhoneLogin";
export { useEmailLogin } from "./hooks/useEmailLogin";
export type { EmailLoginStep } from "./hooks/useEmailLogin";
export { usePasskeyLogin } from "./hooks/usePasskeyLogin";
export { useLogout } from "./hooks/useLogout";
export type { LogoutContextStatus } from "./hooks/useLogout";
