# ADR-0022: Guided Surfaces

Date: 2026-11-12
Status: Accepted
Scope: Intelligence module and `shared/intelligence`; every app that offers an assistant
Owner: Architecture
Supersedes:
Superseded by:
Related: [ADR-0007](0007-standardize-react-localization.md), [ADR-0012](0012-module-owned-agents-with-guarded-composition.md), [ADR-0017](0017-conversational-front-door-and-shared-agent-interaction.md), [ADR-0020](0020-platform-home-and-app-aware-workspaces.md)

## Context

Software usually asks people to learn it: where each menu is, which screen
holds which task, in what order to fill things in. Vitarish wants a second way
in. A person states what they want in their own words, and an assistant
answers with working pieces of interface, such as a question with choices, a
card to review and confirm, a checklist of what is left, or a link to the right
screen. The person can act on those pieces where they appear. Knowing the
menus becomes optional; the menus and screens remain complete for people who
prefer them.

This model must look and behave the same in every app, and must stay safe even
though a language model, whose output cannot be trusted, decides what to show.
If each app built it alone, the safety rules, the components and the
confirmation process would drift apart.

ADR-0017 already makes one piece shared: a validated interactive reply (text,
one question with options, references, one next action) and its renderer.
This decision completes the model around it.

## Terms

- **Guided surface:** the interface an assistant assembles in a conversation
  from predesigned components.
- **Block:** one component instance in a reply. It has a *kind* and the data
  that kind needs.
- **Vocabulary:** the closed set of block kinds, reference keys and action keys
  an app allows its assistant to use.
- **Prepared action:** a change proposed by an assistant tool, stored with a
  review payload, and carried out only according to its action policy,
  normally after a person confirms it.
- **Run:** one assistant turn. Everything prepared in one run belongs together.

## Decision

1. **One reply format.** A reply is text plus an ordered list of blocks.
   Intelligence owns the block envelope, its validation and its contract in
   `Intelligence.Contracts`. ADR-0017's rules apply to every block: kinds,
   references and actions outside the app's vocabulary are dropped, lengths
   are capped, and anything malformed falls back to text. ADR-0017's
   interactive reply fields are read as blocks. In an app's conversation
   response, blocks are an optional field added beside its existing fields.

2. **Three producers, one renderer.**
   - *The model* produces only small blocks: a question with options,
     references and one next action. The model's output schema stays small,
     because larger schemas fail more often; it never becomes a union of every
     kind.
   - *Tools* produce rich blocks, such as a review group of prepared actions or
     a record summary. An Intelligence-owned run collector gathers the blocks
     that tool results emit during a run and attaches them to the reply. Such
     blocks refer to server records by ID, and the renderer re-reads each
     record's current state by ID rather than trusting the copy in the reply.
   - *A server policy, without a model call,* produces an opening surface, such
     as a welcome and a checklist of what is left. A surface therefore works
     when the model is slow or unavailable.
   Blocks built by tools or server policies carry keys and data, not display
   text; the web localizes them (ADR-0007). Only the model's own text and
   option labels arrive as text.

3. **Trust.** Model output reaches a component only through the validated
   reply or through a tool's prepare step, which validates the arguments and
   builds the review payload. The model never introduces kinds or keys outside
   the vocabulary. For an action whose policy requires confirmation, only what
   a person confirmed is carried out. Actions an app's policy allows directly,
   and ADR-0017's user memory with its undo, keep their own rules.

4. **Two levels of kinds.**
   - *Platform kinds* are owned by Intelligence (contract and validation) and
     `shared/intelligence` (rendering): question with options, reference chips,
     next action, action review group, goal checklist and destination link.
   - *App kinds* are declared by an app's capability pack. The app passes their
     renderers, and the review cards for its action types, to the shared
     renderer through an explicit registry. Nothing is discovered at runtime,
     as in ADR-0012.
   Each kind's key, data schema, validator and renderer has exactly one owner.
   The web receives kind keys from generated contracts, never as literals.

5. **One prepared-action lifecycle.** Intelligence owns how a prepared action
   lives: prepare, revise, confirm, reject, expire, retry of a retryable
   failure, and grouping by run. A revision rejects the original and prepares
   a replacement in the same run, so a proposal never changes after it is made.
   Each action in a group is confirmed on its own; a group is never
   all-or-nothing.
   - *Owners.* The lifecycle commands, the shared policy shape and the ports
     belong to `Intelligence.Application`; the store adapter belongs to
     `Intelligence.Infrastructure`; the record's wire shape belongs to
     `Intelligence.Contracts`. None of it belongs to an Agents project. This
     amends ADR-0012 where it leaves approval and persistence with the app that
     first built them; ADR-0012's rule that profiles and packs never approve,
     execute or persist is unchanged.
   - *Record.* A stored action is app-neutral: action ID, run ID, owning agent
     key, conversation, the app's scope ID, requesting user, capability, action
     type, policy revision, status, timestamps, failure, summary, an opaque
     subject (kind and ID) and opaque review and completion payloads. Every
     lifecycle command checks that the action's agent key and scope match the
     calling app and request before acting.
   - *App side.* Apps keep their action policies (including which actions may
     run without confirmation), committers, payload shapes and authorization,
     registered per agent key. The lifecycle asks the app, through an
     Intelligence-owned port, whether the current request may act on an
     action. The committer authorizes again in the confirming person's request.
   - *Routes.* Confirm, reject and revise are exposed by each app under its own
     authorization and delegate to the shared commands. Intelligence exposes no
     public action routes, so the lifecycle adds no new trust surface. The
     shared review group receives the app's confirm, reject and revise
     functions instead of calling routes itself.

6. **Navigation remains complete.** Every block can be read as text, and every
   next action or destination link leads to an existing screen. A guided
   surface never replaces navigation, and never moves a person into an app they
   did not choose (functional architecture, ADR-0020).

7. **A shared capability by decision.** The owner decided that guided surfaces,
   including the action lifecycle, are a platform capability built once,
   before a second app consumes them. This is a recorded exception to the
   "present need" rule in `src/Apps/AGENTS.md`. The block contract is
   considered stable only after two distinct surfaces use it.

## Consequences

- An app adopts guided surfaces by declaring its vocabulary, action policies,
  committers and app kinds, and by exposing thin confirm, reject and revise
  routes. It writes no state machine, store or generic card.
- **Conversation compatibility.** A change to an agent's instructions, output
  schema or tool exposure changes its compatibility key, and open
  conversations ask once to start again (ADR-0012). A kind produced only by a
  tool result or a server policy does not change the key.
- **Changing the owner of stored actions.** Prepared actions are short-lived.
  When their owner or record format changes, pending actions from the previous
  format are not carried over: confirming one returns not found, and the
  person asks again. The shared store keeps its own key namespace, so older
  code never reads newer records and a rollback loses only proposals made
  after the change. This is an owner-approved exception to
  expand-migrate-contract, valid only while actions live minutes rather than
  days.
- **Past turns.** A guided surface does not promise to redraw earlier blocks
  after a page reload; prepared actions stay reachable by ID.
- **Cost and failure.** Tool-built and server-built blocks reduce dependence
  on strict model output, whose schema failures surface as provider failures
  (ADR-0017). Model cost grows with guided conversations.
- **Framework risk.** The lifecycle is a cross-cutting framework. If no second
  app uses it within two agent features, revisit whether it stays shared.
  ADR-0012's own condition for a second production agent still applies.

## Alternatives Considered

### Option 1: Shared format, shared kinds and shared lifecycle (chosen)

Every reusable part has one owner, and each app supplies only its own rules and
content.

### Option 2: Each app builds its own, extracted when a second app needs it

Rejected by the owner: the first app's components would be moved within the
next feature, and safety rules would drift meanwhile.

### Option 3: The model generates whole interfaces

Rejected: model-authored layouts and data cannot be trusted, validated or
localized, and a large output schema fails more often.

### Option 4: Scripted wizards per app

Rejected: they cannot answer free-form questions, and each app would build its
own.

### Option 5: A compatibility reader for actions in a previous format

Rejected: actions live minutes, so a reader and an extra port would serve a
window shorter than a release.

## Validation / Enforcement

- Intelligence tests cover block validation (kinds outside the vocabulary
  dropped, caps, text fallback), the run collector, and the lifecycle
  (idempotent confirm, revise within the same run, retry only for retryable
  failures, expiry, the app authorization port, refusal on a mismatched agent
  key or scope).
- Architecture validation keeps the lifecycle and block contract out of app
  and Agents projects, and keeps app kinds out of `shared/intelligence`.
- Each app's tests show its confirm, reject and revise routes delegate to the
  shared lifecycle under its own authorization.
- The two-host E2E proof covers a grouped confirmation through the shared
  lifecycle.

## Notes

- Revisit if a group must commit all-or-nothing, if blocks must stream before a
  run ends, if past turns must be redrawn after a reload, if actions must live
  longer than minutes, or if an app needs kinds the platform set does not
  cover.
