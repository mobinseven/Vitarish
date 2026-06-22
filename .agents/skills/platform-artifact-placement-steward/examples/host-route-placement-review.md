# Example: Host Route Placement Review

## Artifact Placement

Artifact: `ClinicOwnerPatientProcedurePage.razor`

Classification: Routable host page

Owning scope: Main host

Preferred location: `src/Hosts/Main/Pages/...`

Reason: It owns a routable URL and composes module UI for the host experience.

Allowed dependencies:

- route parameters
- route builders/constants
- reusable module UI components
- host composition services

Disallowed dependencies:

- reusable procedure timeline logic embedded directly in the page
- module domain state mutations in page code
- infrastructure adapters directly from the page

Related tests:

- Host navigation/UI flow tests
- Hot scenario if route depends on auth, tenant resolution, DB, and module UI interaction

Documentation impact:

- Link to the journey or roadmap stage that introduced the route.

ADR impact:

- Not required unless this creates a new host routing/composition pattern.
