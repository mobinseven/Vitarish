import { describeSurgeryFlow, surgeryAssistantSteps } from "./surgerySteps";

// Walks the operations assistant's review card for a prepared surgery change.
// Only the ready scenario answers with saved records; the other scenarios are covered by pages.spec.ts.
describeSurgeryFlow("surgery-assistant-flow", [surgeryAssistantSteps]);
