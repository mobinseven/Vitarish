---
name: platform-loop-state-manager
description: Manage implementation loop state, legal transitions, iteration count, termination, escalation, and cleanup without owning plan generation, execution, or review verdicts.
---

# Platform Loop State Manager

## Purpose

Manage the automated implementation loop state.

This skill owns status, iteration count, transition validity, termination, escalation, and cleanup. It does not generate plans, execute changes, or review implementation quality.

## Use When

Use this skill when:

- starting a new automated implementation loop;
- updating `loop-state.json`;
- checking transition validity;
- recording plan generation, execution, review, re-plan, completion, failure, or escalation;
- deciding whether the loop should continue or stop;
- cleaning up completed loop artifacts.

## Do Not Use For

Do not use this skill for:

- writing implementation plans; use `platform-auto-plan-generator`;
- applying code changes; use `platform-auto-plan-executor`;
- judging implementation quality; use `platform-auto-loop-reviewer`;
- normal human-directed PR workflow; use `platform-pr-delivery-loop`.

## Relationship to Other Skills

This skill is the state coordinator for the automated loop.

```txt
issue -> state manager -> plan generator -> state manager -> plan executor -> state manager -> loop reviewer -> state manager -> complete/re-plan/escalate
```

It consumes reports and verdicts from the other loop skills and records the next legal state.

Use `.agents/skills/README.md` for the shared skill operating model.

## Sources of Truth

- `loop-state.json` for current loop state.
- The source GitHub issue.
- Current plan file path under `.mimocode/plans/`.
- Current PR number/URL when implementation exists.
- Reports from generator, executor, and reviewer.
- `AGENTS.md` for architecture rules that may require escalation.

## State Schema

```json
{
  "issue_number": 123,
  "issue_url": "https://github.com/...",
  "iteration": 1,
  "max_iterations": 5,
  "status": "planning | plan_generated | implementing | implementation_complete | awaiting_review | review_complete | changes_needed | replan_generated | completed | failed | escalated",
  "plan_file": "1234567890-swift-river.md",
  "pr_number": 456,
  "pr_url": "https://github.com/...",
  "results": [],
  "review_feedback": null,
  "error": null,
  "history": [
    {
      "iteration": 1,
      "status": "planning",
      "timestamp": "...",
      "action": "initialized"
    }
  ],
  "created_at": "...",
  "updated_at": "...",
  "completed_at": null
}
```

## Legal State Transitions

```txt
planning -> plan_generated
plan_generated -> implementing
implementing -> implementation_complete
implementation_complete -> awaiting_review
awaiting_review -> review_complete
review_complete -> completed | changes_needed

changes_needed -> replan_generated
replan_generated -> implementing

any active state -> failed | escalated
```

Do not invent new status values without updating the schema, workflows, and related skills.

## Operating Procedure

1. Read the current state.
2. Validate the requested transition against the legal transition list.
3. Check iteration limits before starting re-plan or re-execution.
4. Apply exactly one state transition per operation.
5. Append a history entry with timestamp, iteration, previous status, next status, and reason.
6. Update references to plan file, PR, reports, review feedback, or error only when the owning skill produced them.
7. Persist the state atomically when possible.
8. Return the next workflow action.

## Required Output

```md
## Loop State Update

Issue: #<number>
Previous status: <status>
New status: <status>
Iteration: <n>/<max>

### Reason
- <why this transition is valid>

### Recorded Inputs
- Plan: <path or none>
- PR: <number/url or none>
- Review verdict: <approved/changes_needed/escalate/none>
- Error: <none or summary>

### Next Action
- <generate plan / execute plan / review PR / re-plan / complete / escalate / stop>
```

## Termination Conditions

### Normal Termination

- Status = `completed`.
- Review verdict is approved.
- Completion summary is posted or recorded.
- Plan cleanup is done or intentionally deferred with reason.

### Failure Termination

- Status = `failed`.
- Error is recorded.
- Issue/PR is updated with the failure summary when the workflow supports it.

### Escalation Termination

- Iteration >= `max_iterations`.
- Status = `escalated`.
- Human review is requested.
- The reason for escalation is explicit.

## Hard Stops

Stop and escalate when:

- state is missing, malformed, or has an unknown status;
- requested transition is illegal;
- multiple workflows appear to be modifying state at the same time;
- iteration limit is reached;
- source plan or PR reference is missing for a state that requires it;
- review verdict requires a decision that automation cannot make;
- repeated validation or implementation failures occur.

## Guardrails

- Never modify state from multiple workflows simultaneously.
- Never skip history entries.
- Never leave active state without a next action.
- Never mark `completed` without an approved review or explicit human override.
- Never delete the only evidence for failure, escalation, or review feedback.
- Keep loop state mechanical; do not bury product or architecture decisions in it.

## Handoff

Return the next action only. The next action is performed by the owning skill:

| Next action | Skill |
| --- | --- |
| Generate or re-generate plan | `platform-auto-plan-generator` |
| Execute plan | `platform-auto-plan-executor` |
| Review PR | `platform-auto-loop-reviewer` |
| Human-directed follow-up | `platform-pr-delivery-loop` or human maintainer |

## Style

Be deterministic and conservative. Prefer explicit state and reason over prose.
