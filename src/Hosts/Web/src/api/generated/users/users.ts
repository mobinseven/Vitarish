import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/api/client/apiFetch";
import type {
  UserProfileResponse,
  UpdateUserProfileRequest,
  SetUserCultureRequest,
  SetUserThemeRequest,
  WebAuthnCredentialDto,
} from "./model";

export const usersKeys = {
  all: ["users"] as const,
  profile: (userId: string) => [...usersKeys.all, "profile", userId] as const,
  culture: (userId: string) => [...usersKeys.all, "culture", userId] as const,
  theme: (userId: string) => [...usersKeys.all, "theme", userId] as const,
  webauthn: (userId: string) => [...usersKeys.all, "webauthn", userId] as const,
  tokens: (userId: string) => [...usersKeys.all, "tokens", userId] as const,
};

export function useUserProfileQuery(userId: string) {
  return useQuery({
    queryKey: usersKeys.profile(userId),
    queryFn: () =>
      apiFetch<UserProfileResponse>(
        `/api/modules/users/${encodeURIComponent(userId)}/profile`
      ),
    enabled: !!userId,
  });
}

export function useUpdateUserProfileMutation(userId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateUserProfileRequest) =>
      apiFetch<{ succeeded: boolean }>(
        `/api/modules/users/${encodeURIComponent(userId)}/profile`,
        { method: "PUT", body: JSON.stringify(data) }
      ),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: usersKeys.profile(userId) });
    },
  });
}

export function useUserCultureQuery(userId: string) {
  return useQuery({
    queryKey: usersKeys.culture(userId),
    queryFn: () =>
      apiFetch<string | null>(
        `/api/modules/users/${encodeURIComponent(userId)}/culture`
      ),
    enabled: !!userId,
  });
}

export function useSetUserCultureMutation(userId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: SetUserCultureRequest) =>
      apiFetch<{ succeeded: boolean }>(
        `/api/modules/users/${encodeURIComponent(userId)}/culture`,
        { method: "PUT", body: JSON.stringify(data) }
      ),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: usersKeys.culture(userId) });
    },
  });
}

export function useUserThemeQuery(userId: string) {
  return useQuery({
    queryKey: usersKeys.theme(userId),
    queryFn: () =>
      apiFetch<string | null>(
        `/api/modules/users/${encodeURIComponent(userId)}/theme`
      ),
    enabled: !!userId,
  });
}

export function useSetUserThemeMutation(userId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: SetUserThemeRequest) =>
      apiFetch<{ succeeded: boolean }>(
        `/api/modules/users/${encodeURIComponent(userId)}/theme`,
        { method: "PUT", body: JSON.stringify(data) }
      ),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: usersKeys.theme(userId) });
    },
  });
}

export function useWebAuthnCredentialsQuery(userId: string) {
  return useQuery({
    queryKey: usersKeys.webauthn(userId),
    queryFn: () =>
      apiFetch<WebAuthnCredentialDto[]>(
        `/api/modules/users/${encodeURIComponent(userId)}/webauthn`
      ),
    enabled: !!userId,
  });
}

export function useBeginWebAuthnRegisterMutation(userId: string) {
  return useMutation({
    mutationFn: () =>
      apiFetch<string>(
        `/api/modules/users/${encodeURIComponent(userId)}/webauthn/begin-register`,
        { method: "POST", body: JSON.stringify({}) }
      ),
  });
}

export function useCompleteWebAuthnRegisterMutation(userId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (attestationResponseJson: string) =>
      apiFetch<string>(
        `/api/modules/users/${encodeURIComponent(userId)}/webauthn/complete-register`,
        { method: "POST", body: JSON.stringify({ attestationResponseJson }) }
      ),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: usersKeys.webauthn(userId) });
    },
  });
}

export function useDeleteWebAuthnCredentialMutation(userId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (credentialId: string) =>
      apiFetch<boolean>(
        `/api/modules/users/${encodeURIComponent(userId)}/webauthn/${encodeURIComponent(credentialId)}`,
        { method: "DELETE" }
      ),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: usersKeys.webauthn(userId) });
    },
  });
}

export function useUserTokensQuery(userId: string) {
  return useQuery({
    queryKey: usersKeys.tokens(userId),
    queryFn: () =>
      apiFetch<{ tokens: string[] }>(
        `/api/modules/users/${encodeURIComponent(userId)}/tokens`
      ),
    enabled: !!userId,
  });
}

export function useCreateTokenMutation(userId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { lifetimeDays?: number; scopes?: string[] }) =>
      apiFetch<{ token: string }>(
        `/api/modules/users/${encodeURIComponent(userId)}/tokens`,
        { method: "POST", body: JSON.stringify(data) }
      ),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: usersKeys.tokens(userId) });
    },
  });
}

export function useRevokeTokenMutation(userId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (token: string) =>
      apiFetch<boolean>(
        `/api/modules/users/${encodeURIComponent(userId)}/tokens/${encodeURIComponent(token)}`,
        { method: "DELETE" }
      ),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: usersKeys.tokens(userId) });
    },
  });
}
