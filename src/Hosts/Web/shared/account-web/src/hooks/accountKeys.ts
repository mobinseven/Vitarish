import {
  getUsersGetProfileQueryKey,
  getUsersGetCultureQueryKey,
  getUsersListWebAuthnCredentialsQueryKey,
} from "@vitarish/auth-web";

export const accountKeys = {
  profile: (userId: string) => getUsersGetProfileQueryKey(userId),
  culture: (userId: string) => getUsersGetCultureQueryKey(userId),
  webauthn: (userId: string) => getUsersListWebAuthnCredentialsQueryKey(userId),
};
