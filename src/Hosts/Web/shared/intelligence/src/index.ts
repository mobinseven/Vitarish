export {
  appendConfirmedTranscript,
  useLiveDictation,
} from "./hooks/useLiveDictation";
export type {
  LiveDictationController,
  UseLiveDictationOptions,
} from "./hooks/useLiveDictation";
export type {
  LiveDictationFailureCode,
  LiveDictationLifecycle,
} from "./api/intelligence/liveTranscriptionClient";
export { LiveDictation } from "./components/LiveDictation";
export type { LiveDictationProps } from "./components/LiveDictation";
export { AgentConversationComposer } from "./components/AgentConversationComposer";
export { InteractiveOptionPicker } from "./components/InteractiveOptionPicker";
export type { InteractiveOptionPickerProps } from "./components/InteractiveOptionPicker";
export { InteractiveReferenceChips, InteractiveReplyMessage } from "./components/InteractiveReplyMessage";
export type { InteractiveReferenceChipsProps, InteractiveReplyMessageProps } from "./components/InteractiveReplyMessage";
export { toInteractiveReply } from "./interaction/interactiveReply";
export { emptyGuidedSurface, toGuidedSurface } from "./interaction/guidedSurface";
export type { GuidedSurface, GuidedSurfaceActionGroup } from "./interaction/guidedSurface";
export { ActionReviewGroup } from "./components/ActionReviewGroup";
export type { ActionReviewGroupProps, ActionReviewItem, ActionReviewItemState } from "./components/ActionReviewGroup";
export { GoalChecklist } from "./components/GoalChecklist";
export { PresentedSurface } from "./surfaces/PresentedSurface";
export type { GuidedSurfaceSurfaceResponse } from "./api/generated/intelligence/model";
export { PresentedSurfaceAppearanceProvider, usePresentedSurfaceAppearance } from "./surfaces/PresentedSurfaceContext";
export type { PresentedSurfaceAppearance } from "./surfaces/PresentedSurfaceContext";
export type { PresentedRecord, PresentedResult, PresentedField, PresentedFieldType } from "./surfaces/presentedData";
export type { ChecklistGoal, GoalChecklistProps } from "./components/GoalChecklist";
export type { GuidedSurfaceBlockResponse } from "./api/generated/intelligence/model";
export { RememberedNotes, UserMemorySection } from "./components/UserMemory";
export { useUserMemory } from "./hooks/useUserMemory";
export type {
  InteractiveOption,
  InteractiveQuestion,
  InteractiveReference,
  InteractiveRemembered,
  InteractiveReply,
  InteractiveReplyWire,
} from "./interaction/interactiveReply";
export type { AgentConversationComposerProps } from "./components/AgentConversationComposer";
export { intelligenceRunAgentConversation } from "./api/generated/intelligence/intelligence";
export type {
  RunAgentConversationRequest,
  RunAgentConversationResponse,
} from "./api/generated/intelligence/model";
export { rewrite, useRewrite } from "./hooks/useRewrite";
export type {
  IntelligenceSessionContext,
  RewriteOptions,
  RewriteRequestOptions,
  RewriteRequest,
  RewriteResult,
  UseRewriteOptions,
} from "./hooks/useRewrite";
export { useConversationController } from "./hooks/useConversationController";
export type {
  ConversationController,
  ActiveTurnProvisionalAnswer,
  ConversationRunResult,
  UseConversationControllerOptions,
} from "./hooks/useConversationController";
export {
  AgentTurnCanceledError,
  AgentTurnFailedError,
  isConversationNotFound,
  resetAgentConversationCapabilities,
  runAgentConversationTurn,
  UnknownTurnOutcomeError,
} from "./api/intelligence/agentConversationTurn";
export type { AgentConversationTurnOptions, ProvisionalAnswer } from "./api/intelligence/agentConversationTurn";
export { AgentTurnStreamError, IntelligenceErrorCodeValue, readAgentTurnFrames, StreamChunkKindValue } from "./api/intelligence/agentTurnFrames";
export type { AgentTurnFrame, AgentTurnStreamViolation } from "./api/intelligence/agentTurnFrames";
