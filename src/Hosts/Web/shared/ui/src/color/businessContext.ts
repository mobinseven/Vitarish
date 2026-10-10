/**
 * The part of the business a thing belongs to. Each context keeps one hue in
 * every app, regardless of how navigation groups it, so users learn areas by
 * color. The hues are owned by `hue-roles.css`.
 */
export const businessContexts = [
  /** Patient list, profile, overview, and patient create/edit. */
  "patient-record",
  /** Visits, visit events, procedures, and procedure templates. */
  "visit",
  /** Prescriptions and prescription templates. */
  "prescription",
  /** Reception and admission. */
  "check-in",
  /** Reservations, shifts, agenda, and booking settings. */
  "scheduling",
  /** Reminders and outbound messages. */
  "communication",
  /** Contents and forms. */
  "content",
  /** Services, service types, and service providers. */
  "catalog",
  /** Insurance coverage and payers. */
  "insurance",
  /** Home, settings, and account; carries no hue. */
  "general",
] as const;

export type BusinessContext = (typeof businessContexts)[number];

/**
 * Class name that sets an element's tint to its business context's color.
 * The element and its descendants paint with `bg-tint`, `text-tint`,
 * `bg-tint-soft`, and `text-tint-soft-foreground`.
 */
export function contextColorClassName(context: BusinessContext): string {
  return `vitarish-tint vitarish-context--${context}`;
}
