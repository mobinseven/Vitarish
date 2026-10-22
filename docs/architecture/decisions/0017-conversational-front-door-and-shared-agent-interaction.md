# ADR-0017: Conversational Front Door and the Shared Agent Interaction Layer

Date: 2026-10-22
Status: Accepted
Scope: Landing app, Intelligence module and `shared/intelligence`, Authentication sign-in, Parties host policy and onboarding, Users profile storage, `shared/auth`
Owner: Architecture
Supersedes:
Superseded by:
Related: [ADR-0011](0011-server-side-agents-sdk-runtime.md), [ADR-0012](0012-module-owned-agents-with-guarded-composition.md), [ADR-0016](0016-self-service-clinic-onboarding.md)

## Context

The public landing page is a generic chat shell. Its assistant has a one-line
prompt, returns plain text, knows nothing about the product, and offers no next
step. The owner wants the landing page to become a conversation that guides a
business owner, currently a clinic owner, from a first question to their own
clinic: tappable questions, proof drawn from real features, voice, and answers
that carry into sign-up.

The owner also expects chat-based interaction and agentic tools to become
central across every Vitarish app. Anything built for this page must therefore
be reusable by other apps, not owned by the landing app.

Several existing boundaries are involved:

- Sessions without a role are admitted only on the onboarding and sign-in hosts
  (ADR-0016, `IRoleFreeSessionPolicy`), so a new phone user cannot stay signed
  in on the landing host.
- Live dictation is reachable only from clinic screens and through the
  protected Intelligence group with an exact trusted origin.
- The user profile already stores a JSON dictionary (`User.ProfileJson`), but
  the profile update replaces the whole dictionary.
- Phone sign-in finds or creates the account, so signing in by phone is also
  signing up.

## Decision

1. **Shared interactive replies.** The Intelligence module owns an app-agnostic
   interactive reply: text, at most one question with a small set of options,
   references to items the calling app declares, and at most one next action
   from a set the calling app declares. Intelligence owns its shape, schema and
   validation. The model output is untrusted: unknown references or actions are
   dropped, lengths are capped, and invalid output falls back to plain text.
   Apps own their prompt, their catalog of referable items, their action set,
   and their wording. The wire shape lives in `Intelligence.Contracts`, which an
   app's Contracts project references as an approved shared contract library;
   the app's Api maps the Application reply to it. The landing app exposes one
   public, rate-limited catalog endpoint with each feature's availability, the
   consultation's questions and answer keys, and the next-action keys, so the
   page, the assistant and the saved setup draft share one set of keys.
2. **Shared interaction UI.** `shared/intelligence` owns rendering of
   interactive replies (question options, reference chips with their status,
   the next-action bar, the "remembered" note and the side panel shell);
   `shared/auth` owns the inline phone sign-in card; `@vitarish/ui` keeps the
   generic chat primitives. Apps supply content, never their own copies of
   these parts.
3. **Role-free sessions on the landing host.** The landing host joins the
   onboarding and sign-in hosts in `IRoleFreeSessionPolicy`. Its tenant holds no
   practice data, so the reasoning of ADR-0016 rule 3 applies unchanged: the
   session may use account endpoints, landing endpoints and the handoff to
   onboarding; every tenant endpoint still refuses it. Quick sign-in happens
   inside the conversation with the existing phone sign-in endpoints.
4. **Dictation for every signed-in user.** Live dictation is available to any
   signed-in user, including a role-free session on the landing host. The
   existing per-session limits, origin check and "audio is never stored" rule
   stay. No per-user daily limit is added (owner decision).
5. **Handoff from landing to onboarding.** A signed-in landing user who chooses
   to open a clinic receives the same one-time handoff to the onboarding host
   that the sign-in host issues today.
6. **What Vitarish learns about a user.** Users owns profile storage and gains
   a merge-by-key write, so no writer replaces another writer's keys.
   - *Structured answers* (the clinic setup draft: clinic kind, booking method,
     chosen pains, team size) are stored structured under a Parties-owned
     onboarding key, and sign-up and the first-run walkthrough read them.
   - *Free-text knowledge* is user memory, owned by Intelligence and stored
     under its own key. Only landing and onboarding conversations may add to
     it; clinic conversations never do, and patient details are never stored.
     Each saved item shows a quiet inline "Remembered" note with undo, and the
     account lists every item for the user to see, edit or delete, including
     "forget everything".
   No new table is added while the profile JSON is enough.
7. **Landing content.** The old About, Portfolio, Explore and Contact pages and
   the chat shell are removed. "Talk to a person" opens WhatsApp with a summary
   the user sends themselves.

## Consequences

- One conversation surface and one reply contract serve every app; a second app
  adopts them by declaring its catalog and actions.
- The landing host becomes another place a role-free session exists. It is
  bounded by the same tenant refusal as the other two hosts and needs its own
  authorization-matrix row and two-host E2E proof.
- Opening dictation to role-free users means free sign-ups can consume
  speech-to-text capacity. Per-session limits and the phone sign-in rate limits
  are the only controls; this is an accepted cost risk.
- User memory holds business context about real people. It must stay visible,
  correctable and deletable, and its source restriction is a privacy control,
  not a convenience.
- The profile JSON grows with memory. When it needs search, history,
  retention rules or more than a few dozen items, it moves to its own store.
- Model output can fail the schema (a known local failure mode reports schema
  errors as `ProviderFailure`), so the plain-text fallback is mandatory.
- Rollback: remove the landing host from the role-free policy and redeploy the
  previous landing page. The interactive reply fields are optional additions to
  the landing response, and the new profile keys are ignored by older code. No
  migration is involved.

## Alternatives Considered

### Option 1: Shared layer, role-free landing sessions, profile-backed memory (chosen)

Keeps the conversation in one place, makes every new part reusable, and needs no
schema migration.

### Option 2: Sign in through the shared sign-in host and return

Rejected: the landing page would never know the user, so voice, memory and the
draft could not work there, and the visitor leaves the conversation.

### Option 3: Landing-only components and contract

Rejected: the owner expects this interaction model across all apps; a
landing-owned copy would have to be extracted later and would drift meanwhile.

### Option 4: A dedicated memory table now

Deferred: the profile JSON is sufficient for the current volume. See the
revision trigger under Consequences.

### Option 5: A scripted question tree without the model

Rejected: it cannot answer free-form questions. The four consultation steps
remain fixed, page-owned UI so the flow still works when the model is
unavailable.

## Validation / Enforcement

- Intelligence tests cover reply validation: unknown references and actions,
  length caps and fallback.
- Parties and Authentication tests cover the landing host in the role-free
  policy and the landing handoff, including refusal on clinic tenants.
- Users tests cover merge-by-key writes; Intelligence tests cover the memory
  source restriction.
- The two-host E2E proof covers quick sign-in on the landing host, dictation
  and the handoff into onboarding.
- `docs/security/authorization-matrix.md` lists the landing role-free session,
  dictation for role-free users and the landing handoff.

## Notes

- Product wording that follows from these decisions (free with no time limit,
  WhatsApp as the human channel) is recorded in the landing app's `PRODUCT.md`.
- The landing page currently calls its API with a hand-written client instead of
  a generated one; that is pre-existing and tracked separately.
