/** Centralized route definitions for the Platform Admin app.
 *  Single source of truth for all navigation paths. */
export const Routes = {
  /** Root path — session-aware redirect */
  Root: "/",

  /** Public login routes */
  Login: {
    Index: "/login",
    Password: "/login/password",
    Phone: "/login/phone",
    Email: "/login/email",
    Passkey: "/login/passkey",
  },
  Logout: "/logout",

  /** Admin dashboard */
  Dashboard: "/",

  /** Users administration */
  Users: {
    Index: "/users",
    Detail: (userId: string) => `/users/${userId}` as const,
  },

  /** Owners administration */
  Owners: {
    Index: "/owners",
    Detail: (ownerId: string) => `/owners/${ownerId}` as const,
  },

  /** Tenants administration */
  Tenants: {
    Index: "/tenants",
    Detail: (tenantId: string) => `/tenants/${tenantId}` as const,
  },

  /** Operations */
  Operations: {
    Health: "/operations/health",
    Search: "/operations/search",
    Communications: "/operations/communications",
  },

  /** Audit */
  Audit: "/audit",
} as const;
