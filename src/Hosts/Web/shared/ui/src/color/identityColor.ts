/**
 * Ordered categorical palette for telling records or options apart; a slot
 * carries no meaning. Earlier slots are the most distinct from each other, so
 * small sets should use the first slots. The hues are owned by `hue-roles.css`.
 */
export const identitySlots = [0, 1, 2, 3, 4, 5, 6, 7] as const;

export type IdentitySlot = (typeof identitySlots)[number];

/**
 * Class name that sets an element's tint to an identity slot's color. The
 * element and its descendants paint with the same `tint` utilities as a
 * business context.
 */
export function identityColorClassName(slot: IdentitySlot): string {
  return `vitarish-tint vitarish-identity--${slot}`;
}

/** Stable identity slot for a record ID, so a record keeps its color everywhere. */
export function identitySlotForId(id: string): IdentitySlot {
  let hash = 0x811c9dc5;
  for (let index = 0; index < id.length; index++) {
    hash = Math.imul(hash ^ id.charCodeAt(index), 0x01000193);
  }
  return identitySlots[(hash >>> 0) % identitySlots.length];
}
