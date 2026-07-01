export const enMessages = {
  common: {
    actions: { save: "Save", cancel: "Cancel", close: "Close", previous: "Previous", next: "Next", retry: "Retry", delete: "Delete", edit: "Edit", search: "Search" },
    states: { loading: "Loading", saving: "Saving", error: "Something went wrong", empty: "Nothing to show yet" },
    values: { notAvailable: "Not available", unknown: "Unknown" },
  },
  auth: {
    login: {
      title: "Sign in to Vitarish", subtitle: "Choose a secure sign-in method to continue.", usernameOrEmailLabel: "Username or Email", usernameOrEmailPlaceholder: "Enter your username or email", passwordLabel: "Password", passwordPlaceholder: "Enter your password", signInAction: "Sign In",
      emailAction: "Email code", phoneAction: "Phone code", passkeyAction: "Passkey", passwordAction: "Password", loading: "Signing in", redirecting: "Redirecting", logoutAction: "Sign out",
    },
    otp: { emailLabel: "Email", phoneLabel: "Phone", codeLabel: "Verification code", codePlaceholder: "Enter the code", sendAction: "Send code", verifyAction: "Verify code", resendAction: "Resend code", expiresIn: "Code expires in {value}" },
    passkey: { signIn: "Sign in with passkey" },
    errors: { generic: "We could not complete sign-in. Please try again." },
  },
  account: { title: "Account", settings: "Account settings", email: "Email", phone: "Phone", name: "Name", password: "Password", passkeys: "Passkeys", consents: "Consents", theme: "Theme", culture: "Language and region" },
  journey: { loading: "Checking your account", errorTitle: "We could not load your next step", profileTitle: "Complete your profile", consentTitle: "Review consents", contactTitle: "Confirm your contact information", workspaceTitle: "Choose a workspace", continueAction: "Continue" },
  clinic: { patients: { title: "Patients", emptyState: "No patients found", searchPlaceholder: "Search patients", countFound: { one: "{count} patient found", other: "{count} patients found" } }, patient: { gender: { male: "Male", female: "Female", other: "Other", unknown: "Unknown" }, education: { primary: "Primary", secondary: "Secondary", higher: "Higher education", unknown: "Unknown" } } },
  knowledge: { qa: { title: "Knowledge Q&A", askAction: "Ask", questionPlaceholder: "Ask a question", sources: "Sources", emptyState: "Ask a question to get started" } },
  home: { title: "Home", dashboardTitle: "Your workspace", dashboardDescription: "Choose a module to continue." },
  appShell: { brand: "Vitarish", navigation: { home: "Home", knowledge: "Knowledge", clinic: "Clinic" }, userMenu: { account: "Account", signOut: "Sign out" } },
  metadata: { root: { title: "Vitarish", description: "Vitarish platform authentication" }, home: { title: "Home | Vitarish", description: "Vitarish home dashboard" }, knowledge: { title: "Knowledge | Vitarish", description: "Ask questions against approved knowledge sources" }, clinicPatients: { title: "Patients | Vitarish", description: "Manage clinic patients" } },
  errors: { generic: "Something went wrong. Please try again.", unauthorized: "Please sign in to continue.", forbidden: "You do not have permission to perform this action.", notFound: "The requested item was not found.", validation: "Please review the highlighted fields." },
} as const;
