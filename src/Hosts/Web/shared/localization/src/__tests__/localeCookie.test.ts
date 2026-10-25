import { describe, expect, it } from "vitest";
import {
  localeCookieDomainEnvironmentKey,
  parseLocaleCookie,
  readLocaleCookieDomain,
  serializeLocaleCookie,
} from "../localeCookie";

describe("locale cookie domain", () => {
  it("shares the cookie across the deployment's hosts when a domain is configured", () => {
    const domain = readLocaleCookieDomain({ [localeCookieDomainEnvironmentKey]: " Vitarish.com " });

    expect(domain).toBe("vitarish.com");
    expect(serializeLocaleCookie("fa", domain)).toBe("vitarish.locale=fa; Path=/; Domain=vitarish.com; SameSite=Lax; Max-Age=31536000");
  });

  it("keeps the cookie on the current host when no domain is configured", () => {
    expect(readLocaleCookieDomain({})).toBeNull();
    expect(serializeLocaleCookie("en", null)).toBe("vitarish.locale=en; Path=/; SameSite=Lax; Max-Age=31536000");
  });

  it("refuses a configured value that is not a plain domain name, so it cannot add cookie attributes", () => {
    expect(readLocaleCookieDomain({ [localeCookieDomainEnvironmentKey]: "vitarish.com; Secure=no" })).toBeNull();
    expect(readLocaleCookieDomain({ [localeCookieDomainEnvironmentKey]: ".vitarish.com" })).toBeNull();
  });

  it("reads back the locale written for the shared domain", () => {
    const cookie = serializeLocaleCookie("fa", "vitarish.com").split(";")[0];

    expect(parseLocaleCookie(`theme=dark; ${cookie}`)).toBe("fa");
  });
});
