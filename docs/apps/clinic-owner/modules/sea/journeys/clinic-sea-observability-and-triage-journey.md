# Clinic Sea Clinical Observability and Triage Journey

## 1. Context

- App: Clinic Owner
- Module: Sea usage in patient workflows
- Capability: Clinical attention signals and workflow triage
- Related modules: Sea, Communications, Appointments, Medical
- Journey type: Future state translation of current Sea observability with clinical gaps
- Scenario: Clinic operators use patient/procedure stats, unread indicators, reminders, and progress signals to decide what needs attention.
- User goal: Quickly identify which patient/procedure/step needs clinical or administrative action.
- Business goal: Reduce missed follow-ups and hidden procedure work while preserving module ownership.
- Scope start: User opens patient list, patient overview, procedure list, or event list.
- Scope end: User understands attention priority and drills to the owning item or acknowledges no action is needed.
- Primary persona: Clinical coordinator / clinic owner
- Evidence sources: Platform Sea Observability and Triage journey; Clinic Owner patient overview journey.
- Confidence level: Medium

## 2. Clinical Journey Stages

### Stage 1: Triage patients by attention signals

- **Clinical actions:** Sort/filter patients by new evidence, pending reminders, incomplete procedures, restricted updates, or stale activity.
- **Sea source:** Project stats and new/unread item indicators.
- **User questions:** Which patient needs action now? Is this urgent clinical work or routine admin?
- **Pain points:** Generic counts do not explain clinical priority.
- **Needs:** Patient-safe attention labels and drill-through to exact procedure/event/item.
- **Opportunities:** Translate counts into badges such as new note, file uploaded, reminder due, step pending.
- **Success metrics:** Time to action; overdue reminders; ignored unread indicators.

### Stage 2: Triage procedures under patient

- **Clinical actions:** Identify active/incomplete procedure, recent changes, blocked/restricted steps, and due reminders.
- **Sea source:** Procedure stats and event progress.
- **User questions:** Which treatment is behind? What changed since last visit?
- **Pain points:** Progress inferred from events may not match clinical status.
- **Needs:** Clear distinction between event progress and explicit procedure lifecycle status.
- **Opportunities:** Procedure summary combining Sea stats with clinic status extension if implemented.
- **Success metrics:** Missed active procedure rate; time to open relevant event.

### Stage 3: Drill into event items

- **Clinical actions:** Open the event or item behind a badge/count and review evidence/follow-up.
- **Sea source:** Event item timeline and stats drill-down.
- **User questions:** Which item is new? Who added it? What should I do next?
- **Pain points:** Counts without exact item navigation create review fatigue.
- **Needs:** Direct links to newest item, author/time, item type, and source module.
- **Opportunities:** Highlight first unread/new item inside the clinical timeline.
- **Success metrics:** New item open rate; repeated drill backs; time to locate new evidence.

### Stage 4: Handle unavailable or partial signals

- **Clinical actions:** See partial failures when Files, Communications, or plugin timelines cannot load.
- **Sea source:** Plugin and related-module item rendering.
- **User questions:** Is there no data or did loading fail? Can I safely proceed?
- **Pain points:** Blank states can hide clinically relevant missing data.
- **Needs:** Honest unavailable states and source-specific retry.
- **Opportunities:** Distinguish no reminders from reminders unavailable, no files from files unavailable.
- **Success metrics:** Partial-load error rate; retry success; incidents from hidden unavailable data.

## 3. Documentation Architecture Audit

Artifact: `clinic-sea-observability-and-triage-journey.md`  
Type: Documentation journey  
Responsibility: Translate Sea stats and item indicators into clinic patient/procedure triage usage.

- SRP: Pass — covers only attention/triage experience.
- Single state model: Pass — Sea stats are signals; procedure status remains separate if introduced.
- Low coupling: Pass — related module failures are surfaced but not owned by Sea.
- Abstraction: Pass — avoids implementation-specific observability tooling.
- Command-query: Pass — triage is read-only until user enters owning action flow.
- Failure safety: Pass — unavailable/partial signals are explicitly handled.
- Open/Closed: Pass — clinic labels extend stats interpretation.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — no speculative alerting platform.
- Single source of truth: Pass — signals point to their owning source item.
