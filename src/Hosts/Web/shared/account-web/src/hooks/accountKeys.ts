import {
  getUsersGetProfileQueryKey,
  getUsersGetCultureQueryKey,
  getUsersGetThemeQueryKey,
  getUsersListWebAuthnCredentialsQueryKey,
} from "@vitarish/auth-web";

export const accountKeys = {
  profile: (userId: string) => getUsersGetProfileQueryKey(userId),
  culture: (userId: string) => getUsersGetCultureQueryKey(userId),
  theme: (userId: string) => getUsersGetThemeQueryKey(userId),
  webauthn: (userId: string) => getUsersListWebAuthnCredentialsQueryKey(userId),
};
