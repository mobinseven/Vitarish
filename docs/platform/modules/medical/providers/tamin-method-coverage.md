# Tamin SDK method coverage matrix

Date: 2026-11-09
Owner: Medical module / Infrastructure provider integration
Status: Infrastructure covers the prescription lifecycle and the coding lists through `Tamin.Integration` 4.1.0 (`ITaminClient`). Application/API/UI exposure of the remaining operations stays deferred until a user-facing use case needs it.

This matrix is the source of truth for which `ITaminClient` operations have deliberate Vitarish coverage. SDK types and `JsonElement` payloads stay inside Medical Infrastructure.

Supersedes the earlier `Vorn.Tamin` 2.0.0 matrix. [tamin-implementation-plan.md](tamin-implementation-plan.md) is the historical plan for that SDK and is not current.

## Ownership split

| Concern | Owner |
|---|---|
| Hosts (pilot/production), PKCE, token exchange and refresh, retry of GETs, wire models | `Tamin.Integration` 4.x. Vitarish configures none of them. |
| Doctor token persistence and cross-request refresh lease | `TaminCredentialTokenStore` (implements the SDK's `ITaminTokenStore` over the encrypted credential row). |
| Secure outbound transport | `TaminClientFactory`, through the shared `IHttpMessageHandlerFactory` policy. |
| Item-to-request mapping, response-to-canonical mapping | `TaminPrescriptionMapper`. |
| Coding-list reading | `TaminMedicalInfrastructureGateway` and `TaminProviderJsonReader`. |

## Operations

| `ITaminClient` operation | Vitarish use | Status |
|---|---|---|
| `CreateAuthorizationUri`, `CompleteAuthorizationAsync`, `TaminClient.CreateCodeVerifier/CreateCodeChallenge` | `TaminCredentialSessionStore.BeginAuthorizationAsync` / `CompleteAuthorizationAsync` | Used. |
| `CreatePrescriptionAsync` | `TaminInsuranceAdapter.SubmitPrescriptionAsync` | Used. Sends `srvType` per item; SNOMED drug values and `terminology` come from item metadata. |
| `UpdatePrescriptionAsync` | `EditPrescriptionAsync` | Used. |
| `DeletePrescriptionAsync` | `CancelPrescriptionAsync` | Used. |
| `GetPrescriptionDetailsAsync` | `FetchPrescriptionAsync` | Used. |
| `GetPatientEntitlementAsync` | `CheckEntitlementAsync`, run by `CheckDraftEligibility` when the sheet submits | Used. The response shape is not documented by the guide; the mapper reads `status` / `statusCode` / `status_code`. |
| `ListServicesAsync` (`01` drugs, `02` laboratory, others) | Reference sync, code systems `drug`, `service`, `all-services` | Used. |
| `ListPrescriptionTypesAsync` | Code system `prescription-type` | Used. |
| `ListLaboratoryTariffGroupsAsync` | Code system `paraclinic-tariff` | Used. |
| `ListDrugAmountsAsync`, `ListDrugInstructionsAsync` | Code systems `drug-amount`, `drug-instruction` | Used. |
| `ListDrugUsagesAsync`, `ListDosageFormsAsync` | Code systems `drug-usage`, `dosage-form` | Used. |
| `GetMedicationInstructionsByDosageFormAsync` | `IInsuranceProviderDrugConsumptionAdapter` (Tamin), served by `GetDrugConsumptionOptions` under the caller's own login | Used. Feeds the per-drug consumption picker in the prescription sheet. |
| `CheckDentistRulesAsync`, dentist service lists | None | Deferred. |
| Referral, hospitalization, family-doctor, patient-disease operations | None | Deferred. |
| Nursing operations | None | Not supported by the SDK yet (see the SDK changelog). |

## Behaviour to remember

- **Every Tamin call carries a doctor's token, including the coding lists.** Reference sync therefore needs `CredentialId` (the doctor/clinic login to use). Without it the adapter returns a validation error. There is no anonymous or shared sync credential.
- **A credential with no stored token fails fast** with an authentication error. An expired but refreshable token is refreshed lazily by the SDK on the first call, under the credential's lease; a rejected refresh clears the stored token and surfaces as `TaminReauthorizationRequiredException`, mapped to an authentication error. A network or 5xx failure during refresh keeps the refresh token.
- **`AuthenticateAsync` / credential test no longer performs a network refresh.** It confirms a token is stored; the first real call proves it.
- **The sheet checks eligibility itself.** Submit first runs `CheckDraftEligibility`, which asks the provider under the caller's own login (doctor and clinic from the authenticated scope) and records the answer on the draft; validation then gates on it.
- **Drug items need their consumption values.** `drugAmntId` and `drugInstId` are mandatory in the guide, so `SubmitPrescriptionAsync` and `EditPrescriptionAsync` reject a drug item without them by name. Templates that add drugs without them must be completed before submitting.
- **The doctor's mobile is stored on the credential** (`ProviderDoctorMobile`, set in insurance settings) and sent as `docMobileNo`. A credential without it cannot submit; existing credentials must be re-saved.
- **Each drug's dosage form** (`formCode`) is stored in the synced item metadata. Drugs synced before this change need a re-sync before the picker works for them.
- **Patient mobile is optional.** `FallbackMobileNumber` is sent only when configured.
- **Item metadata keys read by the mapper:** `illnessId`, `planId`, `toothId`, `serviceType` (overrides the category default `srvType`), and for drugs `drugAmntId`, `drugInstId`, `doseCode`, `methodUsageId`. A drug with `doseCode` marks the prescription `terminology: SNOMED` and sends the route as `doseCode` instead of free-text `dose`.
- **Removed configuration keys:** `BaseUrl`, `AccountBaseUrl`, `SoaBaseUrl`, `ApiBaseUrl`, `SandboxBaseUrl`, `TokenEndpoint`, `AllowedHosts`, `ReferenceDataRequiresToken`, `TokenSkew`. The SDK takes hosts from its OpenAPI contract, binds tokens to those hosts, and refreshes two minutes before expiry. Pilot versus production is still chosen by `Endpoint` / `UseSandbox`.

## Known gaps

- The SDK has not been validated against the live Tamin service (see its changelog); response shapes for most operations are untyped.
