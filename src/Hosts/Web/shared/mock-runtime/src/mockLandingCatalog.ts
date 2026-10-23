import { clinicSpecialties } from "./mockOnboardingHandlers";

/**
 * Mock of `GET /api/apps/landing/catalog`. Source of truth: `LandingFeatureCatalog` and
 * `LandingConsultation` in `src/Apps/Landing/Application`; keep this fixture in step with them.
 */
export const landingCatalog = {
  features: [
    { key: "online-booking", availability: "Live" },
    { key: "reservations", availability: "Live" },
    { key: "shifts-and-services", availability: "Live" },
    { key: "check-in-sms", availability: "Live" },
    { key: "reminders", availability: "Live" },
    { key: "patient-records", availability: "Live" },
    { key: "care-timeline", availability: "Live" },
    { key: "dictated-notes", availability: "Live" },
    { key: "clinical-templates", availability: "Live" },
    { key: "staff-roles", availability: "Live" },
    { key: "audit-log", availability: "Live" },
    { key: "clinic-address", availability: "Live" },
    { key: "bilingual", availability: "Live" },
    { key: "tamin-insurance", availability: "InProgress" },
    { key: "clinic-statistics", availability: "NotYet" },
  ],
  consultation: {
    kind: {
      key: "kind",
      maxSelections: 1,
      options: clinicSpecialties.map((key) => ({ key, featureKeys: [], proof: null })),
    },
    booking: {
      key: "booking",
      maxSelections: 1,
      options: ["phone", "messaging", "walk-in", "other-software"].map((key) => ({ key, featureKeys: [], proof: null })),
    },
    pains: {
      key: "pains",
      maxSelections: 3,
      options: [
        { key: "no-shows", featureKeys: ["reminders", "check-in-sms"], proof: "NoShowCost" },
        { key: "phone-load", featureKeys: ["online-booking", "reservations"], proof: "BookingPreview" },
        { key: "patient-history", featureKeys: ["patient-records", "care-timeline"], proof: "PatientTimeline" },
        { key: "after-hours-notes", featureKeys: ["dictated-notes", "clinical-templates"], proof: "Dictation" },
        { key: "shifts-and-roles", featureKeys: ["shifts-and-services", "staff-roles"], proof: "ShiftWeek" },
        { key: "insurance", featureKeys: ["tamin-insurance"], proof: "InProgress" },
      ],
    },
  },
  nextActions: { openClinic: "open-clinic", talkToTeam: "talk-to-team", tryVoice: "try-voice" },
} as const;
