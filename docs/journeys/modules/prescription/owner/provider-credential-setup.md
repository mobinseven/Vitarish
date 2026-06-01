# Journey: Provider Credential Setup

## 1. Context
- **Persona:** Clinic Admin
- **Trigger:** New doctor needs credentials for insurance provider, or existing credentials need renewal/update.
- **Goal:** Map doctor credentials to insurance providers so they can submit prescriptions.

## Current Implementation Alignment
- **Status:** `missing` as a complete owner journey; `partial` backend/read-model support exists.
- **Implemented now:** credential health can be queried and displayed in operational monitoring; adapters expose `AuthenticateAsync`; `DoctorProviderCredential` models credential state.
- **Current artifacts:** `PrescriptionOperationalDashboard`, `GetCredentialHealthQuery`, `CredentialHealthResult`, `DoctorProviderCredential`, `IInsuranceProviderAdapter.AuthenticateAsync`.
- **Authoritative state:** provider credential state belongs to `DoctorProviderCredential`.
- **Known gaps:** no routed credential setup page, no credential configuration form, no save/update command, no explicit credential test command, and no owner-facing renewal workflow.

## 2. Entry Conditions
1. Clinic admin is authenticated with credential management permission.
2. Insurance provider is registered and active.
3. Doctor is registered in the clinic.

## 3. Journey Stages

### Stage 1: Credential Management Dashboard
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Admin navigates to provider credential management | Dashboard shows: all doctors × all active providers, with credential status per combination. |
| 1.2 | — | Status indicators: valid (green), expiring soon (yellow), expired (red), not configured (grey). |
| 1.3 | Admin identifies doctor needing credential setup | Selects doctor + provider combination. |

### Stage 2: Credential Configuration
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | Admin clicks "Configure" or "Update" for doctor-provider pair | Credential form opens with provider-specific fields. |
| 2.2 | Enters provider-specific credentials: username, identifier, OTP setup (if required) | System validates format per provider requirements. |
| 2.3 | — | Credentials stored securely (encrypted at rest via secret reference, never plaintext in DB). |
| 2.4 | Optionally tests credential (if provider supports test/verify endpoint) | System calls provider adapter `Authenticate` in sandbox/test mode. Reports success/failure. |
| 2.5 | Saves credential mapping | `DoctorProviderCredential` record created/updated. Status: active. Expiration tracked if applicable. |

### Stage 3: Credential Renewal
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | Admin receives expiration warning (notification or dashboard indicator) | Credential approaching expiry highlighted. |
| 3.2 | Admin updates credential before expiration | Same flow as configuration. Old credential deactivated. |
| 3.3 | — | Audit records credential change: who, when, which provider. (No credential values in audit.) |

### Stage 4: Multi-Branch/Multi-Clinic
| Step | User Action | System Response |
|------|-------------|-----------------|
| 4.1 | Admin configures credentials per clinic/branch (if doctor works at multiple) | Credential is scoped: doctor_id + provider_key + clinic_id. |
| 4.2 | — | Doctor's available providers in prescription flow reflect credentials for current clinic context. |

## 4. Exit Conditions
1. Doctor-provider credential mapping is active.
2. Doctor can now submit prescriptions to configured provider.
3. Credential stored securely with audit trail of configuration changes.
4. Expiration tracking active for time-limited credentials.

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| Credential test fails | "Authentication test failed — verify credentials" | Credential saved as draft/unverified; not active for submission until test passes |
| Credential expires while doctor is mid-session | Next submission attempt shows "Credential expired — contact admin" | Prescription draft preserved; submission blocked |
| Provider requires OTP/2FA during credential setup | OTP input step in configuration flow | Token captured and stored securely |
| Admin accidentally revokes active credential | Confirmation dialog warns of impact | Revocation audit-logged; affected doctor notified |

## 6. Acceptance Criteria
1. Credentials are scoped per doctor + provider + clinic combination.
2. Credentials are encrypted at rest — never stored in plaintext configuration.
3. Credential changes are audited (actor, timestamp, provider — never credential values).
4. Expiration warnings surface before credentials become invalid.
5. Credential test (where supported) gives immediate feedback.
6. Prescription workflow dynamically reflects credential availability.
7. Revoking credentials immediately blocks affected submission paths.
