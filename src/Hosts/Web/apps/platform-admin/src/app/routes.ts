/** The platform-admin application's route source of truth. */
export const Routes = {
  Root: "/",
  Login: "/login",
  Users: { Index: "/users", New: "/users/new", Detail: (userId: string) => `/users/${userId}` as const },
  Owners: { Index: "/owners", New: "/owners/new", Detail: (ownerId: string) => `/owners/${ownerId}` as const },
  Tenants: { Index: "/tenants", New: "/tenants/new", Detail: (tenantId: string) => `/tenants/${tenantId}` as const, Edit: (tenantId: string) => `/tenants/${tenantId}/edit` as const, IndicatorEdit: (tenantId: string, indicatorId: string) => `/tenants/${tenantId}/indicators/${indicatorId}/edit` as const },
  Audit: "/audit",
  Settings: {
    Index: "/settings",
    Landing: "/settings/landing",
    LocaleNotifications: "/settings/locale-notifications",
    LocaleNotification: (locale: string) => `/settings/locale-notifications/${locale}` as const,
  },
} as const;
