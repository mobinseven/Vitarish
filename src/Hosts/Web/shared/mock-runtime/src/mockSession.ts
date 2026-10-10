/** Cookie a preview or screenshot run sets to make the mock API answer as a signed-out visitor. */
export const mockSessionCookieName = "vitarish.mock-session";

/** Value of {@link mockSessionCookieName} that selects the signed-out session. */
export const mockAnonymousSessionValue = "anonymous";

/** Browsers withhold the cookie header from fetch requests, so the in-page worker reads the document cookie. */
export function isMockAnonymousSession(cookieHeader: string | null): boolean {
  return (cookieHeader ?? globalThis.document?.cookie ?? "")
    .split(";")
    .some((part) => part.trim() === `${mockSessionCookieName}=${mockAnonymousSessionValue}`);
}
