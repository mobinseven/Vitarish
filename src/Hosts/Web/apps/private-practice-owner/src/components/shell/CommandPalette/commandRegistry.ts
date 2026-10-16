import { Routes } from "@/app/routes";

export interface Command {
  id: string;
  labelKey: string;
  icon: string;
  shortcut?: string;
  action: () => void;
  keywords: string[];
}

export interface CommandGroup {
  id: string;
  labelKey: string;
  commands: Command[];
}

export function createCommandGroups(
  navigate: (href: string) => void,
  toggleTheme?: () => void
): CommandGroup[] {
  return [
    {
      id: "navigation",
      labelKey: "commandPalette.groups.navigation",
      commands: [
        {
          id: "home",
          labelKey: "appShell.navigation.home",
          icon: "house",
          shortcut: "G H",
          action: () => navigate(Routes.Home),
          keywords: ["home", "dashboard", "main"],
        },
        {
          id: "patients",
          labelKey: "clinic.patients.title",
          icon: "users",
          shortcut: "G P",
          action: () => navigate(Routes.Clinic.Patients.Index),
          keywords: ["patients", "users", "list"],
        },
        {
          id: "check-in",
          labelKey: "clinic.dashboard.sections.checkIn.title",
          icon: "clipboard-check",
          shortcut: "G C",
          action: () => navigate(Routes.Clinic.CheckIn),
          keywords: ["checkin", "check-in", "arrival"],
        },
        {
          id: "appointments",
          labelKey: "clinic.appointments.reservations.title",
          icon: "calendar-check",
          shortcut: "G A",
          action: () => navigate(Routes.Clinic.Appointments.Reservations.Index),
          keywords: ["appointments", "reservations", "booking"],
        },
        {
          id: "surgeries",
          labelKey: "clinic.surgeries.title",
          icon: "scissors",
          shortcut: "G S",
          action: () => navigate(Routes.Clinic.Surgeries.Index),
          keywords: ["surgeries", "surgery", "operations"],
        },
        {
          id: "reminders",
          labelKey: "clinic.dashboard.sections.reminders.title",
          icon: "bell",
          shortcut: "G R",
          action: () => navigate(Routes.Clinic.Reminders),
          keywords: ["reminders", "alerts", "notifications"],
        },
        {
          id: "knowledge",
          labelKey: "appShell.navigation.knowledge",
          icon: "book-open",
          shortcut: "G K",
          action: () => navigate(Routes.Knowledge.Index),
          keywords: ["knowledge", "docs", "help"],
        },
        {
          id: "contents",
          labelKey: "contents.title",
          icon: "file-pen",
          shortcut: "G M",
          action: () => navigate(Routes.Contents.Index),
          keywords: ["content", "cms", "publish", "articles"],
        },
      ],
    },
    {
      id: "quickActions",
      labelKey: "commandPalette.groups.quickActions",
      commands: [
        {
          id: "new-patient",
          labelKey: "commandPalette.commands.newPatient",
          icon: "user-plus",
          shortcut: "N P",
          action: () => navigate(Routes.Clinic.Patients.New),
          keywords: ["new", "add", "create", "patient"],
        },
        {
          id: "new-shift",
          labelKey: "commandPalette.commands.newShift",
          icon: "clock",
          shortcut: "N S",
          action: () => navigate(Routes.Clinic.Appointments.Shifts.New),
          keywords: ["new", "add", "create", "shift", "schedule"],
        },
        {
          id: "new-content",
          labelKey: "contents.create",
          icon: "file-circle-plus",
          shortcut: "N C",
          action: () => navigate(Routes.Contents.New),
          keywords: ["new", "add", "create", "content", "article"],
        },
      ],
    },
    {
      id: "appointments",
      labelKey: "commandPalette.groups.appointments",
      commands: [
        {
          id: "all-reservations",
          labelKey: "clinic.appointments.reservations.title",
          icon: "calendar-check",
          action: () => navigate(Routes.Clinic.Appointments.Reservations.Index),
          keywords: ["reservations", "list", "all"],
        },
        {
          id: "shifts",
          labelKey: "clinic.appointments.shifts.title",
          icon: "clock",
          action: () => navigate(Routes.Clinic.Appointments.Shifts.Index),
          keywords: ["shifts", "schedule", "work"],
        },
        {
          id: "service-types",
          labelKey: "clinic.appointments.serviceTypes.title",
          icon: "list",
          action: () => navigate(Routes.Clinic.Appointments.ServiceTypes.Index),
          keywords: ["service", "types", "category"],
        },
        {
          id: "services",
          labelKey: "clinic.appointments.services.title",
          icon: "concierge-bell",
          action: () => navigate(Routes.Clinic.Appointments.Services.Index),
          keywords: ["services", "treatments", "offerings"],
        },
        {
          id: "service-providers",
          labelKey: "clinic.appointments.serviceProviders.title",
          icon: "user-tie",
          action: () => navigate(Routes.Clinic.Appointments.ServiceProviders.Index),
          keywords: ["providers", "doctors", "staff"],
        },
      ],
    },
    {
      id: "account",
      labelKey: "commandPalette.groups.account",
      commands: [
        {
          id: "account-settings",
          labelKey: "account.title",
          icon: "user",
          action: () => navigate(Routes.Account.Index),
          keywords: ["account", "profile", "settings"],
        },
        {
          id: "edit-name",
          labelKey: "account.navigation.name",
          icon: "pen",
          action: () => navigate(Routes.Account.Name),
          keywords: ["name", "display", "rename"],
        },
        {
          id: "change-email",
          labelKey: "account.navigation.email",
          icon: "envelope",
          action: () => navigate(Routes.Account.Email),
          keywords: ["email", "mail", "address"],
        },
        {
          id: "change-phone",
          labelKey: "account.navigation.phone",
          icon: "phone",
          action: () => navigate(Routes.Account.Phone),
          keywords: ["phone", "mobile", "number"],
        },
        {
          id: "change-password",
          labelKey: "account.navigation.password",
          icon: "lock",
          action: () => navigate(Routes.Account.Password),
          keywords: ["password", "security", "credential"],
        },
        {
          id: "passkeys",
          labelKey: "account.navigation.passkeys",
          icon: "key",
          action: () => navigate(Routes.Account.Passkeys),
          keywords: ["passkey", "webauthn", "biometric"],
        },
        {
          id: "language",
          labelKey: "account.navigation.language",
          icon: "globe",
          action: () => navigate(Routes.Account.Culture),
          keywords: ["language", "locale", "i18n", "culture"],
        },
      ],
    },
    {
      id: "clinicSettings",
      labelKey: "commandPalette.groups.settings",
      commands: [
        {
          id: "clinic-settings",
          labelKey: "clinic.dashboard.sections.settings.title",
          icon: "gear",
          action: () => navigate(Routes.Clinic.Settings.Index),
          keywords: ["settings", "configuration", "preferences"],
        },
        {
          id: "users",
          labelKey: "clinic.settings.users.title",
          icon: "users-gear",
          action: () => navigate(Routes.Clinic.Settings.Users),
          keywords: ["users", "staff", "team", "members"],
        },
        {
          id: "statistics",
          labelKey: "clinic.settings.statistics.title",
          icon: "chart-bar",
          action: () => navigate(Routes.Clinic.Settings.Statistics),
          keywords: ["statistics", "analytics", "reports", "metrics"],
        },
        {
          id: "prescription-templates",
          labelKey: "clinic.settings.sections.prescriptionTemplates.title",
          icon: "prescription",
          action: () => navigate(Routes.Clinic.Settings.PrescriptionTemplates),
          keywords: ["prescription", "templates", "medication", "drug"],
        },
      ],
    },
    {
      id: "system",
      labelKey: "commandPalette.groups.system",
      commands: [
        {
          id: "toggle-theme",
          labelKey: "commandPalette.commands.toggleTheme",
          icon: "circle-half-stroke",
          shortcut: "T T",
          action: () => toggleTheme?.(),
          keywords: ["theme", "dark", "light", "mode", "appearance"],
        },
        {
          id: "sign-out",
          labelKey: "appShell.userMenu.signOut",
          icon: "arrow-right-from-bracket",
          action: () => navigate(Routes.Logout),
          keywords: ["logout", "signout", "exit", "session"],
        },
      ],
    },
  ];
}
