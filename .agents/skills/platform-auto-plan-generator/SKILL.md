---
name: platform-auto-plan-generator
description: Generate implementation plans from GitHub issues with objective, scope, constraints, and skill routing.
---

# Platform Auto Plan Generator

## Purpose

Convert GitHub issues into structured plan files that can be executed by the implementation loop.

## Use When

- GitHub issue labeled "auto-implementation"
- User requests automated implementation
- Converting requirements into executable plans

## Plan Generation Process

1. **Parse Issue**: Extract objective, scope, constraints, required skills
2. **Classify Work**: Determine if journey, roadmap, ADR, or implementation needed
3. **Route Skills**: Load appropriate skills based on work type
4. **Generate Phases**: Break work into executable phases
5. **Validate Plan**: Ensure plan follows schema and architecture rules
6. **Output Plan**: Write plan file to `.mimocode/plans/`

## Required Sections

Every plan must include:

```markdown
# Plan: {title}

## Metadata
- Issue: #{number}
- Generated: {timestamp}
- Iteration: {n}/{max}
- Skills Required: {list}

## Objective
{one paragraph}

## Scope
### In Scope
- {item}
### Out of Scope
- {item}

## Constraints
- {constraint}

## Phases

### Phase 1: {name}
#### Objective
{what this phase achieves}

#### Artifacts
| Artifact | Type | Responsibility | Location |
|----------|------|----------------|----------|
| {name} | {type} | {one sentence} | {path} |

#### Implementation Steps
1. {step}

#### Validation
```bash
{command}
```

#### Acceptance Criteria
- [ ] {criterion}

## Verification
1. {check}
```

## Skill Routing Rules

| Work Type | Skills to Load |
|-----------|----------------|
| User workflow | platform-user-journey-curator |
| Roadmap/planning | platform-feature-roadmap-curator |
| Architecture decision | platform-adr-steward |
| Artifact placement | platform-artifact-placement-steward |
| Testing | platform-test-curator |
| Implementation | platform-pr-delivery-loop |

## Guardrails

- Do not generate plans without clear objective
- Do not skip architecture validation
- Do not create plans that violate AGENTS.md rules
- Do not exceed reasonable scope (max 50 files per plan)
- Always include validation commands
- Always include acceptance criteria