import { describeSurgeryFlow, surgeryStatusSteps } from "./surgerySteps";

// Walks completing a surgery, correcting the completion, and cancelling a surgery.
// Only the ready scenario answers with saved records; the other scenarios are covered by pages.spec.ts.
describeSurgeryFlow("surgery-status-flow", [surgeryStatusSteps]);
