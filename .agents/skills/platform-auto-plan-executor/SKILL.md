---
name: platform-auto-plan-executor
description: Execute implementation plans by routing to appropriate skills and generating code changes.
---

# Platform Auto Plan Executor

## Purpose

Execute plan files by loading relevant skills, generating code changes, and creating PRs.

## Use When

- Plan file exists in `.mimocode/plans/`
- Loop state indicates implementation needed
- GitHub Actions triggers implementation workflow

## Execution Process

1. **Read Plan**: Parse plan file and extract phases
2. **Load Skills**: Load skills specified in plan
3. **Execute Phases**: Run each phase sequentially
4. **Run Validation**: Execute validation commands
5. **Create Branch**: Create implementation branch
6. **Generate Changes**: Create/modify files as specified
7. **Commit Changes**: Commit with descriptive messages
8. **Create PR**: Create pull request with plan reference
9. **Update State**: Update loop-state.json

## Phase Execution

For each phase:

1. Read phase objective and artifacts
2. For each artifact:
   - Load artifact placement skill
   - Determine correct location
   - Generate artifact following skill rules
   - Validate against architecture rules
3. Run phase validation
4. Update progress

## Code Generation Rules

- Follow existing code conventions
- Use existing libraries and patterns
- Respect architecture boundaries
- Include required tests
- Add necessary documentation
- Follow AGENTS.md rules

## PR Creation

PR must include:
- Reference to issue number
- Plan file reference
- Iteration number
- Summary of changes
- Testing checklist
- Next steps

## Error Handling

- If phase fails: log error, update state, continue or abort
- If validation fails: fix issues, re-run validation
- If build fails: fix build errors, re-run build
- If tests fail: fix tests, re-run tests

## Guardrails

- Never modify plan file during execution
- Always run validation before committing
- Always create new commits, never amend
- Always reference issue in PR
- Never skip required skills