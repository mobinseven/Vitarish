import { describeSurgeryFlow, surgeryOverviewSteps } from "./surgerySteps";

// Walks where surgeries show up: the dashboard panel, the calendar, and the patient profile.
// Only the ready scenario answers with saved records; the other scenarios are covered by pages.spec.ts.
describeSurgeryFlow("surgery-overview-flow", [surgeryOverviewSteps]);
