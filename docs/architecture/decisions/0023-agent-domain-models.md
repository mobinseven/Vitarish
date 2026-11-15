# ADR-0023: Agent Domain Models

Date: 2026-11-15
Status: Accepted
Scope: Intelligence module; every app that offers an assistant over its own records
Owner: Architecture
Supersedes:
Superseded by:
Related: [ADR-0012](0012-module-owned-agents-with-guarded-composition.md), [ADR-0017](0017-conversational-front-door-and-shared-agent-interaction.md), [ADR-0022](0022-guided-surfaces.md)

## Context

An assistant works for a person who speaks in the words of their own trade. A
clinic coordinator says "patient", "treatment", "visit", "the step is done",
"add a note to the last session". A project lead says "project", "task",
"milestone". The records behind those words are often owned by a generic
module whose own vocabulary is different. One workflow module, for example,
stores *projects* that contain *procedures* that contain an ordered chain of
*events*, and an event can be *committed* and can carry *attachments* and
*reminders*. A clinic uses that module with a patient as the project and a
treatment's ordered steps as the events.

A language model does not know any of this. When an assistant is given only
tools and a sentence of guidance per tool group, the model sees module words
in tool names and descriptions, has no statement of which thing contains
which, does not know what a state such as "committed" means to the user, and
cannot map a user's own phrasing to the right records. It then guesses: it
calls the wrong tool, asks the user to repeat themselves, or answers from one
record when the question needed three. Adding more prose to each tool does not
help, because the knowledge that matters is about how things relate, and no
single tool owns that.

Every app with an assistant meets the same problem, so the way an app states
its domain to the model, and the rules that keep tools consistent with that
statement, must be the same everywhere.

## Terms

- **Concept:** one kind of thing the users of an app talk about, such as a
  patient, a procedure or a step.
- **Domain model:** an app's declaration of its concepts for its assistants:
  for each concept its meaning, the words users may say for it, how it relates
  to other concepts, the states it can be in and what each state means, what
  it must not be confused with, and the operations the assistant can perform
  on it.
- **Operation:** one verb on a concept (read, list, create, change, commit,
  and so on), bound to the tool that performs it and an optional note on how.
- **Exposed tool:** a tool that the composed assistant can actually call in
  the current conversation, after capability, permission and role checks.
- **Domain section:** the instruction text generated from a domain model for
  one composed assistant.
- **Coverage matrix:** for each concept, each verb and each assistant, either
  the tool that performs it or a deliberate statement that it is not
  available there.

## Decision

1. **Apps declare a domain model.** An app that offers an assistant over its
   records declares a domain model in its own agent composition boundary. The
   app owns the content: the concepts, the words, the meanings and the rules.
   A module that owns generic records does not declare an app's meaning for
   them; the app translates.

2. **Intelligence owns the shape and the rendering.** The Intelligence module
   owns the domain-model type, its validation and the generation of the domain
   section. The type is a plain description with no behavior beyond
   validation, usable both by assistants composed from profiles and by
   assistants built directly. Validation rejects a model where a concept name
   repeats, or where one word is claimed by two concepts, because one word
   must lead the model to one concept.

3. **The domain section reflects what is exposed.** The domain section is
   generated per composed assistant from the exposed tools. An operation whose
   tool is exposed is stated with its tool. An operation whose tool is not
   exposed is stated as not available in this conversation, so the model tells
   the user where to go instead of guessing. Concepts are always described, so
   the model understands a user's words even where it cannot act on them. The
   domain section is ordinary instruction text and is part of the
   conversation compatibility key.

4. **One vocabulary for everything the model reads.** Tool names, tool
   descriptions, tool result field names, capability instructions and the
   domain section use the app's words. Module names and module-internal terms
   never appear in what the model reads, except where the app's word is the
   same.

5. **Results carry relationships.** A tool result about a concept includes the
   related facts a user asks about together, such as a procedure's current and
   next step, or a step's notes and reminders, so that a common question is
   answered by one read rather than a chain the model has to know to make.
   Results stay bounded and state when they are partial.

6. **Tools follow a coverage matrix.** Each app keeps a coverage matrix for its
   assistants. A missing cell is a deliberate decision recorded in the domain
   model as an unavailable operation, not an accident. A typed app tool over a
   module capability keeps the module's authorization, preparation and
   confirmation path; the app tool adds the app's scoping and words and never
   grants authority of its own.

7. **Behaviour is checked against the real model.** Each app's assistant has a
   behaviour suite that runs the composed assistant against the production
   model with varied user phrasings, in each language the app supports, and
   asserts which tools are called with which arguments. A change to a domain
   model, a tool name, a tool description, a tool schema or a tool result shape
   is checked by that suite before it is released.

## Consequences

- The model receives one coherent description of an app's world, generated
  from one owner, instead of fragments per tool group.
- An app can add a word users say, or a new operation, by editing its domain
  model; tools and the domain section stay consistent because the section is
  generated from exposed tools.
- Changing a domain model changes the instruction text and therefore the
  compatibility key; existing conversations continue on carried-over history
  as ADR-0012 defines.
- Instruction length grows with the number of concepts. Apps describe only the
  concepts their assistants work with and keep meanings short.
- Typed app tools over a generic module capability mean more tools per
  assistant. This is accepted: a typed tool with the app's words is understood
  far more reliably than a generic operation tool with an untyped payload.
- A behaviour suite that calls a real model costs money and time per run, so
  scenarios stay few and specific, and the suite runs on changes to what the
  model reads rather than on every change.

## Alternatives Considered

### Option 1: App-declared domain model rendered by Intelligence (chosen)

Selected because the knowledge that is missing is relational and app-specific,
while its shape and its relationship to exposed tools are the same for every
app. Two differently composed assistants use it from the start: the clinic's
patient-page assistant and its practice assistant, which expose different
tools over the same concepts, so one model must render differently per
assistant. Every other assistant over generic records needs the same shape.

### Option 2: Longer per-tool and per-capability instructions

Rejected. No tool owns the relationship between concepts, so the knowledge
stays fragmented, duplicated and inconsistent between tools, and nothing ties
it to what is exposed.

### Option 3: An automatic renaming layer over module vocabulary

Rejected. An app's meaning is not a renaming: states mean different things to
different users, some module records must not be treated as the app's records
(templates are not a patient's evidence), and access rules differ. A mapping
table alone cannot carry that.

### Option 4: Do nothing

Assistants keep failing to understand ordinary requests that use the users'
own words, and each app invents its own partial fix.

## Validation / Enforcement

- Intelligence unit tests cover domain-model validation (repeated concept,
  word claimed twice) and the domain section (exposed operations named with
  their tool, unexposed ones marked unavailable, deterministic text).
- Each app's agent tests assert that every tool named in its domain model is a
  registered tool of that app, and that its composed assistants include the
  domain section.
- Each app's behaviour suite covers its concepts with varied phrasings in each
  supported language.
- Code review rejects module names in model-facing text and new tools that are
  not reflected in the domain model.

## Notes

The module documentation for Intelligence describes how to declare a domain
model and how to add behaviour scenarios.
