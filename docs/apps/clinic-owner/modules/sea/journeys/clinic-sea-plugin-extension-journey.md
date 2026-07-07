# Clinic Sea Plugin Extension Journey

## 1. Context

- App: Clinic Owner
- Module: Sea usage in patient workflows
- Capability: Clinic-specific Sea UI/plugin extensions
- Related modules: Sea, Medical, Appointments, Communications, Files, Users
- Journey type: Future state translation of current Sea plugin extension with clinical gaps
- Scenario: Clinic-specific plugins adapt generic Sea project, procedure, event, and event-item surfaces to clinical language and workflows.
- User goal: Work in patient/procedure language while Sea keeps the workflow hierarchy consistent.
- Business goal: Extend Sea for Clinic without duplicating state or leaking unrelated module internals.
- Scope start: User enters a Sea-backed clinic surface that renders plugin content or actions.
- Scope end: Plugin content is rendered, action is launched, source module failure is handled, or fallback UI remains safe.
- Primary persona: Clinic operator; secondary persona: module implementer
- Evidence sources: Platform Sea Plugin Extension journey; Clinic Owner Sea design objective.
- Confidence level: Medium

## 2. Clinical Journey Stages

### Stage 1: Render patient-oriented project extensions

- **Clinical actions:** See patient summary, flags, next appointment, current procedures, or clinic-specific actions on a Sea-backed patient/project surface.
- **Sea source:** Project grid/detail/edit plugin extension points.
- **User questions:** Is this the right patient? What should I do next?
- **Pain points:** Plugins can improve context but risk creating a second patient overview state.
- **Needs:** Read-only summaries sourced from owning modules; safe fallbacks when a plugin fails.
- **Opportunities:** Patient overview cards that link to the owning module/action.
- **Success metrics:** Plugin load failures; next-action click-through; duplicate-state defects.

### Stage 2: Render clinical event item plugins

- **Clinical actions:** Review specialized timeline cards such as prescription reference, lab result summary, appointment-linked follow-up, or medical measurement.
- **Sea source:** Event item creation/timeline plugins.
- **User questions:** What is this item? Which module owns it? Can I act on it here?
- **Pain points:** Plugin items can blur boundaries if they mutate unrelated module state implicitly.
- **Needs:** Source labels, read-only versus action affordance clarity, explicit errors.
- **Opportunities:** Use plugins to display medical context while commands route to the owning module.
- **Success metrics:** Plugin item open rate; action failure rate; source-confusion reports.

### Stage 3: Launch clinic-specific actions safely

- **Clinical actions:** Add reminder, attach file, open medical reference, start follow-up appointment, or navigate to provider task from a Sea event.
- **Sea source:** Plugin callbacks and event item action areas.
- **User questions:** Will this action change the event, the appointment, or another module?
- **Pain points:** Multi-module actions can hide command ownership.
- **Needs:** Clear action labels and command boundaries; no silent cross-module mutation.
- **Opportunities:** Confirmation copy that names the target state owner.
- **Success metrics:** Wrong-module action reports; command rollback/failure clarity.

### Stage 4: Handle plugin failure or absence

- **Clinical actions:** Continue core patient/procedure/event work even when optional plugin content fails.
- **Sea source:** Plugin rendering contracts and fallback behavior.
- **User questions:** Is data missing, unavailable, or not configured for this clinic?
- **Pain points:** Plugin failure could hide important clinical context if presented as empty.
- **Needs:** Honest unavailable states and source-specific retry/contact path.
- **Opportunities:** Standard plugin failure copy for clinic surfaces.
- **Success metrics:** Plugin outage impact; retry success; support tickets.

## 3. Extension Boundary Rules

- Plugins may adapt labels, summaries, navigation, and source-owned actions.
- Plugins must not create a parallel patient, procedure, event, note, file, reminder, appointment, or medical-reference state model.
- Commands must execute through the owning module boundary; plugin UI should not hide cross-module mutation.
- Fallback UI must preserve the core Sea hierarchy: patient/project, procedure, event, event items.

## 4. Documentation Architecture Audit

Artifact: `clinic-sea-plugin-extension-journey.md`  
Type: Documentation journey  
Responsibility: Define how Clinic-specific Sea plugins adapt patient/procedure workflows without duplicating state.

- SRP: Pass — covers only plugin extension usage.
- Single state model: Pass — explicitly forbids parallel state models.
- Low coupling: Pass — plugin actions route to owning module boundaries.
- Abstraction: Pass — focuses on extension behavior, not implementation details.
- Command-query: Pass — read-only summaries and commands are distinguished.
- Failure safety: Pass — plugin failure/absence fallback expectations are documented.
- Open/Closed: Pass — plugins are the extension point.
- Core protection: Pass — reinforces state ownership and boundary protection.
- KISS/YAGNI: Pass — no new framework is proposed.
- Single source of truth: Pass — each plugin item/action names its source owner.
