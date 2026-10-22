import type { InteractiveReply } from "@vitarish/intelligence";
import type {
  LandingConsultationResponse,
  LandingProofKind,
} from "@vitarish/platform";

/** A consultation question, named by its property in the server-owned consultation. */
export type ConsultationQuestionKey = keyof LandingConsultationResponse;

export type ConsultationStep = "clinic" | "day" | "proof" | "open";

export const consultationSteps: readonly ConsultationStep[] = ["clinic", "day", "proof", "open"];

export type ThreadEntry =
  | { id: number; type: "opening" }
  | { id: number; type: "question"; question: ConsultationQuestionKey }
  | { id: number; type: "answer"; question: ConsultationQuestionKey; options: readonly string[] }
  | { id: number; type: "proofs" }
  | { id: number; type: "confirmed" }
  | { id: number; type: "ready" }
  | { id: number; type: "signIn" }
  | { id: number; type: "signedIn" }
  | { id: number; type: "user"; text: string }
  | { id: number; type: "reply"; reply: InteractiveReply };

export interface ConsultationState {
  step: ConsultationStep | null;
  answers: Record<ConsultationQuestionKey, readonly string[]>;
  thread: readonly ThreadEntry[];
  nextId: number;
}

export type ConsultationEvent =
  | { type: "start" }
  | { type: "answer"; question: ConsultationQuestionKey; options: readonly string[] }
  | { type: "confirm" }
  | { type: "requestSignIn" }
  | { type: "signedIn" }
  | { type: "said"; text: string }
  | { type: "replied"; reply: InteractiveReply }
  | { type: "restart" };

export const initialConsultation: ConsultationState = {
  step: null,
  answers: { kind: [], booking: [], pains: [] },
  thread: [],
  nextId: 1,
};

type NewEntry = ThreadEntry extends infer Entry ? (Entry extends ThreadEntry ? Omit<Entry, "id"> : never) : never;

function append(state: ConsultationState, ...entries: NewEntry[]): ConsultationState {
  let nextId = state.nextId;
  const added = entries.map((entry) => ({ ...entry, id: nextId++ }) as ThreadEntry);
  return { ...state, thread: [...state.thread, ...added], nextId };
}

/** The question that follows each answer, or the step the consultation moves to. */
const after: Record<ConsultationQuestionKey, NewEntry & { step: ConsultationStep }> = {
  kind: { type: "question", question: "booking", step: "clinic" },
  booking: { type: "question", question: "pains", step: "day" },
  pains: { type: "proofs", step: "proof" },
};

export function consultationReducer(state: ConsultationState, event: ConsultationEvent): ConsultationState {
  switch (event.type) {
    case "start":
      if (state.step !== null) return state;
      return { ...append(state, { type: "opening" }, { type: "question", question: "kind" }), step: "clinic" };
    case "answer": {
      if (state.answers[event.question].length > 0 || event.options.length === 0) return state;
      const { step, ...next } = after[event.question];
      const answered = append(state, { type: "answer", question: event.question, options: event.options }, next as NewEntry);
      return { ...answered, step, answers: { ...state.answers, [event.question]: event.options } };
    }
    case "confirm":
      if (state.step !== "proof") return state;
      return { ...append(state, { type: "confirmed" }, { type: "ready" }), step: "open" };
    case "requestSignIn":
      // One sign-in card at a time; a second request while it is open changes nothing.
      if (state.thread.some((entry) => entry.type === "signIn") && !state.thread.some((entry) => entry.type === "signedIn")) return state;
      return append(state, { type: "signIn" });
    case "signedIn":
      if (!state.thread.some((entry) => entry.type === "signIn") || state.thread.some((entry) => entry.type === "signedIn")) return state;
      return append(state, { type: "signedIn" });
    case "said":
      return append(state, { type: "user", text: event.text });
    case "replied":
      return append(state, { type: "reply", reply: event.reply });
    case "restart":
      return initialConsultation;
  }
}

/** Features matched to the owner's pains, in the order they picked them, without repeats. */
export function matchedFeatures(consultation: LandingConsultationResponse, pains: readonly string[]): string[] {
  const keys = pains.flatMap((pain) => consultation.pains.options.find((option) => option.key === pain)?.featureKeys ?? []);
  return [...new Set(keys)];
}

/** The examples to show, led by the first pain the owner picked. */
export function proofsFor(consultation: LandingConsultationResponse, pains: readonly string[]): LandingProofKind[] {
  const kinds = pains.flatMap((pain) => {
    const proof = consultation.pains.options.find((option) => option.key === pain)?.proof;
    return proof ? [proof] : [];
  });
  return [...new Set(kinds)];
}
