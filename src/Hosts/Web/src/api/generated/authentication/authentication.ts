import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/api/client/apiFetch";
import type {
  PasswordLoginRequest,
  BeginPhoneLoginRequest,
  CompletePhoneLoginRequest,
  BeginEmailLoginRequest,
  CompleteEmailLoginRequest,
  BeginPasskeyAuthenticationRequest,
  CompletePasskeyAuthenticationRequest,
  LogoutRequest,
  AuthenticationRedirectResultDto,
  AuthenticationChallengeDto,
  AuthenticationSessionDto,
  CurrentAuthenticationUserDto,
  LogoutContextDto,
  LogoutResultDto,
} from "./model";

export const authenticationKeys = {
  all: ["authentication"] as const,
  session: () => [...authenticationKeys.all, "session"] as const,
  me: () => [...authenticationKeys.all, "me"] as const,
  logoutContext: (logoutId: string | null) =>
    [...authenticationKeys.all, "logoutContext", logoutId] as const,
};

export function usePasswordLoginMutation() {
  return useMutation({
    mutationFn: (data: PasswordLoginRequest) =>
      apiFetch<AuthenticationRedirectResultDto>(
        "/api/modules/authentication/login/password",
        { method: "POST", body: JSON.stringify(data) }
      ),
  });
}

export function useBeginPhoneLoginMutation() {
  return useMutation({
    mutationFn: (data: BeginPhoneLoginRequest) =>
      apiFetch<AuthenticationChallengeDto>(
        "/api/modules/authentication/login/phone/begin",
        { method: "POST", body: JSON.stringify(data) }
      ),
  });
}

export function useCompletePhoneLoginMutation() {
  return useMutation({
    mutationFn: (data: CompletePhoneLoginRequest) =>
      apiFetch<AuthenticationRedirectResultDto>(
        "/api/modules/authentication/login/phone/complete",
        { method: "POST", body: JSON.stringify(data) }
      ),
  });
}

export function useBeginEmailLoginMutation() {
  return useMutation({
    mutationFn: (data: BeginEmailLoginRequest) =>
      apiFetch<AuthenticationChallengeDto>(
        "/api/modules/authentication/login/email/begin",
        { method: "POST", body: JSON.stringify(data) }
      ),
  });
}

export function useCompleteEmailLoginMutation() {
  return useMutation({
    mutationFn: (data: CompleteEmailLoginRequest) =>
      apiFetch<AuthenticationRedirectResultDto>(
        "/api/modules/authentication/login/email/complete",
        { method: "POST", body: JSON.stringify(data) }
      ),
  });
}

export function useBeginPasskeyLoginMutation() {
  return useMutation({
    mutationFn: (data: BeginPasskeyAuthenticationRequest) =>
      apiFetch<AuthenticationChallengeDto>(
        "/api/modules/authentication/login/passkey/begin",
        { method: "POST", body: JSON.stringify(data) }
      ),
  });
}

export function useCompletePasskeyLoginMutation() {
  return useMutation({
    mutationFn: (data: CompletePasskeyAuthenticationRequest) =>
      apiFetch<AuthenticationRedirectResultDto>(
        "/api/modules/authentication/login/passkey/complete",
        { method: "POST", body: JSON.stringify(data) }
      ),
  });
}

export function useGetAuthenticationSessionQuery() {
  return useQuery({
    queryKey: authenticationKeys.session(),
    queryFn: () =>
      apiFetch<AuthenticationSessionDto>(
        "/api/modules/authentication/session"
      ),
    retry: false,
  });
}

export function useGetCurrentAuthenticationUserQuery() {
  return useQuery({
    queryKey: authenticationKeys.me(),
    queryFn: () =>
      apiFetch<CurrentAuthenticationUserDto>(
        "/api/modules/authentication/me"
      ),
    retry: false,
  });
}

export function useGetLogoutContextQuery(logoutId: string | null) {
  return useQuery({
    queryKey: authenticationKeys.logoutContext(logoutId),
    queryFn: () =>
      apiFetch<LogoutContextDto>(
        `/api/modules/authentication/logout/context${logoutId ? `?logoutId=${encodeURIComponent(logoutId)}` : ""}`
      ),
    enabled: true,
    retry: false,
  });
}

export function useLogoutMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: LogoutRequest) =>
      apiFetch<LogoutResultDto>(
        "/api/modules/authentication/logout",
        { method: "POST", body: JSON.stringify(data) }
      ),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: authenticationKeys.all });
    },
  });
}
