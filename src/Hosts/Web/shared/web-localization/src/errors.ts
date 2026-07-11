import type { MessageKey, Translate } from "./messageTypes";

export interface CodeBearingError { code?: string | null; status?: number; }

const apiErrorCodeMessages: Record<string, MessageKey> = {
  Unauthorized: "errors.unauthorized",
  Forbidden: "errors.forbidden",
  NotFound: "errors.notFound",
  ValidationFailed: "errors.validation",
};

const apiHttpStatusMessages: Record<number, MessageKey> = {
  400: "errors.validation",
  401: "errors.unauthorized",
  403: "errors.forbidden",
  404: "errors.notFound",
};

export function getApiErrorMessageKey(error: unknown): MessageKey {
  if (typeof error === "object" && error !== null) {
    const err = error as CodeBearingError;
    if (err.code && err.code in apiErrorCodeMessages) {
      return apiErrorCodeMessages[err.code];
    }
    if (err.status && err.status in apiHttpStatusMessages) {
      return apiHttpStatusMessages[err.status];
    }
  }
  return "errors.generic";
}

export function localizeApiError(error: unknown, t: Translate): string {
  return t(getApiErrorMessageKey(error));
}
