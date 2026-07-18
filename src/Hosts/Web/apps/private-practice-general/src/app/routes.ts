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
    Index: "/owner/clinic",
    CheckIn: "/owner/clinic/check-in",
    Patients: {
      Index: "/owner/clinic/patients",
      New: "/owner/clinic/patients/new",
      Detail: (patientId: string) => `/owner/clinic/patients/${patientId}` as const,
      Edit: (patientId: string) => `/owner/clinic/patients/${patientId}/edit` as const,
      Procedure: (patientId: string, procedureId: string) =>
        `/owner/clinic/patients/${patientId}/procedures/${procedureId}` as const,
      ProcedureEvent: (patientId: string, procedureId: string, eventId: string) =>
        `/owner/clinic/patients/${patientId}/procedures/${procedureId}/events/${eventId}` as const,
    },
    Settings: {
      Index: "/owner/clinic/settings",
      Users: "/owner/clinic/settings/users",
      Statistics: "/owner/clinic/settings/statistics",
    },
    Reminders: "/owner/clinic/reminders",
    Appointments: {
      Settings: "/owner/clinic/appointment-settings",
      Reservations: {
        Index: "/owner/clinic/reservations",
        Detail: (reservationId: string) => `/owner/clinic/reservations/${reservationId}` as const,
      },
      Shifts: {
        Index: "/owner/clinic/shifts",
        New: "/owner/clinic/shifts/new",
        NewWithDay: (dayOfWeek: string) => `/owner/clinic/shifts/new/${dayOfWeek}` as const,
        Detail: (shiftId: string) => `/owner/clinic/shifts/${shiftId}` as const,
        Edit: (shiftId: string) => `/owner/clinic/shifts/${shiftId}/edit` as const,
        Occurrence: (shiftId: string, occurrenceDate: string) =>
          `/owner/clinic/shifts/${shiftId}/occurrence/${occurrenceDate}` as const,
      },
      ServiceTypes: {
        Index: "/owner/clinic/service-types",
        Detail: (serviceTypeId: string) => `/owner/clinic/service-types/${serviceTypeId}` as const,
      },
      Services: {
        Index: "/owner/clinic/services",
        Detail: (serviceId: string) => `/owner/clinic/services/${serviceId}` as const,
      },
      ServiceProviders: {
        Index: "/owner/clinic/service-providers",
        Detail: (serviceProviderId: string) => `/owner/clinic/service-providers/${serviceProviderId}` as const,
      },
    },
  },

  /** Clinic User routes */
  ClinicUser: {
    Reservations: {
      Index: "/user/clinic/reservations",
      Detail: (reservationId: string) => `/user/clinic/reservations/${reservationId}` as const,
    },
    Shifts: {
      Index: "/user/clinic/shifts",
      Detail: (shiftId: string) => `/user/clinic/shifts/${shiftId}` as const,
    },
  },

  /** SEA Owner routes */
  Sea: {
    Index: "/owner/sea",
    Projects: {
      Index: "/owner/sea/projects",
      New: "/owner/sea/projects/new",
      Detail: (projectId: string) => `/owner/sea/projects/${projectId}` as const,
      Edit: (projectId: string) => `/owner/sea/projects/${projectId}/edit` as const,
    },
    Procedures: {
      Detail: (projectId: string, procedureId: string) =>
        `/owner/sea/projects/${projectId}/procedures/${procedureId}` as const,
    },
    Events: {
      Detail: (projectId: string, procedureId: string, eventId: string) =>
        `/owner/sea/projects/${projectId}/procedures/${procedureId}/events/${eventId}` as const,
    },
    Templates: {
      Index: "/owner/sea/templates",
      Detail: (projectId: string) => `/owner/sea/templates/${projectId}` as const,
    },
    Settings: {
      Users: "/owner/sea/settings/users",
      Statistics: "/owner/sea/settings/statistics",
    },
  },
} as const;
