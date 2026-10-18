import { describeSurgeryFlow, surgeryPlanSteps } from "./surgerySteps";

// Walks planning a surgery from the calendar, from finding the patient to the saved plan.
// Only the ready scenario answers with saved records; the other scenarios are covered by pages.spec.ts.
describeSurgeryFlow("surgery-plan-flow", [surgeryPlanSteps]);
