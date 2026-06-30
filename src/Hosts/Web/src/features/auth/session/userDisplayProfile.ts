import type { CurrentAuthenticationUserDto } from "@/api/generated/authentication/model";

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
  const email = normalizeText(user?.email);
  const phoneNumber = normalizeText(user?.phoneNumber);
  const emailLocalPart = normalizeText(email?.split("@")[0]);
  const displayLabel = displayName ?? emailLocalPart ?? phoneNumber ?? "friend";

  return {
    displayLabel,
    givenName: resolveGivenName(displayName),
    familyName: resolveFamilyName(displayName),
    initials: resolveInitials(displayLabel),
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

function resolveInitials(displayLabel: string): string | null {
  const initials = displayLabel
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toLocaleUpperCase())
    .join("");

  return initials || null;
}
