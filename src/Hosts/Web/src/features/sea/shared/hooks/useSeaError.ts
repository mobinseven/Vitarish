import type { Translate } from "@/localization";

const seaErrorCodeMessages: Record<number, string> = {
  1: "sea.errors.notFound",
  2: "sea.errors.forbidden",
  3: "sea.errors.conflict",
  4: "sea.errors.timeout",
  5: "sea.errors.network",
};

export function getSeaErrorMessageKey(error: unknown): string {
  if (typeof error === "object" && error !== null && "code" in error) {
    const code = (error as { code: unknown }).code;
    if (typeof code === "number" && code in seaErrorCodeMessages) {
      return seaErrorCodeMessages[code];
    }
  }
  return "sea.errors.notFound";
}

export function localizeSeaError(error: unknown, t: Translate): string {
  return t(getSeaErrorMessageKey(error) as Parameters<Translate>[0]);
}
