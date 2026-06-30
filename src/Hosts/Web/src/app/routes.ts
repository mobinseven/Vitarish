/** Centralized route definitions for the React web host.
 *  Equivalent to Blazor's AppRoutes class.
 *  Single source of truth for all navigation paths. */
export const Routes = {
  /** Root path — redirects to /login */
  Root: "/",

  /** Authenticated home dashboard */
  Home: "/home",

  /** Public routes */
  Login: {
    Index: "/login",
    Password: "/login/password",
    Phone: "/login/phone",
    Email: "/login/email",
    Passkey: "/login/passkey",
  },
  Logout: "/logout",

  /** Authenticated account routes */
  Account: {
    Index: "/account",
    Name: "/account/name",
    Email: "/account/email",
    Phone: "/account/phone",
    Password: "/account/password",
    Passkeys: "/account/passkeys",
    Theme: "/account/theme",
    Culture: "/account/culture",
    Consents: "/account/consents",
    ContactConfirm: "/account/contact/confirm",
    ProfileComplete: "/account/profile/complete",
  },

  /** Knowledge routes */
  Knowledge: {
    Index: "/knowledge",
    QA: "/knowledge/qa",
  },

  /** Workspace routes */
  Workspaces: {
    Select: "/workspaces/select",
  },
} as const;
