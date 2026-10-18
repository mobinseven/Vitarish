import { describeSurgeryFlow, surgeryTypeSteps } from "./surgerySteps";

// Walks surgery type management: create, edit, and delete a type, and set the cancellation message.
// Only the ready scenario answers with saved records; the other scenarios are covered by pages.spec.ts.
describeSurgeryFlow("surgery-types-flow", [surgeryTypeSteps]);
