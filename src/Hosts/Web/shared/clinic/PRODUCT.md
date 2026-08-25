# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

The primary user is the person in charge of a private-practice clinic. They oversee patient routines, coordinate doctors and experts, manage appointments and shifts in real time, and track patient progress through procedures and prescriptions. They need to keep patient care current without important details being lost in daily volume.

## Product Purpose

`@vitarish/clinic` is the shared clinic-management UI layer for the Vitarish platform. It provides the complete feature set for private-practice operations: patient records, check-in workflows, appointment and shift management, procedure tracking with step-level progress, prescription template authoring, reminders, statistics, and clinic settings. The package is consumed by host apps (such as `private-practice-owner`) that supply the shell, routing, and authentication. Success means the operator can manage patient progress and office operations from a single, fast, comprehensible interface.

## Positioning

Vitarish is modern and AI-native. Its distinct mechanism is guiding the operator toward a meaningful grasp of each patient's progress in their routines, rather than acting only as a data-acquisition or record-storage system. The UI must feel lightweight and direct — never clinical in the sterile sense, but always precise.

## Operating Context

The product is used during daily private-practice operation: monitoring patient routines, documenting progress, managing doctors and experts, coordinating appointments and agendas in real time, defining shifts and services, managing providers, creating prescriptions from templates, and tracking clinic statistics. It runs in a browser, supports English and Persian with right-to-left layout, and must perform well on typical clinic hardware.

## Capabilities and Constraints

- Patient records with search, sort, infinite-scroll list, and detailed profile views.
- Patient check-in wizard: find patient, select shift, confirm.
- Appointment management: owner and user views for shifts and reservations, with agenda views (day/week/month).
- Procedure tracking: step-level progress, event timelines, evidence/attachment management, access control, and template-based procedure creation.
- Prescription template workspace: author reusable bundles of drugs, lab tests, imaging, and referrals with tag-based organization.
- Reminder management for patient notifications.
- Clinic settings: user management, appointment settings, insurance provider integration (Tamin), service/type/provider configuration.
- Statistics dashboard.
- Navigation is intent-based: a `NavigationIntent` discriminated union drives host-app routing.
- The binding product principle is KISS: avoid multilevel menus, multilevel directions, and overcomplicated forms. Workflows should be stupidly simple.
- Bilingual (English/Persian) with full RTL support.
- Data fetching via `@tanstack/react-query` with infinite pagination for lists.
- API layer generated via Orval from OpenAPI specs.

## Brand Commitments

The product is called Vitarish. It should communicate a modern, AI-native product without sacrificing clarity or simplicity. The UI uses HeroUI v3 and HeroUI Pro components, Font Awesome icons via `FaIcon`, and the shared `@vitarish/ui` primitives.

## Product Principles

- Make the next meaningful action obvious.
- Preserve the clinical story, not just the incoming data.
- Keep routine office work fast and comprehensible in real time.
- Prefer simple, shallow workflows over feature-dense navigation.
- Automate and guide where it reduces cognitive load, while keeping operators in control.

## Accessibility & Inclusion

The product must remain usable in both English and Persian, including right-to-left presentation where applicable. Simplicity is an inclusion requirement: avoid unnecessary steps, dense navigation hierarchies, and complex forms. All interactive elements must have accessible labels. Buttons must have localized tooltips and icons so they can shrink to icon-only on mobile.
