# First-Run Setup Journey

Status: Current state  
Owner: Private Practice General app  
Related: [ADR-0022 Guided Surfaces](../../../architecture/decisions/0022-guided-surfaces.md), [Guided surfaces](../../../platform/modules/intelligence/guided-surfaces.md), [Prepared actions](../../../platform/modules/intelligence/prepared-actions.md)  
Last reviewed: 2026-11-12

## Persona and outcome

A new clinic's owner lands on `/home` after the onboarding handoff to an empty day view. Instead, the operations assistant welcomes the owner and guides setup by chat: it prepares whole steps for review, and nothing changes until the owner confirms. Every step stays reachable from the normal screens for owners who prefer the menus.

## Who sees setup mode

Setup mode applies when the caller is a tenant owner or platform admin, the clinic has no service, no provider or no active shift, and the clinic has not chosen "Later". Staff, and clinics that have all three, see today's dashboard unchanged. If setup progress cannot be read, `/home` shows today's dashboard. When setup applies, the setup home sits above today's panels.

The rules live in `ClinicSetupGoals` (`src/Apps/Clinic/Application/Setup/ClinicSetupGoals.cs`), which has no I/O. `GetClinicSetupProgressQueryHandler` (`src/Apps/Clinic/Infrastructure/Setup`) gathers the facts one dependency at a time and applies it. A dependency failure answers 503.

## Goals

Four goals, always in this order; each is open or done from live data and is re-read after every confirmation.

| Goal key | Done when |
| --- | --- |
| `ServicesAndProviders` | at least one service and one provider |
| `ShiftsAndBookingRules` | at least one active shift and one booking policy |
| `StaffUsers` | at least one tenant member besides the owner |
| `ClinicProfile` | the business profile has an address and a logo |

`nextGoal` is the first open goal. Each goal opens an existing screen: services, shifts, Settings users, clinic settings (`shared/clinic/src/features/clinic-setup/setupGoals.ts`).

## Journey

| Stage | Experience | Behavior |
| --- | --- | --- |
| Welcome | The assistant greets the owner in the main column, with a goal list beside it (stacked on mobile) and one suggestion chip per open goal. | No model call. The server sends goal keys and states only; the web composes the welcome text and chip labels from them in fa and en. Tapping a chip sends its label as an ordinary user message, so the conversation history holds only real turns. The welcome shows only while the conversation has no messages. |
| Guidance | The assistant reads setup progress (`get_clinic_setup_progress`, read-only, which also returns working days and hours), works on the next open goal, and prepares a whole step in one reply, for example one shift per working day. It ends with a question and a next-step button. | It only prepares; it never commits. |
| Batched review | Everything prepared in one reply appears as one card. | Actions of one run are grouped. "Confirm all" confirms the pending items one by one in order; "Retry failed" re-confirms only retryable failures. Each item shows its own outcome; a group is never all-or-nothing. A single item renders as its plain card. |
| Edit | The owner edits an appointment proposal or a staff invitation in its card and saves. | The edit re-runs the owning tool's prepare step under the same guards, rejects the original as revised, and returns the replacement in the same run and group. An appointment edit cannot change which operation it is. Only a proposed action can be revised; otherwise the answer is a conflict. |
| Goal list | Each confirmation refreshes the goal list; open goals are buttons that open their screen. | Progress is refetched, never kept client-side. |

## Staff invitations

- The assistant prepares an invitation from a phone number or an email (exactly one), a first name and a last name. There is no role input; the invited person always becomes staff.
- The tool validates the contact with the same rules the Users provisioning command applies, before preparing.
- The model sees only a masked contact. The full contact travels sealed with Data Protection (30-minute lifetime, bound to the action ID), because the tool runs in the runtime callback request while the action is collected in the conversation request. Logs and telemetry carry only the masked contact.
- The review card shows the full contact and the fixed staff role, so the owner sees exactly what is confirmed.
- Confirming sends `ProvisionTenantMemberCommand` in the confirming request, so the Users handler re-checks the caller and writes the account audit. Created and already-a-member complete (the latter says so); an active-session conflict and a membership conflict are retryable failures; invalid contact, contact in use, role-scope conflict and permission denied are final.
- The capability and action are available to tenant owners and platform admins, never to staff.

## Later

"Later" calls the dismissal route. Parties records `SetupGuideDismissedAt` on the clinic's business profile (nullable column, set once; repeating it is a no-op). It applies to every owner of that clinic; other clinics are unaffected. Only an owner or platform admin may dismiss (policy `Clinic.Setup.Manage`); staff are refused. After it, `applies` is false and the dashboard returns. The goal list stays reachable: while goals are open and the clinic is dismissed, the assistant side panel on the clinic pages shows it above the conversation. Rollback leaves the column in place harmlessly.

## One operations conversation

The setup home and the side panel use the same operations conversation, per browser session: its ID is kept in `sessionStorage` (`useOperationsAssistantConversation`). Closing the panel no longer deletes the conversation; only the reset action does, which deletes it on the server for both places. Server history is retained for the agent conversation retention period (default 90 days, `AgentConversationRetentionOptions`). Earlier turns are not redrawn after a page reload; pending actions stay reachable by ID, and prepared actions live 30 minutes by default. The side panel is not offered on `/home` itself.

## Routes and operations

Clinic API base `/api/modules/clinic` (`ClinicApiRoutes`, `ClinicApiOperations`).

| Purpose | Route | Operation ID |
| --- | --- | --- |
| Read setup progress | `GET /setup/progress` | `ClinicGetSetupProgress` |
| Set setup aside | `POST /setup/dismissal` (204) | `ClinicDismissSetupGuide` |
| Edit a proposal | `POST /assistant/actions/{actionId}/revise` | `ClinicReviseAssistantAction` |
| Confirm, reject | `POST /assistant/actions/{actionId}/confirm`, `/reject` | existing |

The progress response carries `applies`, `goals[] { goal, done }`, `nextGoal` and `dismissed`. Conversation replies carry the optional `blocks` field ([Guided surfaces](../../../platform/modules/intelligence/guided-surfaces.md)). The progress read needs an authenticated tenant user and answers `applies: false` for staff.

## Testing

- Unit: `tests/Apps/Clinic.Application.Tests/Setup/ClinicSetupGoalsTests.cs`, `tests/Apps/Clinic.Tests/Infrastructure/GetClinicSetupProgressQueryHandlerTests.cs`, `tests/Apps/Clinic.Tests/Api/ClinicSetupContractTests.cs`, `tests/Modules/Parties.Tests/Application/TenantSetupGuideDismissalTests.cs`, and under `tests/Apps/Clinic.Application.Tests/Assistant/` `StaffInvitationAssistantTests.cs`, `ReviseAssistantActionTests.cs` and `AssistantGuidedSurfaceTests.cs`. Web: `shared/clinic/src/__tests__/ClinicSetupHome.test.tsx`, `shared/intelligence/src/__tests__/guidedSurface.test.tsx`, `apps/private-practice-owner/src/__tests__/components/HomeDashboard.test.tsx`.
- Mock screenshot flow: `src/Hosts/Web/tests/mock/clinic-setup-home-flow.spec.ts` walks the welcome, the prepared group, an edit, "Confirm all" and "Later" in fa and en.
- Two-host E2E: `src/Hosts/Web/tests/e2e/clinic-setup-home.spec.ts` covers a brand-new owner on the setup home, one group of two shifts, an edit, "Confirm all" checked through the public appointments API, a staff invitation that shows in Settings users, "Later" surviving a reload, and the goal list in the side panel. The E2E host has no model; scripted turns run through the real tools (see [Prepared actions](../../../platform/modules/intelligence/prepared-actions.md)).
