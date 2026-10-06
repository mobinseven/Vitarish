import { describe, expect, it } from "vitest";
import { authMessageCatalogs } from "../../localization/catalogs";

// Apps merge this catalog one level deep into their own messages:
// `auth: { ...app.auth, ...authMessageCatalogs.<locale>.auth }`. A shared key that
// an app also defines under `auth` would replace the app's whole section.
const appOwnedAuthSections = ["guard", "login", "otp", "passkey", "logout", "errors"] as const;

describe("shared auth message catalogs", () => {
  it.each(Object.keys(authMessageCatalogs) as Array<keyof typeof authMessageCatalogs>)(
    "%s does not replace app-owned auth sections when merged",
    (locale) => {
      const sharedSections = Object.keys(authMessageCatalogs[locale].auth);
      for (const section of appOwnedAuthSections) {
        expect(sharedSections).not.toContain(section);
      }

      const appAuth = { login: { signInAction: "Sign in" } };
      const merged = { ...appAuth, ...authMessageCatalogs[locale].auth } as Record<string, unknown>;
      expect(merged.login).toEqual(appAuth.login);
    },
  );
});

describe("sign-in failure messages", () => {
  it("are defined for every locale with the same keys, and Persian text is Persian", () => {
    const englishKeys = Object.keys(authMessageCatalogs.en.auth.failures).sort();
    expect(englishKeys.length).toBeGreaterThan(0);
    expect(Object.keys(authMessageCatalogs.fa.auth.failures).sort()).toEqual(englishKeys);
    for (const message of Object.values(authMessageCatalogs.fa.auth.failures)) {
      expect(message).toMatch(/[؀-ۿ]/);
    }
  });
});
