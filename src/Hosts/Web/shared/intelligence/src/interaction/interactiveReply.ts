/**
 * The view model of an agent's interactive reply (`AgentInteractiveReplyResponse` from
 * Intelligence.Contracts). Each app's generated client carries the wire shape; apps convert it with
 * `toInteractiveReply` so every app renders replies through the same components.
 */
export interface InteractiveReply {
  text: string;
  question: InteractiveQuestion | null;
  references: readonly string[];
  nextAction: string | null;
}

export interface InteractiveQuestion {
  prompt: string;
  options: readonly InteractiveOption[];
  maxSelections: number;
}

export interface InteractiveOption {
  key: string;
  label: string;
}

/** A referenced item as the app resolves it: its label, its status label, and the status tone. */
export interface InteractiveReference {
  key: string;
  label: string;
  statusLabel?: string;
  tone?: "success" | "warning" | "default";
}

/** The wire shape as generated clients type it; integers may arrive as strings. */
export interface InteractiveReplyWire {
  text: string;
  question?: {
    prompt: string;
    options: readonly InteractiveOption[];
    maxSelections: number | string;
  } | null;
  references?: readonly string[];
  nextAction?: string | null;
}

export function toInteractiveReply(wire: InteractiveReplyWire): InteractiveReply {
  const question = wire.question && wire.question.options.length > 0
    ? {
        prompt: wire.question.prompt,
        options: wire.question.options,
        maxSelections: Math.min(Math.max(Number(wire.question.maxSelections) || 1, 1), wire.question.options.length),
      }
    : null;

  return {
    text: wire.text,
    question,
    references: wire.references ?? [],
    nextAction: wire.nextAction ?? null,
  };
}
