# Retained OSS HeroUI Components

Date: 2026-07-05
Status: Active
Module: Design System
Scope: HeroUI Pro migration — components intentionally kept on OSS primitives

## Summary

After the HeroUI Pro migration next-phase work, the following OSS `@heroui/react` components remain in use. Each is retained because the OSS primitive is the correct fit for its current workflow, or Pro does not yet ship a matching component.

## Retained Components

### Base form primitives (no Pro equivalent needed)

| Component | Count | Usage locations | Reason retained |
|-----------|-------|-----------------|-----------------|
| `Card` | 58 | Account pages, clinic detail pages, auth forms, knowledge components | Simple content containers; Pro does not ship a basic card primitive that improves these surfaces |
| `Button` | 53 | Throughout all features | Base interactive primitive; Pro does not replace Button |
| `Spinner` | 29 | Loading states across all features | Base loading indicator; no Pro replacement needed |
| `TextField` | 21 | Account forms, auth forms, clinic forms | Base form input; Pro cell controls are for settings layouts, not standalone forms |
| `Label` | 21 | Paired with TextField in forms | Base accessibility primitive |
| `Input` | 20 | Inside TextField compositions | Base input element |
| `Form` | 6 | Auth and account forms | Base form wrapper |
| `Link` | 10 | Navigation links across features | Next.js Link wrapper; no Pro replacement |
| `FieldError` | 3 | Form validation display | Base validation primitive |
| `Select` | 3 | Clinic settings forms | Base select input |
| `ListBox` | 3 | Knowledge corpus selection, option lists | Base list primitive |
| `Alert` | 7 | Auth status messages, knowledge callouts | Base alert/banner; Pro does not ship a matching callout |
| `Modal` | 1 | Patient delete dialog | Base modal; Pro Sheet is for side-panels, not confirmation dialogs |
| `Dropdown` | 1 | User menu | Base dropdown; Pro does not replace menu dropdowns |
| `Checkbox` | 1 | Consent form | Base checkbox |
| `Kbd` | 1 | Keyboard shortcut display | Base keyboard indicator |
| `InputGroup` | 1 | Auth form input grouping | Base layout primitive |
| `Avatar` | 1 | User menu avatar display | Base avatar |
| `AvatarFallback` | 1 | User menu fallback initials | Base avatar fallback |

### Auth forms — deliberately retained

All auth surfaces (login, OTP, passkey, email/phone change) use OSS base primitives exclusively. These are simple single-field or two-field forms where Pro advanced data-display components add no value:

- `src/components/auth/LoginForm.tsx` — OSS Form, TextField, Button
- `src/components/auth/EmailOtpForm.tsx` — OSS Form, TextField, Button
- `src/components/auth/PhoneOtpForm.tsx` — OSS Form, TextField, Button
- `src/components/auth/OtpVerificationStep.tsx` — OSS Form, Button, custom OtpCodeInput
- `src/components/auth/LoginPageCard.tsx` — OSS Card layout shell
- `src/components/auth/AuthMethodSelector.tsx` — OSS Card for method selection tiles

### Account pages — deliberately retained

Account pages use OSS form primitives because the workflows are simple text inputs and selection controls:

- `src/app/account/name/page.tsx` — OSS TextField for first/last name
- `src/app/account/email/page.tsx` — OSS TextField for email input
- `src/app/account/phone/page.tsx` — OSS TextField for phone input
- `src/app/account/culture/page.tsx` — custom OptionSelector with OSS Button
- `src/app/account/passkeys/page.tsx` — OSS Card for credential list
- `src/app/account/password/page.tsx` — OSS Form, TextField, Button

### Clinic surfaces — migrated to Pro

The following were migrated to Pro during this phase:

- `shared/clinic/src/components/appointments/reservations/OwnerReservationsList.tsx` — Pro Table
- `shared/clinic/src/components/appointments/shifts/OwnerShiftsList.tsx` — Pro Table
- `shared/clinic/src/components/user-appointments/reservations/UserReservationsList.tsx` — Pro Table
- `shared/clinic/src/components/user-appointments/shifts/UserShiftsList.tsx` — Pro Table
- `shared/clinic/src/components/patients/list/PatientsGrid.tsx` — Pro Table
- `src/app/owner/clinic/layout.tsx` — Pro AppLayout, Sidebar, Navbar

### Knowledge QA — already on Pro

Knowledge QA chat surfaces already use Pro chat primitives (ChatConversation, ChatMessage, PromptInput, ChatSource, ChatListView, ChatLoader, EmptyState). No further migration needed.

## Revisit Triggers

Re-evaluate retained components when:

- Pro ships a basic Card primitive that improves content containers
- Account pages become settings-cell layouts (consider Pro InlineSelect/CellSwitch)
- Auth flows become multi-step wizards (consider Pro Stepper/Wizard)
- Clinic detail pages need advanced data display (consider Pro detail views)
- Pro ships a callout/banner component (replace OSS Alert in knowledge)

## Decision Log

| Surface | Current | Considered | Decision | Date |
|---------|---------|------------|----------|------|
| Auth forms | OSS Form/TextField/Button | Pro form controls | Retain OSS — simple forms | 2026-07-05 |
| Account forms | OSS TextField/Button | Pro InlineSelect/CellSwitch | Retain OSS — simple text inputs | 2026-07-05 |
| Clinic lists | OSS Card rows | Pro Table | Migrated to Pro Table | 2026-07-05 |
| Clinic shell | OSS AppShellWrapper | Pro AppLayout/Sidebar/Navbar | Migrated to Pro | 2026-07-05 |
| Knowledge chat | Partial Pro | Pro ChatConversation etc. | Already Pro, stabilized | 2026-07-05 |
| Dashboard tiles | OSS Card grid | Pro Widget/KPI | Deferred — no Pro tile primitive | 2026-07-05 |
| Patient delete modal | OSS Modal | Pro Sheet | Retain OSS — confirmation dialog, not side-panel | 2026-07-05 |
