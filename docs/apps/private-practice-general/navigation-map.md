# Private Practice General navigation map

Status: Active — enforced by tests/mock/navigation-routes.spec.ts
Owner: Private Practice General app
Scope: Ideal information architecture of the clinic workspace (Darman) for a
surgeon's private practice. Account, sign-in, platform home and onboarding are
out of scope; see [platform functional architecture](../../platform/functional-architecture.md).

This map is designed from user needs, not from the current screens. Feature
journeys under [journeys/](journeys/) describe behavior inside a destination;
this document owns which destinations exist, how they are grouped and how a
person moves between them. Once approved, the route registry in the app is
derived from this map and checked against it.

## 1. Roles

| Role | Responsibility | Main place of work |
| --- | --- | --- |
| Owner-doctor | Sees patients, decides treatment, plans and performs surgery, owns clinic configuration. | Today, patient record, surgery plan |
| Receptionist | Books and reschedules visits, checks patients in, handles calls and reminders, keeps demographics correct. | Today, schedule, check-in |
| Clinical assistant | Prepares surgeries, records procedure steps and notes, follows up after surgery. | Today, surgery board, procedure workspace |

Every role sees the same structure; permissions hide or disable actions, never
rearrange the map.

## 2. Jobs by frequency

Frequency decides depth: hourly and daily jobs are one click from anywhere,
weekly jobs are two, rare jobs live in settings.

| Job | Roles | Frequency |
| --- | --- | --- |
| See what is happening today (visits, surgeries, who is waiting) | all | hourly |
| Check a patient in on arrival | receptionist | hourly |
| Find a patient and open their record | all | hourly |
| Book, move or cancel a visit | receptionist | hourly |
| Record the visit: notes, prescription, next step | owner-doctor | hourly |
| Register a new patient | receptionist | daily |
| Plan a surgery for a patient | owner-doctor | daily |
| Prepare and track a surgery to completion | assistant | daily |
| Record procedure steps and follow-up | assistant, owner-doctor | daily |
| Send or review reminders | receptionist | daily |
| Review the coming week's schedule and surgeries | owner-doctor, receptionist | weekly |
| Adjust working shifts and availability | owner-doctor, receptionist | weekly |
| Review clinic statistics | owner-doctor | monthly |
| Manage staff and permissions | owner-doctor | rare |
| Configure services, surgery types, templates, insurance | owner-doctor | rare |
| Review the security audit | owner-doctor | rare |

## 3. Target map

Primary navigation has six destinations plus settings. Every page belongs to
exactly one of them, and that item is highlighted while the page is open.

| Destination | Persian | English |
| --- | --- | --- |
| Today | پیشخوان | Today |
| Patients | بیماران | Patients |
| Check-in | پذیرش | Check-in |
| Schedule | نوبت‌ها | Schedule |
| Surgeries | جراحی‌ها | Surgeries |
| Reminders | یادآورها | Reminders |
| Settings | تنظیمات | Settings |

```text
Today                         /home
Check-in                      /check-in
Patients                      /patients
  Patient record              /patients/{id}
    Overview (default tab)    /patients/{id}
    Visits                    /patients/{id}/visits
    Surgeries                 /patients/{id}/surgeries
    Procedures                /patients/{id}/procedures → workspace /patients/{id}/procedures/{pid}
    Prescriptions             /patients/{id}/prescriptions
    Reminders                 (tab, once patient-level reminders exist; surgery reminders show under Surgeries)
  New patient                 /patients/new
  Edit patient                /patients/{id}/edit
Schedule                      /appointments
  Day / week / month views    (view switch, not separate pages)
  Visit detail                /appointments/reservations/{id}
Surgeries                     /surgeries
  Calendar / board views      (view switch)
  Surgery detail              /surgeries/{id}
  Plan surgery                /surgeries/new
Reminders                     /reminders
Settings                      /settings
  Staff and permissions       /settings/users
  Services and providers      /settings/services (types, services, providers as tabs)
  Booking rules               /settings/appointments
  Shifts                      /settings/shifts → /settings/shifts/{id}
  Surgery types               /settings/surgery-types
  Procedure templates         /settings/procedure-templates
  Prescription templates      /settings/prescription-templates
  Insurance                   /settings/insurance
  Statistics                  /settings/statistics
  Security audit              /settings/audit
```

Global, available on every page:

- **Find patient** (command menu, ⌘K) — search by name, phone or identification code; opens the record.
- **New** — book visit, register patient, plan surgery.

## 4. Page types

Each page has one type; the type fixes header, back behavior and layout.

| Type | Header | Back goes to | Examples |
| --- | --- | --- | --- |
| Overview | Large header with date navigation | — | Today |
| List | Title, count, search, filters, one primary "new" action | — | Patients, Schedule, Surgeries, Reminders |
| Record | Identity header (name, code, key facts), action row, tabs | Its list | Patient record, Surgery detail, Visit detail |
| Workspace | Compact header with parent breadcrumb | Its record | Procedure workspace |
| Form | Title, single column, save and cancel at the end | Where it was opened from | New/edit patient, Plan surgery |
| Settings | Title and description; settings index uses cards | Settings index | All settings pages |

## 5. Key task flows

Each flow lists clicks from any page; the target is at most three for hourly jobs.

1. **Patient arrives** — Check-in → enter code or name → confirm visit → patient appears as waiting on Today. 2–3 clicks.
2. **Doctor sees next patient** — Today → waiting patient → record opens on Overview with the visit in progress → notes / prescription → finish visit → back to Today. 2 clicks to start.
3. **Phone booking** — New → book visit → find or create patient → pick slot from shifts → confirm. 3–4 clicks.
4. **Plan surgery** — Patient record → Surgeries tab → Plan surgery (patient prefilled) → type, date, location, team → save → surgery detail. 3 clicks.
5. **Prepare surgery** — Surgeries → board view filtered to this week → surgery → checklist and team → mark ready. 2 clicks.
6. **Record a procedure step** — Patient record → Procedures tab → procedure → step → note or file → complete. 3 clicks.
7. **Follow-up reminders** — Reminders → due today → send or reschedule. 1–2 clicks.
8. **Weekly review** — Schedule week view; Surgeries calendar. 1 click each.
9. **New staff member** — Settings → Staff → invite with role. 3 clicks.

## 6. Navigation rules

- One route registry declares every page with its parent, primary destination
  and page type; the sidebar, active highlight, breadcrumbs, back links and the
  command menu are generated from it.
- Every page is reachable through navigation; no destination exists only by URL.
- One URL per destination; alternate URLs redirect to the canonical one.
- Detail pages always offer back to their list, preserving filters and view.
- A failed or missing record shows the app layout with a localized message and
  a way back; never the framework error or 404 page.
- Each page has exactly one `h1`, and every icon-only control has a label.

## 7. Owner decisions

1. Destination names follow the table in section 3.
2. Check-in is its own destination.
3. Visits, surgeries, procedures and prescriptions are tabs of the patient
   record; Surgeries also keeps its clinic-wide destination for planning.
4. Shifts and Statistics are settings pages.
5. Content management (`/contents`) and knowledge (`/knowledge`) are removed
   from this app.

## 8. Validation and implementation

1. Walk every flow in section 5 on this map; record clicks and dead ends.
2. Tree test with two or three clinic staff: give them the jobs from section 2 and ask where they would go.
3. After approval, reconcile each existing route as keep, move, merge, rebuild or delete.
4. Implement the route registry and generated navigation, then enforce it with
   checks: every route reachable, exactly one active destination, one `h1`, and
   every route renders under the ready mock scenario without errors.
