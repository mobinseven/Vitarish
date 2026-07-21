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
export {
  getIntelligenceRewriteMutationOptions,
  getIntelligenceStreamRewriteMutationOptions,
  intelligenceRewrite,
  intelligenceStreamRewrite,
  useIntelligenceRewrite,
  useIntelligenceStreamRewrite,
} from "./api/generated/intelligence/intelligence";
export type {
  IntelligenceRewriteMutationBody,
  IntelligenceRewriteMutationError,
  IntelligenceRewriteMutationResult,
  IntelligenceStreamRewriteMutationBody,
  IntelligenceStreamRewriteMutationError,
  IntelligenceStreamRewriteMutationResult,
} from "./api/generated/intelligence/intelligence";
export type {
  IntelligenceSessionContext,
  ProblemDetails,
  RewriteOptions,
  RewriteRequest,
} from "./api/generated/intelligence/model";
