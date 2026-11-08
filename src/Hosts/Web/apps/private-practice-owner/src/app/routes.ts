import { AuthenticationWebRoutes } from "@vitarish/auth/routes";
import { tenantHandoffPath } from "@vitarish/journey/onboarding-host";
import type { PatientQuickAction, PatientRecordTab } from "@vitarish/clinic";

/** Centralized route definitions for the React web host.
 *  Equivalent to Blazor's AppRoutes class.
 *  Single source of truth for all navigation paths. */
export const Routes = {
  /** Root path — session-aware redirect to /home or /login */
  Root: "/",

  /** Public overview of Darman */
  PracticeOverview: "/about-darman",

  /** Authenticated home dashboard */
  Home: "/home",

  /** Public authentication routes */
  Login: {
    Index: AuthenticationWebRoutes.SignIn,
    Password: AuthenticationWebRoutes.SignInPassword,
    Phone: AuthenticationWebRoutes.SignInPhone,
    Email: AuthenticationWebRoutes.SignInEmail,
    Passkey: AuthenticationWebRoutes.SignInPasskey,
    ForgotPassword: AuthenticationWebRoutes.PasswordRecovery,
  },
  Logout: "/logout",

  /** Creating your own clinic, on the onboarding host */
  Setup: "/setup",

  /** Public: redeems the one-time sign-in link from setup on the new clinic's own host */
  Handoff: tenantHandoffPath,

  /** Authenticated account routes */
  Account: {
    Index: AuthenticationWebRoutes.Account,
    Name: "/account/name",
    Email: "/account/email",
    Phone: "/account/phone",
    Password: "/account/password",
    Passkeys: "/account/passkeys",
    Culture: "/account/culture",
    Security: AuthenticationWebRoutes.AccountSecurity,
    Memory: "/account/memory",
    Consents: "/account/consents",
    ContactConfirm: "/account/contact/confirm",
    ProfileComplete: "/account/profile/complete",
  },

  /** Shared sign-in host: continues a signed-in person to their platform home */
  SignInContinue: "/continue",

  /** Private-practice clinic owner routes */
  Clinic: {
    Index: "/home",
    CheckIn: "/check-in",
    Patients: {
      Index: "/patients",
      New: "/patients/new",
      Detail: (patientId: string, quickAction?: PatientQuickAction) =>
        quickAction
          ? (`/patients/${patientId}?${PatientDetailSearchParams.QuickAction}=${quickAction}` as const)
          : (`/patients/${patientId}` as const),
      /** A tab of the patient record; the overview is the record's own address. */
      Tab: (patientId: string, tab: PatientRecordTab) =>
        (tab === "overview" ? `/patients/${patientId}` : `/patients/${patientId}/${tab}`) as string,
      Edit: (patientId: string) => `/patients/${patientId}/edit` as const,
      Procedure: (patientId: string, procedureId: string) =>
        `/patients/${patientId}/procedures/${procedureId}` as const,
      ProcedureEvent: (patientId: string, procedureId: string, eventId: string) =>
        `/patients/${patientId}/procedures/${procedureId}/events/${eventId}` as const,
    },
    Surgeries: {
      Index: "/surgeries",
      New: "/surgeries/new",
      NewForPatient: (patientId: string) =>
        `/surgeries/new?${SurgeryPlanSearchParams.PatientId}=${encodeURIComponent(patientId)}` as const,
      Detail: (surgeryId: string) => `/surgeries/${surgeryId}` as const,
      Edit: (surgeryId: string) => `/surgeries/${surgeryId}/edit` as const,
    },
    Settings: {
      Index: "/settings",
      Users: "/settings/users",
      Audit: "/settings/audit",
      Insurance: "/settings/insurance",
      InsuranceCallback: "/settings/insurance/callback",
      Statistics: "/settings/statistics",
      ProcedureTemplates: "/settings/procedure-templates",
      PrescriptionTemplates: "/settings/prescription-templates",
      SurgeryTypes: "/settings/surgery-types",
      Appointments: "/settings/appointments",
      Shifts: {
        Index: "/settings/shifts",
        New: "/settings/shifts/new",
        NewWithDay: (dayOfWeek: string) => `/settings/shifts/new/${dayOfWeek}` as const,
        Detail: (shiftId: string) => `/settings/shifts/${shiftId}` as const,
        Edit: (shiftId: string) => `/settings/shifts/${shiftId}/edit` as const,
        Occurrence: (shiftId: string, occurrenceDate: string) =>
          `/settings/shifts/${shiftId}/occurrence/${occurrenceDate}` as const,
      },
      Services: {
        Index: "/settings/services",
        Detail: (serviceId: string) => `/settings/services/${serviceId}` as const,
        Types: {
          Index: "/settings/services/types",
          Detail: (serviceTypeId: string) => `/settings/services/types/${serviceTypeId}` as const,
        },
        Providers: {
          Index: "/settings/services/providers",
          Detail: (serviceProviderId: string) => `/settings/services/providers/${serviceProviderId}` as const,
        },
      },
    },
    Reminders: "/reminders",
    Appointments: {
      Reservations: {
        Index: "/appointments",
        Detail: (reservationId: string) => `/appointments/${reservationId}` as const,
      },
    },
  },

  ClinicUser: {
    Reservations: {
      Index: "/appointments",
      Detail: (reservationId: string) => `/appointments/${reservationId}` as const,
    },
    Shifts: {
      Index: "/settings/shifts",
      Detail: (shiftId: string) => `/settings/shifts/${shiftId}` as const,
    },
  },
} as const;

/** Search parameters the patient detail route understands. */
export const PatientDetailSearchParams = {
  /** A patient quick action to open on arrival. */
  QuickAction: "action",
} as const;

/** Search parameters the surgery plan route understands. */
export const SurgeryPlanSearchParams = {
  /** The patient to preselect when planning a surgery. */
  PatientId: "patientId",
} as const;
