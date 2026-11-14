// Hooks
export { useCurrentJourney } from "./hooks/useCurrentJourney";
export type { JourneyDecision, JourneyStep, JourneyGate } from "./hooks/useCurrentJourney";
export { resolvePostLoginNavigation } from "./hooks/resolvePostLoginNavigation";
export { useCompleteProfile } from "./hooks/useCompleteProfile";
export type { ProfileCompletionValues } from "./hooks/useCompleteProfile";

// Components
export { JourneyGateLayout } from "./components/JourneyGateLayout";
export { JourneyGateCard } from "./components/JourneyGateCard";
export { JourneyGateHeader } from "./components/JourneyGateHeader";
export { JourneyGateActions } from "./components/JourneyGateActions";
export { JourneyLoadingState } from "./components/JourneyLoadingState";
export { JourneyErrorState } from "./components/JourneyErrorState";
export { ProfileCompletionForm } from "./components/ProfileCompletionForm";
export { ContactConfirmationPanel } from "./components/ContactConfirmationPanel";
export { ConsentReviewPanel } from "./components/ConsentReviewPanel";

// Pages
export { ProfileCompletionPage } from "./pages/ProfileCompletionPage";
export { ContactConfirmationPage } from "./pages/ContactConfirmationPage";
export { ConsentReviewPage } from "./pages/ConsentReviewPage";
export { TenantSetupPage } from "./pages/TenantSetupPage";
export { TenantHandoffPage } from "./pages/TenantHandoffPage";
export { SignInContinuePage } from "./pages/SignInContinuePage";

// Cross-host handoff
export { handoffUrl, hostUrl } from "./features/handoff/hostUrl";
export { usePlatformHostClinics } from "./features/handoff/usePlatformHostClinics";
export type { PlatformHostClinics } from "./features/handoff/usePlatformHostClinics";

// Clinic home
export { TenantLogoPrompt } from "./features/tenantLogo/TenantLogoPrompt";
