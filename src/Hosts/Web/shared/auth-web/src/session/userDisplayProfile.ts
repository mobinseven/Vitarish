import type { CurrentAuthenticationUserDto } from "../api/generated/authentication/model";

export interface UserDisplayProfile {
  displayLabel: string;
  givenName: string | null;
  familyName: string | null;
  initials: string | null;
  contactLabel: string | null;
}

export function resolveUserDisplayProfile(
  user: CurrentAuthenticationUserDto | null | undefined
): UserDisplayProfile {
  const displayName = normalizeText(user?.displayName);
  const givenName = normalizeText(user?.givenName) ?? resolveGivenName(displayName);
  const familyName = normalizeText(user?.familyName) ?? resolveFamilyName(displayName);
  const email = normalizeText(user?.email);
  const phoneNumber = normalizeText(user?.phoneNumber);
  const emailLocalPart = normalizeText(email?.split("@")[0]);
  const displayLabel = displayName ?? emailLocalPart ?? phoneNumber ?? "friend";
  const isFallback = !user || displayLabel === "friend";

  return {
    displayLabel,
    givenName,
    familyName,
    initials:
      resolveNameInitials(givenName, familyName) ?? resolveInitials(displayLabel, isFallback),
    contactLabel: email ?? phoneNumber,
  };
}

function normalizeText(value: string | null | undefined): string | null {
  const normalized = value?.trim();
  return normalized ? normalized : null;
}

function resolveGivenName(displayName: string | null): string | null {
  return displayName?.split(/\s+/)[0] ?? null;
}

function resolveFamilyName(displayName: string | null): string | null {
  const nameParts = displayName?.split(/\s+/) ?? [];
  return nameParts.length > 1 ? nameParts.slice(1).join(" ") : null;
}

function resolveNameInitials(
  givenName: string | null,
  familyName: string | null
): string | null {
  const initials = [givenName, familyName]
    .map((part) => part?.replace(/[^a-zA-Z]/g, "")[0]?.toUpperCase())
    .filter(Boolean)
    .join("");

  return initials || null;
}

function resolveInitials(displayLabel: string, isFallback: boolean): string | null {
  if (isFallback) {
    return null;
  }

  const initials = displayLabel
    .split(/[\s._-]+/)
    .filter(Boolean)
    .map((part) => part.replace(/[^a-zA-Z]/g, "")[0]?.toUpperCase())
    .filter(Boolean)
    .slice(0, 2)
    .join("");

  return initials || null;
}
