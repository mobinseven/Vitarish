# Prepared actions

Status: Active  
Owner: Platform module: intelligence  
Scope: App-agnostic module documentation

A prepared action is a change an assistant tool proposed and a person must
confirm before it is carried out ([ADR-0022](../../../architecture/decisions/0022-guided-surfaces.md)).
Intelligence owns how it lives; each app owns what it means.

## What Intelligence owns

- The record and its state machine:
  `src/Modules/Intelligence/Domain/PreparedActions/PreparedAction.cs`.
  Proposed → Confirming → Completed, with Rejected, Expired and Failed. A
  retryable failure may be confirmed again.
- The lifecycle (prepare, get, confirm, reject, revise, expiry):
  `src/Modules/Intelligence/Application/PreparedActions/PreparedActionLifecycle.cs`.
  Every call checks, in order, that the action exists, matches the caller's
  agent, scope and subject, was requested by the caller, and is allowed by the
  app's policy. A scope mismatch is reported as not found.
- The store port and its Redis adapter
  (`Infrastructure/Services/PreparedActions/RedisPreparedActionStore.cs`). Records
  live under the `intelligence:prepared-action:` key prefix until one day after
  they expire. Every transition is a compare-and-swap on the stored value.

## What an app supplies

- An `IPreparedActionApp` built per authenticated request: the caller, the
  policy revision and retry classification per action type, authorization, and
  the committer that carries an action out and re-authorizes.
- Its own confirm, reject, revise and read routes, which delegate to the lifecycle.
  Intelligence exposes no public action routes.

Clinic's adapter is
`src/Apps/Clinic/Application/Assistant/Actions/ClinicPreparedActions.cs`.

## Testing

- Transition table, lifecycle and Redis store tests:
  `tests/Modules/Intelligence.Tests/PreparedActions`. The Redis tests need
  Docker and skip without it.
- The E2E host replaces the model provider with a scripted runner
  (`src/Hosts/E2e/Infrastructure/Events/E2eScriptedAgentRunner.cs`): a message
  starting with `e2e-script:` runs the given tool calls through the real guarded
  tools and returns the given structured result.
