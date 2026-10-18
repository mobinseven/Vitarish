import { describeSurgeryFlow, surgeryEditSteps } from "./surgerySteps";

// Walks editing a planned surgery's details, team, tools, and reminders.
// Only the ready scenario answers with saved records; the other scenarios are covered by pages.spec.ts.
describeSurgeryFlow("surgery-edit-flow", [surgeryEditSteps]);
