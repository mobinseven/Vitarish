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
| `Surface` | a presented surface: protocol, protocol version, catalog ID, component tree, data model | The model lays it out through `present_surface`; the server fills the data model from this run's handles (`PresentedSurfaceCollector`, `GuidedSurfaceBlock.Presented`). |

An action group carries IDs only, never a copy of the proposal; the web reads each action's current state by ID through the app's own route. Only the model's question carries display text.

## Transport

The wire shape is `GuidedSurfaceBlockResponse` (`src/Modules/Intelligence/Contracts/GuidedSurfaceContracts.cs`), with kind enum `GuidedSurfaceBlockKindResponse`. It is the optional `blocks` field of the conversation response (`RunAgentConversationResponse`), mapped by `IntelligenceAgentConversationResultMapper` and omitted when empty. Replies without blocks behave exactly as before. Both Clinic assistants (operations and patient) send blocks.

## Prepared actions and revise

Action groups depend on the [prepared-action lifecycle](prepared-actions.md). Revising a proposal (reject the original, prepare a replacement in the same run) is a lifecycle operation; each app exposes it under its own authorization, as Clinic does at `POST /assistant/actions/{actionId}/revise`.

## Shared web pieces

All live in `src/Hosts/Web/shared/intelligence` and are exported from its index.

- `toGuidedSurface` (`src/interaction/guidedSurface.ts`) reads a reply's `blocks` into `{ question, nextAction, actionGroups, surfaces }`; unknown or incomplete blocks are left out, and the last question or next action wins. `emptyGuidedSurface` is the no-blocks value.
- `ActionReviewGroup` (`src/components/ActionReviewGroup.tsx`) shows the items of one run. The app supplies each item's state (`pending`, `done`, `failed`, `closed`), whether a failure is retryable, its card, and a `confirm` function that calls its own route. "Confirm all" confirms pending items one by one; "Retry failed" confirms only retryable failures; one item's failure never stops the rest; a single item renders as its own card.
- `GoalChecklist` (`src/components/GoalChecklist.tsx`) is an ordered list of goals with the next one highlighted; the app supplies keys, labels, done flags and an optional select handler.
- The question reuses `InteractiveOptionPicker`. Texts are localized under `intelligence.guidedSurface.*`.
- `PresentedSurface` (`src/surfaces/PresentedSurface.tsx`) renders one presented surface; `PresentedSurfaceAppearanceProvider` takes the app's field and status labels, kind icons and how a record opens. Texts are localized under `intelligence.surface.*`.

## How an app adopts it

1. Declare the vocabulary: add the optional question and next-action fields to the agent's result schema, with the allowed next-action keys.
2. Register prepared-action policies, committers and payload shapes, and expose thin confirm, reject and revise routes that delegate to the lifecycle.
3. Return the blocks with the conversation result (Clinic: `AssistantGuidedSurface.Blocks`).
4. On the web, call `toGuidedSurface` on the reply, render each action group with `ActionReviewGroup` using the app's cards and confirm function, and render the question and next action. Clinic's composition is `OperationsGuidedReply` in `shared/clinic/src/features/clinic-assistant`. A full-page assistant uses the `AgentChatWorkspace` frame from `@vitarish/ui` (header, optional conversation list, thread, composer); Clinic's is `ClinicAssistantWorkspace`.
5. Anything shown without a model call (a welcome, a goal list) is built from server keys and state in the app's web feature; Clinic does this in `features/clinic-setup`.

Changing an agent's instructions, output schema or tool exposure changes its compatibility key. Open conversations continue: the next turn carries over only their user and assistant messages (ADR-0012).

## Presented surfaces

ADR-0022 decision 8 sets the rules and the requirements any UI protocol must meet. The protocol chosen is [A2UI](https://a2ui.org/) (Agent-to-User Interface, specification 1.0, Apache 2.0, source at `github.com/a2ui-project/a2ui`).

How a surface reaches the screen:

1. An app declares which read tools of which agents are presentable with `AgentToolPresentation` (projector from tool output to `PresentableResult`). Clinic's are `ClinicAssistantPresentations.All()`; every Clinic read tool is either declared there or listed in `ClinicAssistantPresentations.Exempt` with a reason, and `PresentationCoverageTests` enforces it.
2. The Intelligence tool callback runs `PresentableResultPublisher` after each tool: a declared, successful read is stored by run in Redis (`RedisPresentableResultStore`, `intelligence:presentable:{executionId}`, 30 minutes, not encrypted: it holds only the masked values the screens show) and its output gains a `presentation` property with the handle and its fields. A handle is numbered and marked with its run (`r1-3f9c`, `PresentableHandles`), so one remembered from an earlier turn never names a result of the current one. If the store fails, the read still succeeds without a handle.
3. The model calls `present_surface` (`PresentSurfaceTool`, contributed to Clinic by `PresentationAssistantCapability`). Its definition carries the catalog; the agent runtime validates the surface against it (`apps/agent-runtime/.../run/surface.ts`) before calling back, and marks the callback `surfaceValidated`. Main refuses unvalidated surfaces, handles of another run and fields the result does not declare, each as a tool error the model reads.
4. At the end of the run `PresentedSurfaceCollector` turns each accepted `present_surface` call into a `Surface` block, with the data model filled from the handles. Values never come from the model.
5. The web renders it with the A2UI React renderer on HeroUI components.

The catalog is one file, `src/Hosts/Web/shared/intelligence/src/surfaces/assistant-catalog.json`, embedded in `Intelligence.Application` (`AssistantSurfaceCatalog`) and imported by the web. Components: `Column`, `Heading`, `Caption` (literal text), `RecordCard`, `RecordList`, `Metric`, `BarChart` (each `result` is a binding to a handle only).

| Part | Package | Notes |
| --- | --- | --- |
| Web renderer | `@a2ui/react` 0.15 with `@a2ui/web_core` 0.15 | `presentedSurfaceCatalog()` builds the implementations from the catalog document; each view resolves its `result` binding through the surface's data context. |
| Surface validation in the agent runtime | `@a2ui/agent` 0.1 | `validatePayload` against the catalog sent with the tool. |

Design standard (inline cards in a chat, after the OpenAI, Anthropic, Google and Microsoft inline-card guidance): cards size to their content with no inner scrolling and no fixed widths; a title of one or two lines and at most three facts; lists show five rows and then say more exist (with a link to the full screen where the app supplies one); charts read right to left in Persian; one action per record, opening its screen; colors and components come from HeroUI as elsewhere in the app; text is rem-based.

Known gaps:

- All packages are pre-1.0 although the specification is 1.0. `@a2ui/react` and `@a2ui/web_core` bring their own zod 3 alongside the workspace's zod 4.
- The A2UI basic catalog has no chart; Vitarish's `BarChart` uses HeroUI Pro's bar chart.
- The specification has no text-direction or translation guidance; HeroUI and the platform localization provide both.
- The v1.0 resolver reads `@path`; the catalog also accepts `path`, which the web normalizes.

## Not built yet

- Opening presented records other than Clinic's patient candidates, and a full-screen link under shortened lists: the other kinds render without an action until their screens are mapped.
- The web's record-kind list (`shared/clinic/.../presentedKinds.ts`) is a hand-kept mirror of `ClinicPresentedRecordKinds`; it is not generated yet.
- Persisting presented surfaces: Clinic stores no presentation transcript and shows no earlier turns after a reload, so surfaces are not stored and redrawn.
- Prepared-action review cards and the setup welcome as catalog components; they stay on the shared `ActionReviewGroup` and `GoalChecklist`.
- Goal-checklist and destination-link blocks as wire kinds, and reference chips. The checklist is a component the app feeds from its own endpoint, and the next action is a key the app maps to a screen.
- A server-built opening surface on the wire. Clinic's welcome is composed on the web from setup progress keys, because an app API cannot map shared block contracts without duplicating the Intelligence mapper.
- Redrawing earlier blocks after a page reload. Pending actions stay reachable by ID. Within a browser tab, Clinic keeps what its assistant surfaces showed in memory (never browser storage), so navigating away and back shows the thread again; after a reload the conversation continues but its earlier messages are not shown until conversation history is built.
