---
name: platform-ui-design-steward
description: Guide React Web host UI/UX work and enforce the current visual policy: use HeroUI v3.2.1 off-the-shelf styling unless custom design is explicitly approved.
---

# Platform UI Design Steward

## Purpose

Use this skill to keep user-facing React Web host UI visually consistent with the accepted platform policy.

Current policy: **HeroUI v3.2.1 off-the-shelf appearance is the default.** Do not create custom styling or a custom design language unless the task, an ADR, or an in-code exemption explicitly states that the custom design is deliberate.

When encountering existing custom styling or design, remove it or plan its removal unless there is an explicit comment, note, ADR, or task requirement preserving it.

## Source References

Read these references before implementing or reviewing Web UI:

1. `references/design-rules.md` — product-level visual policy.
2. `references/component-patterns.md` — concrete HeroUI v3.2.1 implementation patterns, reset rules, and code-level checks.
3. `references/heroui-v3.2.1-off-the-shelf-reset.md` — detailed inventory for removing the previous custom visual layer.

## Use When

Use this skill when creating, modifying, or reviewing:

- React UI components: cards, buttons, forms, dialogs, tables, badges, alerts, loading states, and empty states.
- Page layouts, navigation, app chrome, account/auth screens, dashboards, and module pages.
- Global CSS and theme setup in `src/Hosts/Web`.
- Any file importing local design-system primitives or adding visual `className`, inline style, custom CSS variables, or custom Tailwind styling.

Apply it especially when:

- A component uses `ds-*`, `glass-card`, `auth-bg`, custom CSS variables, gradients, shadows, blur, or custom color utilities.
- A HeroUI component is visually restyled through `className` or inline `style`.
- A wrapper component changes the default appearance of HeroUI components.
- A UI change risks creating a new platform-specific visual language.

## Do Not Use For

- Component composition and data flow → `platform-feature-ui-integration-steward`.
- Feature hooks, query invalidation, mutation handling → `platform-feature-ui-integration-steward`.
- File placement and module ownership → `platform-artifact-placement-steward`.
- HeroUI component API, props, and examples → `heroui-react`.
- Hard-coded text and localization → `platform-localization-maintenance`.
- Frontend/backend dependency boundaries → `platform-frontend-backend-isolation`.
- OpenAPI contracts and generated clients → `platform-openapi-contract-steward` / `platform-generated-react-client-steward`.

## How This Skill Works

### Step 1: Read the Policy

Read `references/design-rules.md` to understand the visual policy. The policy is intentionally short and conceptual.

### Step 2: Apply the Framework Pattern

Read `references/component-patterns.md` for concrete HeroUI v3.2.1 implementation rules. Use HeroUI defaults first. Treat custom styling as an exception, not a starting point.

### Step 3: Remove Unapproved Custom Design

When existing custom styling appears, remove it unless an explicit exemption or accepted requirement says it is deliberate. Use `references/heroui-v3.2.1-off-the-shelf-reset.md` for the reset inventory.

## Core Rules

1. **HeroUI defaults are the baseline.** Standard HeroUI appearance is the expected result.
2. **Do not create custom styling/design unless explicitly stated.** A vague desire for polish, brand, or better look is not enough.
3. **Remove existing custom styling/design unless explicitly preserved.** Preservation requires an in-code exemption, ADR, or task requirement.
4. **Keep implementation detail in references.** The skill routes decisions; `component-patterns.md` and reset references hold the exhaustive rules.
5. **Prefer framework props over styling.** Use HeroUI components, props, variants, and accessibility APIs before adding wrappers or classes.
6. **Wrappers must not restyle HeroUI.** Wrapper components may compose behavior, data, or accessibility, but not impose a visual language.
7. **Accessibility is not optional.** Removing visual customization must not remove labels, focus behavior, pending states, invalid states, or semantics.

## Opt-Out Mechanism

A component may keep or introduce custom visual styling only with a narrow exemption:

```tsx
// UI-DESIGN-EXEMPTION: <rule name>. <specific functional reason>. This custom styling is deliberate.
```

The exemption must name the rule being overridden and explain why HeroUI's default appearance or props cannot satisfy the requirement. Generic reasons like "looks better", "more polished", or "old design" are not valid.

## Review Procedure

When reviewing UI artifacts, verify:

1. **Policy alignment:** Does it follow HeroUI off-the-shelf appearance by default?
2. **Reference alignment:** Does it follow `component-patterns.md` and the reset reference?
3. **Custom styling:** Is every custom visual style either removed or explicitly exempted?
4. **Framework use:** Are HeroUI components and props used before local styling?
5. **Accessibility:** Are labels, semantics, focus, errors, disabled states, and pending states preserved?

## Required Output

When reviewing or planning UI work, include:

```md
## UI Design Review

### Component: <name>
### Policy: HeroUI v3.2.1 off-the-shelf appearance

| Check | Status |
| --- | --- |
| HeroUI default appearance | Pass / Fail — <detail> |
| No unapproved custom styling | Pass / Fail — <detail> |
| Reference alignment | Pass / Fail — <detail> |
| Accessibility | Pass / Fail — <detail> |

### Findings
| Severity | Issue | Fix |
| --- | --- | --- |
| Must-fix / Should-fix | <description> | <action> |

### Decision: <accept / fix required / reject>
```

## Handoff

Use this skill after artifact placement is confirmed and before or alongside feature integration.

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

Be direct and practical. Keep the skill concise. Put detailed implementation rules in reference files. Focus on whether custom styling is explicitly intended or should be removed.
