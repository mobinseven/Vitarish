# Journey: Provider Onboarding

## 1. Context
- **Persona:** Platform Admin
- **Trigger:** A new insurance provider needs to be registered in the system.
- **Goal:** Register provider, configure capabilities, and enable for clinics/doctors.

## Current Implementation Alignment
- **Status:** `missing` as a user-facing journey; provider integration infrastructure is present.
- **Implemented now:** provider adapters/options/registry exist for configured providers, and domain entities represent providers, contracts, and capabilities.
- **Current artifacts:** `InsuranceProvider`, `ProviderContract`, `InsuranceProviderCapability`, `IInsuranceProviderRegistry`, `InsuranceProviderRegistry`, Tamin/Ditas/Salamat adapters and options.
- **Authoritative state:** provider availability/capability should come from provider configuration plus `InsuranceProviderCapability`; adapter implementation remains infrastructure.
- **Known gaps:** no provider onboarding page, no registration/update commands, no capability matrix editor, no endpoint configuration form, and no UI connectivity test workflow.

## 2. Entry Conditions
1. Platform admin has provider configuration permission.
2. Provider API documentation, endpoints, and capability information are available.
3. Sandbox/test credentials obtained from provider.

## 3. Journey Stages

### Stage 1: Provider Registration
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Admin navigates to provider management | List of registered providers shown with status (active/disabled/sandbox). |
| 1.2 | Clicks "Register New Provider" | Registration form opens. |
| 1.3 | Enters provider identity: key, name, type, integration mode | System validates key uniqueness and required fields. |
| 1.4 | Saves provider record | `InsuranceProvider` record created in `Disabled` state. |

### Stage 2: Capability Configuration
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | Opens capability matrix for new provider | Checklist of all capabilities shown (SupportsEntitlementCheck, SupportsDrugPrescription, etc.). |
| 2.2 | Checks/unchecks capabilities based on provider API documentation | Each capability toggle stored as `InsuranceProviderCapability` record. |
| 2.3 | Configures integration mode: DirectApi / PortalApi / DitasGateway / ManualFallback | System stores mode; affects adapter routing. |
| 2.4 | Saves capability matrix | Matrix persisted. Drives UI capability gating for this provider. |

### Stage 3: Endpoint and Environment Configuration
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | Configures sandbox endpoints (base URL, auth endpoint, API paths) | System stores endpoint configuration per environment. |
| 3.2 | Configures resilience settings (timeout, retry count, circuit breaker thresholds) | Per-provider resilience policy stored. |
| 3.3 | Optionally configures production endpoints (or defers to go-live) | Production config stored separately from sandbox. |

### Stage 4: Adapter Verification
| Step | User Action | System Response |
|------|-------------|-----------------|
| 4.1 | Admin triggers connectivity test against sandbox | System attempts adapter health check: authentication + basic query. |
| 4.2 | — | Results displayed: connectivity OK/failed, latency, authentication result. |
| 4.3 | Admin reviews test results | If successful: provider ready for sandbox enablement. |

### Stage 5: Activation
| Step | User Action | System Response |
|------|-------------|-----------------|
| 5.1 | Admin enables provider for sandbox use | Status changes to `Sandbox`. Providers shows in credential configuration for sandbox environments. |
| 5.2 | After successful testing: admin enables for production | Status changes to `Active`. Provider becomes available in production prescription workflows for clinics with contracts. |
| 5.3 | — | Audit records: who enabled, when, which environment. |

## 4. Exit Conditions
1. Provider registered with unique key and active status.
2. Capability matrix configured and driving UI behavior.
3. Endpoint configuration stored per environment.
4. Adapter connectivity verified in sandbox.
5. Provider available for doctor credential mapping.

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| Duplicate provider key | "Provider key already exists" | Registration blocked |
| Connectivity test fails | "Cannot reach provider sandbox — verify endpoints" | Provider remains in Disabled state |
| Admin enables provider without adapter implementation | "No adapter registered for this provider" | Enablement blocked until code supports it |
| Provider needs to be emergency-disabled | "Disable Provider" action with reason | Immediately removes from all active workflows; paper fallback suggested |

## 6. Acceptance Criteria
1. Provider registration is centralized — one place to manage all providers.
2. Capability matrix drives dynamic UI behavior without code changes.
3. Sandbox and production are strictly separated.
4. Connectivity test gives confidence before enabling.
5. Provider can be disabled instantly without system restart (feature flag / kill switch).
6. All configuration changes are audited.
7. Adding a new provider's configuration does not require redeployment (capability/endpoint config is data-driven).
