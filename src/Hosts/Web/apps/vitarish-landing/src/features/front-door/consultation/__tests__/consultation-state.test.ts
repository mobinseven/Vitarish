import { describe, expect, it } from "vitest";
import type { LandingConsultationResponse } from "@vitarish/platform";
import {
  consultationReducer,
  initialConsultation,
  matchedFeatures,
  proofsFor,
  type ConsultationEvent,
} from "../consultation-state";

const consultation: LandingConsultationResponse = {
  kind: { key: "kind", maxSelections: 1, options: [{ key: "dental", featureKeys: [], proof: null }] },
  booking: { key: "booking", maxSelections: 1, options: [{ key: "phone", featureKeys: [], proof: null }] },
  pains: {
    key: "pains",
    maxSelections: 3,
    options: [
      { key: "no-shows", featureKeys: ["reminders", "check-in-sms"], proof: "NoShowCost" },
      { key: "phone-load", featureKeys: ["online-booking", "reminders"], proof: "BookingPreview" },
    ],
  },
};

function run(...events: ConsultationEvent[]) {
  return events.reduce(consultationReducer, initialConsultation);
}

describe("consultationReducer", () => {
  it("walks from the first question to the open step, one answer at a time", () => {
    const state = run(
      { type: "start" },
      { type: "answer", question: "kind", options: ["dental"] },
      { type: "answer", question: "booking", options: ["phone"] },
      { type: "answer", question: "pains", options: ["phone-load"] },
      { type: "confirm" },
    );

    expect(state.step).toBe("open");
    expect(state.answers).toEqual({ kind: ["dental"], booking: ["phone"], pains: ["phone-load"] });
    expect(state.thread.map((entry) => entry.type)).toEqual([
      "opening", "question", "answer", "question", "answer", "question", "answer", "proofs", "confirmed", "ready",
    ]);
  });

  it("ignores a second answer to a question that was already answered", () => {
    const answered = run({ type: "start" }, { type: "answer", question: "kind", options: ["dental"] });

    const again = consultationReducer(answered, { type: "answer", question: "kind", options: ["physiotherapy"] });

    expect(again).toBe(answered);
  });

  it("cannot reach the open step before the proof step", () => {
    const state = run({ type: "start" }, { type: "confirm" });

    expect(state.step).toBe("clinic");
  });

  it("keeps free conversation in the same thread and clears everything on restart", () => {
    const state = run(
      { type: "start" },
      { type: "said", text: "Do you support insurance?" },
      { type: "replied", reply: { text: "Tamin is in progress.", question: null, references: ["tamin-insurance"], nextAction: null } },
    );
    expect(state.thread.slice(-2).map((entry) => entry.type)).toEqual(["user", "reply"]);
    expect(new Set(state.thread.map((entry) => entry.id)).size).toBe(state.thread.length);

    expect(consultationReducer(state, { type: "restart" })).toBe(initialConsultation);
  });
});

describe("signing in to open the clinic", () => {
  it("shows one sign-in card however often it is asked for, then confirms once", () => {
    const asked = run({ type: "start" }, { type: "requestSignIn", reason: "clinic" }, { type: "requestSignIn", reason: "voice" });
    expect(asked.thread.filter((entry) => entry.type === "signIn")).toHaveLength(1);

    const signedIn = run({ type: "start" }, { type: "requestSignIn", reason: "voice" }, { type: "signedIn" }, { type: "signedIn" });
    expect(signedIn.thread.slice(-2)).toMatchObject([{ type: "signIn", reason: "voice" }, { type: "signedIn", reason: "voice" }]);
  });

  it("ignores a sign-in confirmation that no card asked for", () => {
    const state = run({ type: "start" }, { type: "signedIn" });

    expect(state.thread.some((entry) => entry.type === "signedIn")).toBe(false);
  });
});

describe("matching", () => {
  it("lists matched features in the order of the owner's pains without repeats", () => {
    expect(matchedFeatures(consultation, ["phone-load", "no-shows"])).toEqual(["online-booking", "reminders", "check-in-sms"]);
  });

  it("leads the examples with the first pain the owner picked", () => {
    expect(proofsFor(consultation, ["phone-load", "no-shows"])).toEqual(["BookingPreview", "NoShowCost"]);
  });
});
