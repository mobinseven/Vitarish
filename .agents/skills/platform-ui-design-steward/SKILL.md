---
name: platform-ui-design-steward
description: Guide UI/UX artifact creation for the React Web host to enforce unified glassmorphism and clean minimal design. Centralizes design rules, enforces visual consistency, manages component patterns, and handles HeroUI v3/v3 Pro style governance.
---

# Platform UI Design Steward

## Purpose

Use this skill to enforce a unified visual design language across all user-facing React components in the Web host.

This skill has two layers:

1. **Design philosophy** (`references/design-rules.md`) — abstract ideas about how the product should feel and look. No code, no numbers, no class names. This is the customizable policy layer.
2. **Framework implementation** (`references/component-patterns.md`) — concrete HeroUI components, CSS classes, and tokens that implement the philosophy. This is the technical translation layer.

The skill's job: read the philosophy, find the best HeroUI match, produce the implementation.

## Use When

Use this skill when creating, modifying, or reviewing:

- React UI components (cards, buttons, forms, dialogs, tables, badges)
- Page layouts and navigation
- Design-system primitives (`SurfaceCard`, `AppShell`, `HeroPanel`, etc.)
- Global CSS tokens and styles in `src/Hosts/Web/src/app/globals.css`
- Any user-facing visual artifact in `src/Hosts/Web/`

Apply it especially when:

- Building new UI components
- Refactoring existing components for visual consistency
- Adding glassmorphism effects to surfaces
- Updating design tokens or theme variables
- Reviewing UI pull requests for visual consistency
- Planning a HeroUI upgrade (v3 → v3 Pro)

## Do Not Use For

- **Component composition and data flow** → `platform-feature-ui-integration-steward`
- **Feature hooks, query invalidation, mutation handling** → `platform-feature-ui-integration-steward`
- **File placement and module ownership** → `platform-artifact-placement-steward`
- **HeroUI component API, props, and examples** → `heroui-react`
- **Hard-coded text and localization** → `platform-localization-maintenance`
- **Frontend/backend dependency boundaries** → `platform-frontend-backend-isolation`
- **OpenAPI contracts and generated clients** → `platform-openapi-contract-steward` / `platform-generated-react-client-steward`

## How This Skill Works

### Step 1: Read the Philosophy

Read `references/design-rules.md`. This file contains abstract design principles — how surfaces should feel, how color should behave, how motion should feel. It contains zero implementation details.

### Step 2: Find the Framework Match

For each design principle, find the most compatible option provided by HeroUI v3 (or the current UI framework). Use the `heroui-react` skill's scripts and docs to discover available components, variants, and styling capabilities.

### Step 3: Document the Mapping

The result goes into `references/component-patterns.md` — the concrete implementation of each design principle using the framework. This file contains code, class names, and token references.

### When the Framework Changes

When upgrading to HeroUI v3 Pro (or any framework change):

1. Re-read `references/design-rules.md` — the philosophy does not change.
2. Discover new framework capabilities (new components, new variants, new theming).
3. Update `references/component-patterns.md` with better framework matches.
4. The philosophy stays stable; the implementation evolves.

### When Design Policy Changes

When the product's visual direction changes:

1. Update `references/design-rules.md` with the new philosophy.
2. Review `references/component-patterns.md` — some implementations may no longer match.
3. Update component patterns to find new framework matches for the new philosophy.

## Core Rules

1. **The design rules file is the source of truth.** Read it before implementing or reviewing UI.
2. **Never put framework details in the design rules file.** It contains ideas, not code.
3. **Never put abstract philosophy in the component patterns file.** It contains implementations, not ideas.
4. **Each component pattern must trace back to a design principle.** If a pattern does not serve a philosophy goal, question whether it should exist.
5. **Find the best framework match.** Do not force a framework feature to fit a principle when a better match exists.
6. **When in doubt, re-read the philosophy.** The principles guide decisions; the framework provides tools.

## Component Patterns

Read `references/component-patterns.md` for the required implementation pattern for each component type. Each pattern includes a reference to the design principle it implements.

## Opt-Out Mechanism

Components may be exempt from specific rules when justified:

1. Add a comment in the component file: `// UI-DESIGN-EXEMPTION: <specific reason>`
2. The exemption must name the specific rule being exempted and why.
3. Generic "looks better" is not a valid exemption.
4. Exemptions are reviewed during code review and must be approved.
5. The centralized rules remain the default; exemptions are narrow exceptions.

## Review Procedure

When reviewing UI artifacts, verify:

1. **Philosophy alignment:** Does the component's visual feel match the design principles?
2. **Framework match:** Is the chosen HeroUI component/variant the best match for the design principle?
3. **Implementation consistency:** Does the component follow the patterns in `component-patterns.md`?
4. **Dark mode:** Does the component work in both light and dark themes with the right feel?
5. **Accessibility:** Focus indicators visible, motion preferences respected, contrast adequate.

## Hard Stops

Stop and correct immediately if:

- The design rules file contains class names, numbers, or code
- The component patterns file contains abstract philosophy instead of implementations
- A component pattern does not trace back to a design principle
- Raw color values appear in component code
- A component does not work in dark mode
- No `aria-label` on tables or interactive elements

## Required Output

When reviewing or planning UI work, include:

```md
## UI Design Review

### Component: <name>
### Design principle: <which philosophy this implements>
### Framework match: <HeroUI component/variant chosen>

| Check | Status |
| --- | --- |
| Philosophy alignment | Pass / Fail — <detail> |
| Framework match | Pass / Fail — <detail> |
| Implementation consistency | Pass / Fail — <detail> |
| Dark mode | Pass / Fail — <detail> |
| Accessibility | Pass / Fail — <detail> |

### Findings
| Severity | Issue | Fix |
| --- | --- | --- |
| Must-fix / Should-fix | <description> | <action> |

### Decision: <accept / fix required / reject>
```

## Handoff

Use this skill **after** artifact placement is confirmed (`platform-artifact-placement-steward`) and **before** or **alongside** feature integration (`platform-feature-ui-integration-steward`).

Route to other skills when:

| Situation | Route to |
| --- | --- |
| Component file placement is unclear | `platform-artifact-placement-steward` |
| Component needs data/hooks | `platform-feature-ui-integration-steward` |
| HeroUI component API is unclear | `heroui-react` |
| Component has hard-coded user text | `platform-localization-maintenance` |
| Component touches backend imports | `platform-frontend-backend-isolation` |

## Evidence of Use

When applying this skill, include this line in the plan or final response:

```txt
Applied skill: platform-ui-design-steward
```

## Style

Be direct and practical. Reference design principles by their section name in `design-rules.md`. Reference implementations by their section in `component-patterns.md`. Focus on what changed and what must change.
