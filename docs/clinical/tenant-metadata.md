# Clinical tenant metadata

The Clinical module resolves tenant-specific connector context from tenant metadata so that downstream consumers do not bypass tenant configuration.

## Supported keys

- `clinical.enabled` — enables the Clinical connector for the tenant.
- `clinical.fhir.baseUrl` — overrides the default FHIR endpoint for the tenant.
- `clinical.environment` — names the remote environment used by the tenant.
- `clinical.organization` — identifies the owning organization or care network.
- `clinical.clinic` — identifies the clinic or site inside the tenant.
- `clinical.requireAccessReason` — requires an access reason for Clinical operations.
- `clinical.allowedResourceTypes` — comma-separated or array-based list of permitted FHIR resource types.

## Resolution rules

1. Tenant metadata overrides application defaults.
2. Missing values fall back to the `Vitarish:Clinical` configuration block.
3. Resource type lists accept either arrays or comma-separated strings.
4. Boolean values accept native booleans or their string equivalents.

## Global Clinical connector allowlists

The application-level `Vitarish:Clinical` configuration controls the global resource and operation allowlists that gate remote execution before any tenant-specific logic runs.

- `AllowedResourceTypes` defines which FHIR resource types the platform can target at all.
- `AllowedOperations` defines which generic interactions are enabled globally.
