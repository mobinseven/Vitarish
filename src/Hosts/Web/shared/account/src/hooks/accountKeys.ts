import {
  getUsersGetProfileQueryKey,
  getUsersGetCultureQueryKey,
  getUsersListWebAuthnCredentialsQueryKey,
} from "@vitarish/auth";

export const accountKeys = {
  profile: (userId: string) => getUsersGetProfileQueryKey(userId),
  culture: (userId: string) => getUsersGetCultureQueryKey(userId),
  webauthn: (userId: string) => getUsersListWebAuthnCredentialsQueryKey(userId),
};
