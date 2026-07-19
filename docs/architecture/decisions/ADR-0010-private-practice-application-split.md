# ADR-0010: Distinct owner and patient web applications

## Status

Accepted for implementation.

## Decision

Private-practice owner workflows and patient self-service are independently deployed Next.js applications: `private-practice-owner` and `patients`. Each app owns its first-party HttpOnly session and calls the API through a server-side BFF boundary. Central identity remains the OIDC login authority; browser JavaScript does not receive API access tokens and the applications do not rely on cross-site API cookies.

The owner application uses neutral clinic routes (`/patients`, `/appointments`, `/settings`, and `/check-in`). The patient application owns public practice discovery and patient self-service routes. SEA remains a backend capability and is not exposed by either web application.

## Consequences

The applications can be deployed on separate domains with explicit OIDC redirect/logout and WebAuthn origins. Authentication, tenant derivation, authorization, release visibility, and document access remain server-side concerns. The legacy combined host has no compatibility redirect requirement; removal follows the staged migration and deployment work.

Patient-visible clinical data is default-deny and must be explicitly released by an authorized owner workflow. Portal links and release metadata are additive persisted state; rollback disables portal access without deleting clinical data.
