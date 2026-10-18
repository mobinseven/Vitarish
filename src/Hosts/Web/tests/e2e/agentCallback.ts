import { createHmac } from "node:crypto";

/** The agent runtime's tool callback on the Main host. */
export const agentToolCallbackPath = "/api/modules/intelligence/internal/agent/tools/execute";

export function base64UrlHmac(key: string, payload: string): string {
  return createHmac("sha256", key).update(payload, "utf8").digest("base64url");
}

/** The canonical text the agent runtime signs; the credential leaves out the callback fields, the callback signature covers them. */
export function agentEnvelopeCanonical(envelope: Record<string, unknown>, includeCallbackFields: boolean): string {
  const permissions = [...(envelope.permissions as string[])].sort();
  return [
    envelope.version, envelope.executionId, envelope.agentKey, envelope.tenantId, envelope.practiceId,
    envelope.subjectKind, envelope.subjectId, envelope.authenticatedUserId, envelope.conversationId,
    includeCallbackFields ? envelope.toolCallId : "", includeCallbackFields ? envelope.toolName : "",
    envelope.credentialVersion, new Date(envelope.expiresAt as string).getTime(), envelope.correlationId,
    envelope.role, envelope.locale, envelope.timeZone, envelope.calendar, envelope.currentUtc,
    envelope.currentLocal, JSON.stringify(permissions), envelope.patientNationalId ?? "",
  ].join("\n");
}

/** Signs an execution envelope as the agent runtime does, returning the credential header value. */
export function signAgentCallback(sharedSecret: string, envelope: Record<string, unknown>): string {
  const expirySeconds = Math.floor(new Date(envelope.expiresAt as string).getTime() / 1000);
  const credential = `${expirySeconds}.${base64UrlHmac(sharedSecret, agentEnvelopeCanonical(envelope, false))}`;
  envelope.callbackSignature = base64UrlHmac(credential, agentEnvelopeCanonical(envelope, true));
  return credential;
}
