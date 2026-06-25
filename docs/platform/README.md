# Platform Modules — Journey Maps

This folder contains all shared platform module journey maps. These modules are not owned by a single app but provide capabilities shared across the platform.

## Structure

Platform journey maps are organized by module:

### Medical Module
Located in `modules/medical/journeys/`

Covers owner journeys for code system registry, import operations, local curation, reference queries, service valuations, and tenant preferences.

- See [Medical Module Journeys](./modules/medical/journeys/README.md) for details

### Prescription Module
Located in `modules/prescription/journeys/`

Covers shared e-prescription platform workflows including doctor journeys, recovery/support operations, reference administration, and credential setup. These journeys may be consumed by the Clinic Owner app or other apps.

- See [Prescription Module Journeys](./modules/prescription/journeys/README.md) for details

### Knowledge Module
Located in `modules/knowledge/journeys/`

Covers shared knowledge management platform workflows including corpus management, diagnostics, evaluation, indexing, QA testing, and source import operations.

- See [Knowledge Module Journeys](./modules/knowledge/journeys/README.md) for details

### Platform Module
Located in `modules/platform/journeys/`

Covers platform owner/admin journeys for tenant management, user invitations, billing, action inbox, audit logs, dashboard, and onboarding.

- See [Platform Module Journeys](./modules/platform/journeys/README.md) for details

## How to Use

1. Identify which platform module(s) your work affects
2. Review the module's README for implementation matrix and acceptance criteria
3. Reference specific journey files for detailed user flows
4. Link implementation work back to journey stages

## Related Documentation

- For app-specific journeys, see [App Documentation](../apps/).
- For ADR-0003 pilot-proven module migration conventions, see [ADR-0003 implementation conventions](./modules/platform/conventions/adr-0003-implementation-conventions.md).
