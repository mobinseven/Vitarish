import {
  intelligenceRewrite,
  useIntelligenceRewrite,
} from "../api/generated/intelligence/intelligence";
import type {
  IntelligenceSessionContext,
  RewriteOptions,
  RewriteRequest,
  RewriteResult,
} from "../api/generated/intelligence/model";

export type {
  IntelligenceSessionContext,
  RewriteOptions,
  RewriteRequest,
  RewriteResult,
};

export type UseRewriteOptions = Parameters<typeof useIntelligenceRewrite>[0];
export type RewriteRequestOptions = Parameters<typeof intelligenceRewrite>[1];

export function rewrite(
  request: RewriteRequest,
  options?: RewriteRequestOptions,
  signal?: AbortSignal,
): Promise<RewriteResult> {
  return intelligenceRewrite(request, options, signal);
}

export function useRewrite(options?: UseRewriteOptions) {
  return useIntelligenceRewrite(options);
}
