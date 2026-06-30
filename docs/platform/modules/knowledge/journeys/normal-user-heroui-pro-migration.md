# Normal User Knowledge QA HeroUI Pro Migration Seam

## Purpose

The normal-user Knowledge QA page currently uses free `@heroui/react` primitives, but its chat UI is intentionally routed through a small internal adapter layer. This keeps the page shippable without a Pro license while making a later HeroUI Pro migration small and localized.

## Current Adapter Layer

Canonical adapter location:

```txt
src/Hosts/Web/src/features/knowledge/qa/components/chat/
```

Current adapter exports:

| Internal primitive | Current implementation | Future Pro replacement target |
| --- | --- | --- |
| `KnowledgeChatConversation` | Layout wrapper using free HeroUI/core markup | Pro chat conversation/container component |
| `KnowledgeChatMessage` | Message card using `Card` | Pro chat message component |
| `KnowledgePromptInput` | Prompt form using `TextField`, `Input`, and `Button` | Pro prompt input component |
| `KnowledgeChatSourceList` | Source/citation section wrapper | Pro chat source/source list component |
| `KnowledgeChatSource` | Citation/context source row | Pro chat source component |
| `KnowledgeChatLoader` | Loading card | Pro chat loader/text shimmer component |
| `KnowledgeChatEmptyState` | Empty state card | Optional Pro empty-state or custom shell |
| `KnowledgeChatBody` | Answer body wrapper | Pro markdown/chat message body component |
| `KnowledgeChatCallout` | Error/missing-info callout | Pro tool/status/message extension |

## Migration Rule

Feature/page components should import chat UI only from:

```ts
import { ... } from "./chat";
```

They should not import future Pro chat components directly. When a license is acquired, update the adapter internals first. Keep the Knowledge QA feature API, hooks, and page state unchanged unless the Pro component requires a different state shape.

## Expected Migration Steps

1. Add the Pro package dependency according to the licensed package instructions.
2. Replace internals of `components/chat/KnowledgeChatPrimitives.tsx` with Pro components.
3. Keep exported internal names stable.
4. Move markdown/source rendering into Pro primitives only after confirming citation/source display remains corpus-scoped.
5. Run typecheck and route smoke tests for `/knowledge` and `/knowledge/qa`.

## Guardrails

- Do not couple normal-user QA pages directly to Pro imports.
- Do not expose owner operations through Pro chat actions.
- Preserve all-corpus versus selected-corpus ask-again scope.
- Preserve explicit corpus labels in all-corpus results.
- Preserve citations, missing information, and retrieved context visibility.
