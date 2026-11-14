import type { NavigationTransitionContext } from "./transitionContext";

export const navigationTransitionStates = {
  Idle: "idle", Pending: "pending", Departing: "departing", Bootstrapping: "bootstrapping", Resolving: "resolving",
  Ready: "ready", InteractionRequired: "interaction-required", Failed: "failed", Cancelled: "cancelled",
} as const;
export type NavigationTransitionState = typeof navigationTransitionStates[keyof typeof navigationTransitionStates];
export type NavigationTransitionOutcome = typeof navigationTransitionStates.Ready | typeof navigationTransitionStates.InteractionRequired | typeof navigationTransitionStates.Failed | typeof navigationTransitionStates.Cancelled;

export const navigationTransitionDocument = {
  surfaceId: "navigation-transition",
  statusId: "navigation-transition-status",
  recoveryId: "navigation-transition-recovery",
  contentAttribute: "data-navigation-content",
  stateAttribute: "data-navigation-state",
  modeAttribute: "data-navigation-mode",
  localeAttribute: "data-navigation-locale",
  slowAttribute: "data-navigation-slow",
  runtime: "__vitarishNavigationTransition",
  shellParticipant: "shell",
  navigationParticipant: "navigation",
} as const;

export interface NavigationTransitionRuntime {
  readonly context: NavigationTransitionContext;
  readonly state: NavigationTransitionState;
  readonly signal: AbortSignal;
  hold: (participant: string) => void;
  release: (participant: string) => void;
  finish: (outcome: NavigationTransitionOutcome) => void;
  departing: () => void;
  active: () => boolean;
  start: (context: NavigationTransitionContext) => void;
}
