# Guided surfaces

Status: Active  
Owner: Platform module: intelligence  
Scope: App-agnostic module documentation  
Last reviewed: 2026-11-12

A guided surface is the interface an assistant assembles in a conversation from predesigned components: a question with choices, a card to review and confirm, a checklist of what is left. The decision and its trust rules are in [ADR-0022](../../../architecture/decisions/0022-guided-surfaces.md); this page records what is built. The first consumer is the clinic's [first-run setup](../../../apps/private-practice-general/journeys/0030-first-run-setup-journey.md).

## Block kinds built

A reply is text plus an optional ordered list of blocks. The envelope is `GuidedSurfaceBlock` (`src/Modules/Intelligence/Application/GuidedSurfaces/GuidedSurfaceBlock.cs`); exactly the member matching its kind is set.

| Kind | Carries | Produced by |
| --- | --- | --- |
| `Question` | prompt, options (key, label), max selections | The model. Its validated interactive reply (`AgentInteractiveReplyFormat`) is turned into a block with `GuidedSurfaceBlock.ModelQuestion`. |
| `NextAction` | one key from the app's vocabulary | The model, limited to the keys the app allows (Clinic: the setup goal names, `AssistantNextActions`). |
| `ActionGroup` | run ID and the IDs of the actions that run prepared | The app, from the prepared actions of one run (`GuidedSurfaceBlock.PreparedActions`). Clinic builds all three kinds in `AssistantGuidedSurface.Blocks`. |

An action group carries IDs only, never a copy of the proposal; the web reads each action's current state by ID through the app's own route. Only the model's question carries display text.

## Transport

The wire shape is `GuidedSurfaceBlockResponse` (`src/Modules/Intelligence/Contracts/GuidedSurfaceContracts.cs`), with kind enum `GuidedSurfaceBlockKindResponse`. It is the optional `blocks` field of the conversation response (`RunAgentConversationResponse`), mapped by `IntelligenceAgentConversationResultMapper` and omitted when empty. Replies without blocks behave exactly as before. Only the Clinic operations assistant sends blocks today.

## Prepared actions and revise

Action groups depend on the [prepared-action lifecycle](prepared-actions.md). Revising a proposal (reject the original, prepare a replacement in the same run) is a lifecycle operation; each app exposes it under its own authorization, as Clinic does at `POST /assistant/actions/{actionId}/revise`.

## Shared web pieces

All live in `src/Hosts/Web/shared/intelligence` and are exported from its index.

- `toGuidedSurface` (`src/interaction/guidedSurface.ts`) reads a reply's `blocks` into `{ question, nextAction, actionGroups }`; unknown or incomplete blocks are left out, and the last question or next action wins. `emptyGuidedSurface` is the no-blocks value.
- `ActionReviewGroup` (`src/components/ActionReviewGroup.tsx`) shows the items of one run. The app supplies each item's state (`pending`, `done`, `failed`, `closed`), whether a failure is retryable, its card, and a `confirm` function that calls its own route. "Confirm all" confirms pending items one by one; "Retry failed" confirms only retryable failures; one item's failure never stops the rest; a single item renders as its own card.
- `GoalChecklist` (`src/components/GoalChecklist.tsx`) is an ordered list of goals with the next one highlighted; the app supplies keys, labels, done flags and an optional select handler.
- The question reuses `InteractiveOptionPicker`. Texts are localized under `intelligence.guidedSurface.*`.

## How an app adopts it

1. Declare the vocabulary: add the optional question and next-action fields to the agent's result schema, with the allowed next-action keys.
2. Register prepared-action policies, committers and payload shapes, and expose thin confirm, reject and revise routes that delegate to the lifecycle.
3. Return the blocks with the conversation result (Clinic: `AssistantGuidedSurface.Blocks`).
4. On the web, call `toGuidedSurface` on the reply, render each action group with `ActionReviewGroup` using the app's cards and confirm function, and render the question and next action. Clinic's composition is `OperationsGuidedReply` in `shared/clinic/src/features/clinic-assistant`.
5. Anything shown without a model call (a welcome, a goal list) is built from server keys and state in the app's web feature; Clinic does this in `features/clinic-setup`.

Changing an agent's instructions, output schema or tool exposure changes its compatibility key, so open conversations ask once to start again.

## Not built yet

- The generic run collector of ADR-0022: tool-produced blocks are not gathered by Intelligence. Prepared actions are the only tool-produced blocks, and the app groups them by run.
- Goal-checklist and destination-link blocks as wire kinds, and reference chips. The checklist is a component the app feeds from its own endpoint, and the next action is a key the app maps to a screen.
- A server-built opening surface on the wire. Clinic's welcome is composed on the web from setup progress keys, because an app API cannot map shared block contracts without duplicating the Intelligence mapper.
- Redrawing earlier blocks after a page reload. Pending actions stay reachable by ID.
