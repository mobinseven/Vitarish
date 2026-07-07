# Clinic Owner App — Journey Maps

This folder contains all journey maps for the Clinic Owner app.

## Structure

The clinic owner app's user journeys are organized by functional module:

### Patient Module
Located in `modules/patient/journeys/`

Covers patient profile management (CRUD), patient procedures, and patient overview workflows.

- See [Patient Module Journeys](./modules/patient/journeys/README.md) for details

### Appointment Module
Located in `modules/appointment/journeys/`

Covers appointment booking, management, check-in, and related scheduling workflows.

- See [Appointment Module Journeys](./modules/appointment/journeys/README.md) for details

### Prescription Module
Located in `modules/prescription/journeys/`

Covers e-prescription creation, submission, editing, cancellation, and support operations.

- See [Prescription Module Journeys](./modules/prescription/journeys/README.md) for details

### Sea Module Clinical Usage
Located in `modules/sea/journeys/`

Translates the platform Sea project/procedure/event journey maps into Clinic Owner patient-centered clinical usage, where patients are Sea projects and clinical procedures/events live under each patient profile.

- See [Clinic Owner Sea Module Journeys](./modules/sea/journeys/README.md) for details

## How to Use

1. Start with the [Patient Module](./modules/patient/journeys/README.md) for implementation matrix and acceptance criteria
2. Use the [Sea Module Clinical Usage](./modules/sea/journeys/README.md) when patient procedures, clinical events, notes, attachments, reminders, roles, or Sea-backed plugins are involved
3. Reference specific journey files for detailed user flows and design decisions
4. Use these journeys as the source of truth when planning implementation work

## Related Documentation

For shared platform module journeys (medical, knowledge, platform services), see [Platform Modules](../../platform/modules/).
