---
name: platform-loop-state-manager
description: Manage loop-state.json lifecycle, track iterations, handle termination, and coordinate workflow transitions.
---

# Platform Loop State Manager

## Purpose

Manage the implementation loop state, track progress, handle termination conditions, and coordinate between workflows.

## Use When

- Starting new implementation loop
- Updating loop state
- Checking termination conditions
- Handling failures
- Cleaning up completed loops

## State Schema

```json
{
  "issue_number": 123,
  "issue_url": "https://github.com/...",
  "iteration": 1,
  "max_iterations": 5,
  "status": "planning | plan_generated | implementing | implementation_complete | awaiting_review | review_complete | replan_generated | completed | failed | escalated",
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
      "action": "plan_generated"
    }
  ],
  "created_at": "...",
  "updated_at": "...",
  "completed_at": null
}
```

## State Transitions

```
planning → plan_generated → implementing → implementation_complete → awaiting_review → review_complete
    ↓                              ↓                                    ↓
    ↓                         implementation_failed                changes_needed → replan_generated → (loop)
    ↓                              ↓                                    ↓
    ↓                              ↓                              approved → completed
    ↓                              ↓
    ↓                         escalated (max iterations)
    ↓
    failed
```

## Termination Conditions

### Normal Termination
- Status = "completed"
- Plan file deleted
- Issue labeled "status: completed"

### Failure Termination
- Status = "failed"
- Error logged
- Issue commented with error details

### Escalation Termination
- Iteration >= max_iterations
- Status = "escalated"
- Issue commented requesting manual review
- Issue labeled "status: stuck"

## State Operations

### Initialize
```json
{
  "issue_number": 123,
  "iteration": 0,
  "max_iterations": 5,
  "status": "planning",
  "history": [],
  "created_at": "now"
}
```

### Update
- Add to history
- Update status
- Update timestamp
- Save to file

### Check Termination
- If status in ["completed", "failed", "escalated"]: stop
- If iteration >= max_iterations: escalate
- If plan file missing and status != "completed": handle error

## Error Handling

### Build Failure
- Status: "implementation_failed"
- Log error details
- Comment on issue
- Wait for manual intervention

### Test Failure
- Status: "implementation_failed"
- Log test results
- Comment on issue
- Wait for manual intervention

### Review Failure
- Status: "review_failed"
- Log review error
- Retry once
- If still fails: escalate

## Cleanup

When completed:
1. Delete plan file
2. Update loop state to "completed"
3. Add completion timestamp
4. Comment on issue with summary
5. Add "status: completed" label

## Guardrails

- Never modify state from multiple workflows simultaneously
- Always validate state before transition
- Always log state changes
- Always handle errors explicitly
- Never leave state in inconsistent condition