import { GuidedSurfaceBlockKindResponse } from "../api/generated/intelligence/model";
import type { GuidedSurfaceBlockResponse } from "../api/generated/intelligence/model";
import type { InteractiveQuestion } from "./interactiveReply";

/** The actions one assistant run prepared, by ID; each app reads their current state itself. */
export interface GuidedSurfaceActionGroup {
  agentRunId: string;
  actionIds: readonly string[];
}

/** One reply's guided-surface blocks (ADR-0022), as the shared components render them. */
export interface GuidedSurface {
  question: InteractiveQuestion | null;
  nextAction: string | null;
  actionGroups: readonly GuidedSurfaceActionGroup[];
}

export const emptyGuidedSurface: GuidedSurface = { question: null, nextAction: null, actionGroups: [] };

/** Reads the blocks of a conversation reply; unknown or incomplete blocks are left out. */
export function toGuidedSurface(blocks: readonly GuidedSurfaceBlockResponse[] | null | undefined): GuidedSurface {
  let question: InteractiveQuestion | null = null;
  let nextAction: string | null = null;
  const actionGroups: GuidedSurfaceActionGroup[] = [];
  for (const block of blocks ?? []) {
    if (block.kind === GuidedSurfaceBlockKindResponse.Question && block.question) {
      const maxSelections = Number(block.question.maxSelections);
      question = {
        prompt: block.question.prompt,
        options: block.question.options.map(({ key, label }) => ({ key, label })),
        maxSelections: Number.isFinite(maxSelections) && maxSelections > 0 ? maxSelections : 1,
      };
    } else if (block.kind === GuidedSurfaceBlockKindResponse.NextAction && block.nextAction) {
      nextAction = block.nextAction;
    } else if (block.kind === GuidedSurfaceBlockKindResponse.ActionGroup && block.actionGroup?.actionIds.length) {
      actionGroups.push({ agentRunId: block.actionGroup.agentRunId, actionIds: block.actionGroup.actionIds });
    }
  }
  return { question, nextAction, actionGroups };
}
