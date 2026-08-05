export type LandingConversationOwner =
  | "home"
  | "about"
  | "portfolio"
  | "contact"
  | "explore";

export type LandingConversationReply = {
  conversationId: string;
  message: string;
};

export type LandingConversationFailureCode =
  | "not_found"
  | "rate_limited"
  | "unavailable";

export class LandingConversationError extends Error {
  constructor(
    readonly code: LandingConversationFailureCode,
    readonly status: number,
  ) {
    super(code);
  }
}

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

function apiHeaders(): Headers {
  const headers = new Headers({ "content-type": "application/json" });
  if (typeof window !== "undefined" && window.location.host) {
    headers.set("X-Vitarish-Tenant-Host", window.location.host);
  }
  return headers;
}

export async function submitLandingMessage(
  ownerKey: LandingConversationOwner,
  message: string,
  conversationId: string | null,
  signal: AbortSignal,
): Promise<LandingConversationReply> {
  const response = await fetch(
    API_BASE_URL + "/api/apps/landing/conversations",
    {
      method: "POST",
      credentials: "include",
      signal,
      headers: apiHeaders(),
      body: JSON.stringify({ ownerKey, message, conversationId }),
    },
  );

  if (!response.ok) {
    throw new LandingConversationError(
      response.status === 404
        ? "not_found"
        : response.status === 429
          ? "rate_limited"
          : "unavailable",
      response.status,
    );
  }

  return response.json() as Promise<LandingConversationReply>;
}

export async function deleteLandingConversation(
  ownerKey: LandingConversationOwner,
  conversationId: string,
): Promise<void> {
  const response = await fetch(
    API_BASE_URL +
      "/api/apps/landing/conversations/" +
      encodeURIComponent(conversationId) +
      "?ownerKey=" +
      encodeURIComponent(ownerKey),
    { method: "DELETE", credentials: "include", headers: apiHeaders() },
  );

  if (!response.ok && response.status !== 404) {
    throw new LandingConversationError(
      response.status === 429 ? "rate_limited" : "unavailable",
      response.status,
    );
  }
}
