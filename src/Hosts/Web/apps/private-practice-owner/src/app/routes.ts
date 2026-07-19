/** Centralized route definitions for the React web host.
 *  Equivalent to Blazor's AppRoutes class.
 *  Single source of truth for all navigation paths. */
export const Routes = {
  /** Root path — session-aware redirect to /home or /login */
  Root: "/",

  /** Authenticated home dashboard */
  Home: "/home",

  /** Public authentication routes */
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

  /** Private-practice clinic owner routes */
  Clinic: {
    Index: "/home",
    CheckIn: "/check-in",
    Patients: {
      Index: "/patients",
      New: "/patients/new",
      Detail: (patientId: string) => `/patients/${patientId}` as const,
      Edit: (patientId: string) => `/patients/${patientId}/edit` as const,
      Procedure: (patientId: string, procedureId: string) =>
        `/patients/${patientId}/procedures/${procedureId}` as const,
      ProcedureEvent: (patientId: string, procedureId: string, eventId: string) =>
        `/patients/${patientId}/procedures/${procedureId}/events/${eventId}` as const,
    },
    Settings: {
      Index: "/settings",
      Users: "/settings/users",
      Statistics: "/settings/statistics",
    },
    Reminders: "/reminders",
    Appointments: {
      Settings: "/appointments/settings",
      Reservations: {
        Index: "/appointments",
        Detail: (reservationId: string) => `/appointments/${reservationId}` as const,
      },
      Shifts: {
        Index: "/appointments/shifts",
        New: "/appointments/shifts/new",
        NewWithDay: (dayOfWeek: string) => `/appointments/shifts/new/${dayOfWeek}` as const,
        Detail: (shiftId: string) => `/appointments/shifts/${shiftId}` as const,
        Edit: (shiftId: string) => `/appointments/shifts/${shiftId}/edit` as const,
        Occurrence: (shiftId: string, occurrenceDate: string) =>
          `/appointments/shifts/${shiftId}/occurrence/${occurrenceDate}` as const,
      },
      ServiceTypes: {
        Index: "/appointments/service-types",
        Detail: (serviceTypeId: string) => `/appointments/service-types/${serviceTypeId}` as const,
      },
      Services: {
        Index: "/appointments/services",
        Detail: (serviceId: string) => `/appointments/services/${serviceId}` as const,
      },
      ServiceProviders: {
        Index: "/appointments/service-providers",
        Detail: (serviceProviderId: string) => `/appointments/service-providers/${serviceProviderId}` as const,
      },
    },
  },

  ClinicUser: {
    Reservations: {
      Index: "/appointments",
      Detail: (reservationId: string) => `/appointments/${reservationId}` as const,
    },
    Shifts: {
      Index: "/appointments/shifts",
      Detail: (shiftId: string) => `/appointments/shifts/${shiftId}` as const,
    },
  },
} as const;
