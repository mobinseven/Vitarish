# Project skill routing

Canonical project skills live in this directory. Select the smallest route that owns the decisions in the request; `AGENTS.md` remains authoritative.

## Route by decision boundary

| Decision | Skill |
| --- | --- |
| Non-trivial scope, assumptions, slices, and acceptance criteria | `platform-change-planning` |
| Approved implementation and evidence-based handoff | `platform-delivery-loop` |
| Artifact placement, ownership, dependency direction, and ADR need | `platform-artifact-architecture` |
| Domain/Application use cases and ports | `platform-backend-use-case` |
| Persisted data, migrations, and compatibility windows | `platform-data-evolution` |
| Remote providers and vendor adapters | `platform-external-integration` |
| HTTP/OpenAPI or realtime transport contracts | `platform-transport-contract` |
| Authentication, authorization, privacy, and untrusted input | `platform-trust-boundary` |
| React features, browser APIs, localization, and accessibility | `platform-react-feature` |
| Web route shells, outer geometry, landmarks, and scroll ownership | `apply-web-layout-policy` |
| Deterministic tests, builds, generators, and runtime scenarios | `platform-test-validation` |
| Requested deletion, reuse, or abstraction reduction | `platform-simplification-audit` |
| Skill collection and routing maintenance | `platform-skill-governance` |

## Route by task mode

- Planning: use `platform-change-planning` plus only the decision-boundary skills touched.
- Implementation: use `platform-delivery-loop`, the touched decision-boundary skills, and `platform-test-validation` for non-trivial changes.
- Diagnosis: use the owning boundary skill and `platform-test-validation`; do not load delivery unless a fix is authorized.
- Review: use the owning boundary skill and `platform-test-validation`; add `platform-delivery-loop` only for final delivery review or authorized edits.

## Isolated layout routing cases

| Case | Example request | Expected route |
| --- | --- | --- |
| Positive | “Choose the correct shell and primary scroll owner for a new responsive route.” | `apply-web-layout-policy` |
| Negative | “Tighten spacing inside this existing feature card grid; the outer shell is unchanged.” | `platform-react-feature`, not `apply-web-layout-policy` |
| Planning | “Plan migration of a page from raw viewport utilities to DashboardFrame workspace mode.” | `platform-change-planning` + `apply-web-layout-policy` + `platform-test-validation` |
| Implementation | “Implement the approved centered auth-page layout migration.” | `platform-delivery-loop` + `apply-web-layout-policy` + `platform-react-feature` + `platform-test-validation` |
| Diagnosis | “Explain why this dashboard has two vertical page scrollers; do not edit.” | `apply-web-layout-policy` + `platform-test-validation` |
| Review | “Review this PR for nested mains, app-local viewport sizing, and incorrect aside behavior.” | `apply-web-layout-policy` + `platform-test-validation` |
