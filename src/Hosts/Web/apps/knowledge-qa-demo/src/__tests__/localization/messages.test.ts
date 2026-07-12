import { describe, expect, it } from "vitest";
import { defaultLocale, getMessages } from "@/localization";

describe("Knowledge QA demo localization catalog", () => {
  it("provides the default QA action labels", () => {
    const messages = getMessages(defaultLocale);

    expect(messages.knowledge.qa.askAction).toBe("Ask");
    expect(messages.knowledge.qa.newQuestionAction).toBe("New question");
  });

  it("provides landing and authentication labels", () => {
    const messages = getMessages(defaultLocale);

    expect(messages.landing.loginAction).toBe("Log in");
    expect(messages.auth.login.title).toBe("Log in to Knowledge QA");
    expect(messages.auth.guard.loading).toBe("Checking your session...");
  });
});
