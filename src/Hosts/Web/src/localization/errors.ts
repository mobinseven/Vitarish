import type { MessageKey, Translate } from "./messageTypes";

export interface CodeBearingError { code?: string | null; status?: number; }

const apiErrorCodeMessages: Record<string, MessageKey> = {
  Unauthorized: "errors.unauthorized",
  Forbidden: "errors.forbidden",
  NotFound: "errors.notFound",
  ValidationFailed: "errors.validation",
};

export function getApiErrorMessageKey(error: unknown): MessageKey {
  const code = typeof error === "object" && error !== null && "code" in error ? String((error as CodeBearingError).code ?? "") : "";
  return apiErrorCodeMessages[code] ?? "errors.generic";
}

export function localizeApiError(error: unknown, t: Translate): string {
  return t(getApiErrorMessageKey(error));
}
