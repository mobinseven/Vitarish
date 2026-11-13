import type { ParticleFormationId } from "./particleFormations";

/** How the page tells the particle scene which formation belongs where. */
export const particleAnchorAttributes = {
  /** On a section: the formation the field takes while that section is centred in the view. */
  formation: "data-particle-formation",
  /** On an element inside that section: the box the formation is fitted into. Without one, the formation fills the view. */
  slot: "data-particle-slot",
} as const;

export const particleAnchor = (formation: ParticleFormationId) => ({ [particleAnchorAttributes.formation]: formation });

export const particleSlot = { [particleAnchorAttributes.slot]: "" } as const;
