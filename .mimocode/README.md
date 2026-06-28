# MiMoCode Automated Implementation Loop

## Overview

This system automates the implementation of features from GitHub issues through a recursive loop:

1. **Planning**: Issue → Plan file
2. **Implementation**: Plan file → PR
3. **Review**: PR → Feedback
4. **Re-plan**: Feedback → New plan (if needed)
5. **Loop**: Continue until complete

## Usage

1. Create a GitHub issue with the "auto-implementation" label
2. Fill in the issue template (Objective, Scope, Constraints)
3. The system will automatically:
   - Generate a plan file
   - Implement the plan
   - Create a PR
   - Review the implementation
   - Generate new plans if changes are needed
   - Continue until approved

## Configuration

- Max iterations: 5 (configurable in loop-state.json)
- Termination: Plan file deleted or status = completed
- Failure: Escalate to human after max retries

## Files

- `.mimocode/plans/` - Plan files
- `.mimocode/loop-state.json` - Loop state
- `.mimocode/schemas/plan-schema.json` - Plan schema
- `.mimocode/scripts/` - Helper scripts

## Workflows

- `issue-to-plan.yml` - Converts issues to plans
- `implement-plan.yml` - Executes plans
- `auto-review.yml` - Reviews implementations
- `loop-controller.yml` - Manages loop state

## Skills

- `platform-auto-plan-generator` - Generates plans
- `platform-auto-plan-executor` - Executes plans
- `platform-auto-loop-reviewer` - Reviews implementations
- `platform-loop-state-manager` - Manages state