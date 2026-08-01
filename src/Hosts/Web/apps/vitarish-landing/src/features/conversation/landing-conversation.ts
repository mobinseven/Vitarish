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

export async function submitLandingMessage(
  ownerKey: LandingConversationOwner,
  message: string,
  conversationId: string | null,
  signal: AbortSignal,
): Promise<LandingConversationReply> {
  const response = await fetch("/api/apps/landing/conversations", {
    method: "POST",
    credentials: "include",
    signal,
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ ownerKey, message, conversationId }),
  });

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
    `/api/apps/landing/conversations/${encodeURIComponent(conversationId)}?ownerKey=${encodeURIComponent(ownerKey)}`,
    { method: "DELETE", credentials: "include" },
  );

  if (!response.ok && response.status !== 404) {
    throw new LandingConversationError(
      response.status === 429 ? "rate_limited" : "unavailable",
      response.status,
    );
  }
}
