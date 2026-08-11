# Vitarish Web product facts

## Product

Vitarish is a bilingual tenant workspace for practice owners and platform administrators to manage projects, procedures, events, attachments, reminders, roles, and statistics through the existing Vitarish product system.

## Central PM assistant

`/assistant` is an immersive, tenant-wide project-management workspace for saved conversations. It is available to tenant owners, platform administrators, and users with at least one writable SEA event. Existing event-scoped assistant Sheets remain event-bound and independent.

The assistant can inspect authorized tenant and SEA resources and can propose mutations. Every referenced resource is authorized, and every prepared mutation is re-authorized at confirmation time. Mutations are independently confirmed or rejected, expire after 30 minutes, are idempotent when confirmed, and are never automatically retried after an unknown outcome.

## Experience

The surface is an operational workspace: a resizable desktop thread list beside a focused chat, and a HeroUI Pro Sheet for the thread list on mobile. It inherits the incumbent Vitarish PM visual language, uses Font Awesome icons, supports RTL and localized dates, and keeps pane overflow inside the workspace rather than the page shell.

## Data and trust

Conversation persistence retains provider history separately from encrypted display-safe transcripts and thread metadata. Existing Intelligence retention rules remain authoritative. Browser `File` objects and bytes remain local until an explicitly confirmed, event-targeted attachment operation; they never enter prompts, history, prepared actions, or server action state.

SEA reminders are event-bound, use verified tenant contacts, require offset-bearing ISO 8601 timestamps normalized to UTC, keep sent records immutable, and are delivered only by background processing. No external recipients, binary transformation, automatic execution, or replacement of event assistants is in scope.
