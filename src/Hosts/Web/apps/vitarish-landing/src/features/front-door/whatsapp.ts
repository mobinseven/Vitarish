/**
 * The runtime environment variable holding the WhatsApp number that answers "talk to a person",
 * in international form (digits only, optionally with a leading +). Read on the server per
 * request, so one image serves every environment; unset means the page offers no WhatsApp link.
 */
export const whatsappNumberEnvironmentKey = "VITARISH_LANDING_WHATSAPP_NUMBER";

const internationalNumber = /^\+?(\d{8,15})$/;

/** The configured WhatsApp number as digits, or null when it is unset or malformed. */
export function readWhatsAppNumber(environment: Record<string, string | undefined>): string | null {
  const match = internationalNumber.exec(environment[whatsappNumberEnvironmentKey]?.trim() ?? "");
  return match ? match[1] : null;
}

/** A link that opens a WhatsApp chat with the number and a message the visitor can still edit before sending. */
export function whatsappChatUrl(number: string, message: string): string {
  return `https://wa.me/${number}?text=${encodeURIComponent(message)}`;
}
