# Architecture Rationale for Agent Rules

This document explains the reasoning behind the repository invariants in the root [`AGENTS.md`](../AGENTS.md). It is guidance for architecture-sensitive work, reviews, and policy maintenance. It does not add new binding rules; the tagged rules in `AGENTS.md` remain authoritative.

## Why the original ten principles became eight

No architectural concern was removed. Two pairs were consolidated because each pair protected the same underlying boundary:

| Original principle | Final owner |
| --- | --- |
| One responsibility per artifact | Rule 1: Cohesion |
| One place where a domain concept is true | Rule 2: Authoritative ownership |
| Minimize component knowledge | Rule 3: Narrow knowledge |
| Depend on contracts rather than infrastructure implementations | Rule 4: Dependency direction |
| Separate commands and queries | Rule 5: Commands and queries |
| Validate input and handle failures explicitly | Rule 6: Explicit boundaries and failure behavior |
| Prefer extension over risky edits | Rule 7: Cohesive edits before invented extension points |
| Keep domain logic independent of frameworks and infrastructure | Rule 4: Dependency direction |
| Avoid speculative abstraction | Rule 8: Current requirements before speculative generality |
| One source of truth; avoid duplicated rules and values | Rule 2: Authoritative ownership |

The original Rules 4 and 8 both protected inward dependency direction and core independence. Keeping them separate repeated the same boundary from two angles. The original Rules 2 and 10 both protected authoritative ownership and prevented independently drifting copies. Combining them permits legitimate DTO, persistence, cache, index, and UI representations while still prohibiting duplicated authority.

## 1. Cohesion

The useful question is not "does this file contain more than one kind of statement?" It is "what independent future requests would force this artifact to change?"

A business policy and SQL mapping change for unrelated reasons, so mixing them raises the chance that a persistence edit changes behavior or a policy edit damages storage. By contrast, a composition root legitimately changes when system composition changes, even though it wires several concrete services. A form component may own rendering, accessibility labels, and local interaction state because those concerns evolve together as one UI behavior.

Split when the responsibilities have independent owners, lifecycles, tests, or change triggers. Keep them together when splitting would create pass-through files, artificial interfaces, or navigation without reducing coupling.

## 2. Authoritative ownership

A platform cannot avoid multiple representations. A patient, account, order, or API operation may appear as a domain model, database row, transport DTO, generated client type, search document, cache record, and view model. The architectural risk is not repetition itself. The risk is allowing several representations to independently define the meaning of the concept.

For each rule or state, identify:

1. The authoritative owner
2. Which representations are derived or transported
3. The mapping or generation path
4. How updates propagate
5. What detects drift

Examples:

- An OpenAPI document owns the generated client shape; generated DTOs must not be edited.
- A domain type owns a business invariant; controllers and UI may reject obviously malformed input but must not redefine the invariant.
- A search index is a derived read model; the transactional store or event stream remains authoritative, and reindexing is the recovery path.
- Localization resources may contain translated copy, while message identifiers and interpolation contracts have a single owner.

Duplicating literal test data is often harmless. Duplicating the policy that interprets it is not.

## 3. Narrow knowledge

Coupling grows when one artifact knows another artifact's internal structure instead of the capability it needs. Typical warning signs include reaching through several properties, reading persistence fields from presentation code, depending on generated implementation details, or passing a large service when a single operation or immutable value would suffice.

The remedy is the narrowest stable contract appropriate to the boundary. That may be an interface, function, record, immutable value, internal public method, or module API. Interfaces are not inherently superior and should not be created mechanically.

Narrow knowledge improves replaceability, but its immediate value is local reasoning: a contributor can understand and test the caller without reconstructing the callee's internals.

## 4. Dependency direction

Domain logic describes what the system means. Application logic coordinates what the system does for a use case. Infrastructure implements interactions with databases, networks, filesystems, clocks, queues, and vendors. Presentation translates external interaction into application requests and results.

The intended direction is:

```text
Presentation ------> Application ------> Domain/Core
Infrastructure ----> Application ------> Domain/Core
```

Domain and application code must not import concrete outward implementations. Application code may define and consume ports such as `IClock`, `IEmailSender`, or a domain-specific repository when the use case genuinely requires that capability. Infrastructure implements those ports.

A port is justified when it protects an architectural boundary, represents a meaningful capability, controls nondeterminism, or supports an existing alternate implementation. A one-to-one interface that merely repeats every method of a concrete SDK usually adds ceremony rather than protection.

## 5. Commands and queries

A caller should know whether invoking an operation changes externally observable state. Queries read without mutating business state. Commands request a mutation and may return the minimum outcome required by the caller.

Useful command results include identifiers, versions, validation results, and operation receipts. Returning such data does not violate command-query separation. The problem begins when a command also becomes the convenient general-purpose way to fetch unrelated state, or when a query produces hidden writes, sends messages, updates timestamps, or changes caches in a way that affects business behavior.

This rule is about semantic clarity and retry safety. It does not require separate databases, separate models, command buses, or mediator libraries.

## 6. Explicit boundaries and failure behavior

Different boundaries own different validation:

- Presentation validates transport syntax and shape.
- Application validates use-case preconditions and authorization context.
- Domain owners enforce business invariants.
- Infrastructure validates assumptions about external responses and storage behavior.

This is not an instruction to copy the same validator into every layer. Each boundary validates what only that boundary can know, while invariants remain centralized.

For operations that can fail, define what happens under cancellation, timeout, dependency outage, duplicate delivery, concurrent updates, partial completion, and retries. A retry policy without idempotency analysis can multiply a failure. Swallowing an exception converts an observable failure into corrupted state or misleading success.

## 7. Cohesive edits before invented extension points

The Open/Closed Principle is frequently misapplied as "never edit stable code." Existing code should be edited when the new behavior belongs to its current responsibility and the change is cohesive. Avoiding a five-line edit by adding a strategy, factory, registration path, and interface increases the number of places every future reader must inspect.

Prefer an existing extension point when it was designed for the variation being added. Introduce a new extension point when multiple current variants or an explicit plug-in requirement justify it. Do not build one merely because future changes are imaginable.

## 8. Current requirements before speculative generality

Every abstraction has carrying cost: naming, documentation, tests, registration, navigation, compatibility, and eventual removal. The cost is paid immediately; the guessed future benefit may never arrive.

Solve the current requirement with the simplest structure that preserves correctness and known boundaries. This does not mean choosing an irreversible shortcut. When two designs are equally simple today, prefer the one with a cheaper migration path. It means avoiding extra machinery whose only justification is an imagined requirement.

## Applying the principles together

The rules constrain each other:

- Cohesion prevents dumping unrelated behavior into one owner.
- Authoritative ownership prevents splitting a rule into competing owners.
- Narrow knowledge and dependency direction protect boundaries.
- Command-query clarity and explicit failure behavior make effects predictable.
- Cohesive edits prevent abstraction for its own sake.
- Current-requirement focus limits the total machinery introduced by all other rules.

When principles appear to conflict, choose the smallest design that preserves correctness, authority, and dependency direction. Record a decision only when the trade-off is durable or difficult to reverse; do not create an ADR for ordinary local implementation choices.

## Examples of acceptable deliberate duplication

Duplication can be appropriate when ownership and synchronization remain clear:

- Generated clients derived from OpenAPI
- Database read models and indexes derived from transactional data
- Cached representations with explicit invalidation or expiry
- Test fixtures that intentionally describe scenarios independently
- Localized strings keyed by authoritative message identifiers
- Versioned API contracts during a compatibility window
- Expand-migrate-contract database shapes during rolling deployment

Extract duplication only when the repeated elements must evolve together under one owner. Similar-looking code that represents independent policies should remain independent.

## Policy maintenance

Update the root `AGENTS.md` when a rule applies to nearly every task or repeated review feedback shows that agents consistently make the same repository-wide mistake. Put guidance in a nested `AGENTS.md` when it applies only to a subtree. Put repeatable task procedures in skills. Put concrete topology and dependency matrices in `docs/architecture.md`.

Remove or consolidate guidance when it becomes redundant, unenforceable, obsolete, or better enforced by tooling. Prefer linters, analyzers, generators, tests, and CI checks over prose whenever a rule can be verified mechanically.
