// Hooks
export { useCurrentJourney } from "./hooks/useCurrentJourney";
export type { JourneyDecision, JourneyStep, JourneyGate } from "./hooks/useCurrentJourney";
export { resolvePostLoginNavigation } from "./hooks/resolvePostLoginNavigation";
export { useCompleteProfile } from "./hooks/useCompleteProfile";
export type { ProfileCompletionValues } from "./hooks/useCompleteProfile";
export { useJourneyWorkspaceOptions } from "./hooks/useJourneyWorkspaceOptions";
export type { JourneyWorkspaceOption } from "./hooks/useJourneyWorkspaceOptions";

// Components
export { JourneyGateLayout } from "./components/JourneyGateLayout";
export { JourneyGateCard } from "./components/JourneyGateCard";
export { JourneyGateHeader } from "./components/JourneyGateHeader";
export { JourneyGateActions } from "./components/JourneyGateActions";
export { JourneyLoadingState } from "./components/JourneyLoadingState";
export { JourneyErrorState } from "./components/JourneyErrorState";
export { WorkspaceSelector } from "./components/WorkspaceSelector";
export { ProfileCompletionForm } from "./components/ProfileCompletionForm";
export { ContactConfirmationPanel } from "./components/ContactConfirmationPanel";
export { ConsentReviewPanel } from "./components/ConsentReviewPanel";

// Pages
export { WorkspaceSelectionPage } from "./pages/WorkspaceSelectionPage";
export { ProfileCompletionPage } from "./pages/ProfileCompletionPage";
export { ContactConfirmationPage } from "./pages/ContactConfirmationPage";
export { ConsentReviewPage } from "./pages/ConsentReviewPage";
