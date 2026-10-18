import { describeSurgeryFlow, surgeryTypeSteps, surgeryOverviewSteps, surgeryPlanSteps, surgeryEditSteps, surgeryStatusSteps, surgeryAssistantSteps } from "./surgerySteps";

// Walks every surgery page and widget of the owner app, in the order an owner meets them.
// Only the ready scenario answers with saved records; the other scenarios are covered by pages.spec.ts.
describeSurgeryFlow("surgery-flow", [surgeryTypeSteps, surgeryOverviewSteps, surgeryPlanSteps, surgeryEditSteps, surgeryStatusSteps, surgeryAssistantSteps]);
