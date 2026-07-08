# Exhaustive static implementation plan — wire Vorn.Tamin methods into Vitarish Medical Infrastructure

Date: 2026-07-01  
Repository inspected: `mobinseven/Vitarish`  
SDK inspected: `mobinseven/Vorn.Tamin`  
Target area: `src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin`

---

## 1. Goal

Implement the Medical Infrastructure side so every currently available public operation in `Vorn.Tamin` has a deliberate Vitarish integration point and can later be wired cleanly to application/API/client/UI interfaces.

The work should not expose provider-specific SDK types directly outside Infrastructure. `Vorn.Tamin` returns mostly `JsonElement`, so the Medical module should own canonical request/response DTOs, mapping, validation boundaries, pagination behavior, error mapping, observability, and later interface contracts.

The expected end state is:

1. Existing prescription submit/fetch/edit/cancel flows remain compatible.
2. Tamin drug and service reference methods become first-class infrastructure operations:
   - search one page of drugs;
   - search one page of services;
   - list all drug pages safely;
   - list all service pages safely;
   - sync all reference datasets supported by the SDK, not only `drug` and `service`.
3. All additional available Tamin workflows are either implemented or explicitly represented as planned interface-ready methods:
   - authentication/session/PKCE/token operations;
   - entitlement/eligibility;
   - visit, drug, paraclinic, medical service, referral, physiotherapy, hospitalization prescription registration;
   - registered-prescription fetch;
   - edit and delete;
   - warning/dental rule checks;
   - referrals;
   - nurse cartable and action recording;
   - hospitalization secretary list;
   - reference data helpers.
4. Unsupported placeholder surfaces are deliberately documented as not implemented:
   - identity;
   - pharmacy;
   - paraclinic service delivery.

---

## 2. Static inspection summary

### 2.1 Current Vitarish state

The Medical Infrastructure project already references the SDK:

```xml
<PackageReference Include="Vorn.Tamin" />
```

Current Tamin implementation exists here:

```txt
src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/
  ITaminCredentialSessionStore.cs
  TaminCredentialSessionStore.cs
  TaminEditedPrescriptionItemMapper.cs
  TaminErrorMapper.cs
  TaminInsuranceAdapter.cs
  TaminOptions.cs
  TaminPrescriptionMapper.cs
```

Current DI registration is in:

```txt
src/Modules/Medical/Infrastructure/DependencyInjection/DependencyInjectionExtensions.cs
```

The current provider abstraction is:

```txt
src/Modules/Medical/Application/Abstractions/Prescriptions/IInsuranceProviderAdapter.cs
```

It only exposes:

```csharp
AuthenticateAsync(...)
CheckEntitlementAsync(...)
SubmitPrescriptionAsync(...)
FetchPrescriptionAsync(...)
EditPrescriptionAsync(...)
CancelPrescriptionAsync(...)
FetchReferenceDataAsync(...)
```

Current `TaminInsuranceAdapter` implements only part of the SDK:
- authentication via `ITaminCredentialSessionStore`;
- private-practice eligibility lookup;
- submit prescription through `TaminPrescriptionMapper.RegisterPrescriptionAsync`;
- fetch registered prescription;
- edit prescription;
- cancel/delete prescription;
- fetch reference data by choosing `GetDrugListAsync` or `GetServiceListAsync`.

The reference-data fetch is too thin. It only switches on `CodeSystem == "drug"` and otherwise calls `GetServiceListAsync`. It does not expose search parameters, paging, all-page listing, drug amount, drug instructions, prescription types, or paraclinic tariffs.

The current sync path is:

```txt
SyncReferenceDataCommand
  -> ProviderCodeSyncHandler
      -> IInsuranceProviderAdapter.FetchReferenceDataAsync(...)
      -> SyncProviderCodesCommand
          -> SyncProviderCodesHandler
```

The sync handler stores provider data under a generated code system key:

```txt
{providerKey}.{codeSystem}
```

but it currently treats idempotency only by `SourceVersion`, not by `{ProviderKey, CodeSystem, Version}`. That can collide when multiple provider code systems return the same version string such as `unknown`.

### 2.2 Current Vitarish capability enum

`InsuranceProviderCapabilityType` already has several capabilities that map to Tamin but are not fully declared by the current Tamin adapter:

```csharp
SupportsIdentityVerification
SupportsEntitlementCheck
SupportsVisitPrescription
SupportsDrugPrescription
SupportsParaclinicPrescription
SupportsMedicalServicePrescription
SupportsReferralPrescription
SupportsPhysiotherapyPrescription
SupportsDentalPrescription
SupportsFetchPrescription
SupportsEditPrescription
SupportsCancelPrescription
SupportsReferenceDrugSync
SupportsReferenceServiceSync
SupportsPriceInquiry
SupportsAllowedCountInquiry
SupportsWarningInquiry
SupportsProviderSession
SupportsOtpAuthentication
SupportsDigitalSignature
SupportsDitasSubmission
SupportsPaperFallbackRegistration
```

Current `TaminInsuranceAdapter.SupportedCapabilities` should be expanded for implemented Tamin operations:
- add `SupportsReferralPrescription`;
- add `SupportsPhysiotherapyPrescription`;
- add `SupportsWarningInquiry`;
- add `SupportsDentalPrescription` only if warning/dental methods are exposed as dental-specific workflows;
- do not add identity, pharmacy, price, allowed-count, digital-signature, Ditas, or paper fallback unless real SDK methods exist.

### 2.3 Current SDK state

The `Vorn.Tamin` SDK exposes these implemented public surfaces:

#### Session and role surfaces

```txt
TaminSession
TaminClient
ServiceClient
ReferenceDataClient
PrescriptionClient
DentistryClient
ReferralClient
EligibilityClient
HospitalizationClient
DoctorClient
SecretaryClient
NurseClient
AuthClient
PkceChallenge
```

`TaminSession` exposes:
- `Service`
- `ReferenceData`
- `Prescription`
- `Prescriptions`
- `Dentistry`
- `Referrals`
- `Eligibility`
- `Hospitalization`
- `Doctor`
- `Secretary`
- `Nurse`
- empty placeholders: `Identity`, `Pharmacy`, `Paraclinic`

`TaminClient` is a role-aware facade over a session:
- `Doctor`
- `Secretary`
- `Nurse`
- `ReferenceData`
- `Prescriptions`
- `Dentistry`
- `Referrals`
- `Eligibility`
- `Hospitalization`

#### Auth/session methods

```csharp
new TaminSession(...)
TaminSession.CreateAsync(...)
TaminSession.RefreshTokenAsync(...)
TaminSession.ValidateTokenAsync(...)
new AuthClient(...)
AuthClient.CreateAuthorizationUrl(...)
AuthClient.ExchangeCodeAsync(...)
AuthClient.RefreshTokenV2Async(...)
AuthClient.CreateSignOutUrl(...)
AuthClient.SignOutAsync(...)
PkceChallenge.Create()
PkceChallenge.FromVerifier(...)
```

#### Reference data methods

`ReferenceDataClient` and `ServiceClient` expose:

```csharp
GetAllServicesAsync(IReadOnlyDictionary<string,string?>? query = null, ...)
GetPrescriptionTypeAsync(IReadOnlyDictionary<string,string?>? query = null, ...)
GetParaclinicTarefAsync(IReadOnlyDictionary<string,string?>? query = null, ...)
GetDrugAmountAsync(IReadOnlyDictionary<string,string?>? query = null, ...)
GetDrugInstructionAsync(IReadOnlyDictionary<string,string?>? query = null, ...)
GetDrugListAsync(
    string? searchText = null,
    string? drugCode = null,
    int? page = null,
    int? pageSize = null,
    bool? activeOnly = null,
    ...)
GetServiceListAsync(
    string? serviceType = null,
    string? serviceGroup = null,
    string? searchText = null,
    int? page = null,
    int? pageSize = null,
    bool? activeOnly = null,
    ...)
```

#### Prescription methods

`PrescriptionClient` exposes:

```csharp
RegisterVisitPrescriptionAsync(RegisterVisitPrescriptionRequest request, ...)
RegisterDrugPrescriptionAsync(RegisterDrugPrescriptionRequest request, ...)
RegisterParaclinicPrescriptionAsync(RegisterParaclinicPrescriptionRequest request, ...)
RegisterMedicalServicePrescriptionAsync(RegisterMedicalServicePrescriptionRequest request, ...)
RegisterReferralPrescriptionAsync(RegisterReferralPrescriptionRequest request, ...)
RegisterPhysiotherapyPrescriptionAsync(RegisterPhysiotherapyPrescriptionRequest request, ...)
GetRegisteredPrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, ...)
EditElectronicPrescriptionAsync(EditPrescriptionRequest request, ...)
DeleteElectronicPrescriptionAsync(DeletePrescriptionRequest request, ...)
CheckPrescriptionWarningAsync(CheckWarningRequest request, ...)
```

#### Eligibility methods

```csharp
EligibilityClient.LookupPrivatePracticeAsync(EligibilityLookupRequest request, ...)
```

#### Dentistry methods

```csharp
DentistryClient.CheckRulesAsync(CheckWarningRequest request, ...)
```

This delegates to the same warning operation as `PrescriptionClient.CheckPrescriptionWarningAsync`.

#### Referral methods

```csharp
ReferralClient.RegisterPrescriptionAsync(RegisterReferralPrescriptionRequest request, ...)
ReferralClient.GetCountsAsync(ReferralCountRequest request, ...)
ReferralClient.FindNoteReferralAsync(long masterId, string doctorId, string trackingCode, ...)
ReferralClient.FetchCartableAsync(string doctorNationalCode, string patientNationalCode, string trackingCode, ...)
ReferralClient.GetReferralListAsync(long referralId, ...)
ReferralClient.GetNoteDetailAsync(ReferralFeedbackRequest request, ...)
ReferralClient.GetPatientOpenCountAsync(string patientNationalCode, ...)
```

#### Nurse methods

```csharp
NurseClient.GetTodoListAsync(NurseTodoListRequest request, ...)
NurseClient.RecordActionAsync(NurseActionWorkflowRequest request, ...)
```

#### Hospitalization methods

```csharp
HospitalizationClient.CreateAsync(HospitalizationCreateRequest request, ...)
HospitalizationClient.GetSecretaryListAsync(HospitalizationSecretaryListRequest request, ...)
```

#### Explicitly empty / not implementable from this SDK version

The SDK has placeholders for these surfaces but no public operation methods:
- `IdentityClient`
- `PharmacyClient`
- `ParaclinicClient`

The Vitarish plan should not pretend those operations exist.

---

## 3. Design principles

### 3.1 Do not leak `Vorn.Tamin` types outside Infrastructure

Keep these inside Infrastructure:
- `TaminSession`
- `TaminClient`
- all `Vorn.Tamin.*Request` SDK DTOs;
- `JsonElement` raw SDK responses;
- provider endpoint quirks.

Application and Contracts should get Vitarish-owned DTOs.

### 3.2 Split generic insurance-provider operations from Tamin-specific operations

`IInsuranceProviderAdapter` is currently a canonical provider interface for shared prescription flows. Do not overload it with every provider-specific operation immediately.

Recommended split:

1. Keep `IInsuranceProviderAdapter` for cross-provider prescription flows.
2. Add a Tamin-specific infrastructure gateway first:
   - `ITaminMedicalInfrastructureGateway`
   - `TaminMedicalInfrastructureGateway`
3. Later, expose selected methods through application interfaces:
   - generic provider reference-data interface;
   - Tamin-specific admin/provider interface;
   - `IMedicalClient` methods only after API contract decisions.

### 3.3 Every SDK method must have one of three states

Each method gets a row in the implementation matrix:

```txt
State = Implement now | Implement later behind DTO | Explicitly unsupported
```

No silent gaps.

### 3.4 Preserve raw provider payloads

Because `Vorn.Tamin` returns `JsonElement` for most methods, Vitarish should:
- map common fields for search/sync UX;
- preserve raw JSON as `RawJson` or `MetadataJson`;
- keep provider operation name and code system in metadata;
- store enough evidence for support tickets.

### 3.5 Keep provider command/query separation

Reference data, search, lookup, eligibility, status fetch, warning check, referral counts, nurse to-do, hospitalization list are queries.

Prescription submit/edit/delete/referral/hospitalization create/nurse action/signout/token exchange are commands.

This distinction matters for resilience and idempotency:
- retry queries safely;
- retry commands only through outbox/idempotency/reconciliation.

---

## 4. Proposed architecture

### 4.1 New internal infrastructure gateway

Add:

```txt
src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/
  ITaminMedicalInfrastructureGateway.cs
  TaminMedicalInfrastructureGateway.cs
  TaminSessionFactory.cs
  ITaminSessionFactory.cs
  TaminProviderJsonReader.cs
  TaminReferenceDataMapper.cs
  TaminWorkflowMapper.cs
  TaminProviderOperationNames.cs
```

Purpose:
- concentrate all direct calls to `Vorn.Tamin`;
- make methods complete and discoverable;
- keep `TaminInsuranceAdapter` small;
- prepare for later interface wiring.

Candidate interface:

```csharp
internal interface ITaminMedicalInfrastructureGateway
{
    // Session / auth
    Task<TaminAuthResult> AuthenticateAsync(TaminAuthenticateOperationRequest request, CancellationToken ct);
    Task<TaminTokenRefreshResult> RefreshTokenAsync(TaminTokenRefreshOperationRequest request, CancellationToken ct);
    Task<TaminTokenValidationResult> ValidateTokenAsync(TaminTokenValidationOperationRequest request, CancellationToken ct);
    TaminAuthorizationUrlResult CreateAuthorizationUrl(TaminAuthorizationUrlOperationRequest request);
    Task<TaminTokenExchangeResult> ExchangeAuthorizationCodeAsync(TaminTokenExchangeOperationRequest request, CancellationToken ct);
    Task<TaminTokenRefreshResult> RefreshTokenV2Async(TaminTokenRefreshV2OperationRequest request, CancellationToken ct);
    TaminSignOutUrlResult CreateSignOutUrl(TaminSignOutUrlOperationRequest request);
    Task<TaminProviderVoidResult> SignOutAsync(TaminSignOutOperationRequest request, CancellationToken ct);

    // Reference data
    Task<TaminReferencePageResult> SearchDrugsAsync(TaminDrugSearchOperationRequest request, CancellationToken ct);
    Task<TaminReferencePageResult> GetDrugPageAsync(TaminDrugPageOperationRequest request, CancellationToken ct);
    IAsyncEnumerable<TaminReferencePageResult> ListAllDrugPagesAsync(TaminDrugListAllOperationRequest request, CancellationToken ct);
    Task<TaminReferencePageResult> SearchServicesAsync(TaminServiceSearchOperationRequest request, CancellationToken ct);
    Task<TaminReferencePageResult> GetServicePageAsync(TaminServicePageOperationRequest request, CancellationToken ct);
    IAsyncEnumerable<TaminReferencePageResult> ListAllServicePagesAsync(TaminServiceListAllOperationRequest request, CancellationToken ct);
    Task<TaminReferencePageResult> GetAllServicesAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken ct);
    Task<TaminReferencePageResult> GetPrescriptionTypesAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken ct);
    Task<TaminReferencePageResult> GetParaclinicTariffsAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken ct);
    Task<TaminReferencePageResult> GetDrugAmountsAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken ct);
    Task<TaminReferencePageResult> GetDrugInstructionsAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken ct);

    // Eligibility
    Task<TaminProviderJsonResult> LookupPrivatePracticeEligibilityAsync(TaminEligibilityOperationRequest request, CancellationToken ct);

    // Prescriptions
    Task<TaminProviderJsonResult> RegisterVisitPrescriptionAsync(TaminRegisterVisitOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> RegisterDrugPrescriptionAsync(TaminRegisterDrugOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> RegisterParaclinicPrescriptionAsync(TaminRegisterParaclinicOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> RegisterMedicalServicePrescriptionAsync(TaminRegisterMedicalServiceOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> RegisterReferralPrescriptionAsync(TaminRegisterReferralOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> RegisterPhysiotherapyPrescriptionAsync(TaminRegisterPhysiotherapyOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> RegisterHospitalizationPrescriptionAsync(TaminRegisterHospitalizationOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> GetRegisteredPrescriptionAsync(TaminGetRegisteredPrescriptionOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> EditElectronicPrescriptionAsync(TaminEditPrescriptionOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> DeleteElectronicPrescriptionAsync(TaminDeletePrescriptionOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> CheckPrescriptionWarningAsync(TaminWarningOperationRequest request, CancellationToken ct);

    // Referrals
    Task<TaminProviderJsonResult> GetReferralCountsAsync(TaminReferralCountOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> FindNoteReferralAsync(TaminFindNoteReferralOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> FetchReferralCartableAsync(TaminFetchReferralCartableOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> GetReferralListAsync(TaminReferralListOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> GetReferralNoteDetailAsync(TaminReferralNoteDetailOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> GetPatientOpenReferralCountAsync(TaminPatientOpenReferralCountOperationRequest request, CancellationToken ct);

    // Nurse
    Task<TaminProviderJsonResult> GetNurseTodoListAsync(TaminNurseTodoListOperationRequest request, CancellationToken ct);
    Task<TaminProviderJsonResult> RecordNurseActionAsync(TaminNurseActionOperationRequest request, CancellationToken ct);

    // Hospitalization
    Task<TaminProviderJsonResult> GetHospitalizationSecretaryListAsync(TaminHospitalizationSecretaryListOperationRequest request, CancellationToken ct);
}
```

This is intentionally exhaustive. Later, this can be split into smaller interfaces:
- `ITaminAuthGateway`
- `ITaminReferenceDataGateway`
- `ITaminPrescriptionGateway`
- `ITaminReferralGateway`
- `ITaminNurseGateway`
- `ITaminHospitalizationGateway`

### 4.2 Session factory

Current session creation is spread between `TaminCredentialSessionStore` and `TaminInsuranceAdapter`.

Add:

```csharp
internal interface ITaminSessionFactory
{
    Task<TaminSessionResult> CreateAuthenticatedSessionAsync(Guid doctorId, Guid clinicId, CancellationToken ct);
    TaminSession CreateReferenceDataSession(bool requiresToken);
    AuthClient CreateAuthClient();
}
```

Responsibilities:
- resolve `TaminOptions`;
- resolve endpoint mode;
- create `HttpClient` from `IHttpClientFactory`;
- create authenticated session from cached or refreshed credentials;
- create no-token reference-data session for operations that do not require a doctor session;
- create `AuthClient` for PKCE/sign-out/token flows;
- centralize exception handling and error mapping.

### 4.3 Update `TaminOptions`

Current options:

```csharp
BaseUrl
ApiKey
IsEnabled
UseSandbox
SandboxBaseUrl
EffectiveBaseUrl
```

Add:

```csharp
public TaminEndpointMode Endpoint { get; set; } = TaminEndpointMode.Production;
public int DefaultPageSize { get; set; } = 100;
public int MaxPageSize { get; set; } = 500;
public int MaxReferencePages { get; set; } = 1000;
public bool ReferenceDataRequiresToken { get; set; } = false;
public TimeSpan TokenSkew { get; set; } = TimeSpan.FromMinutes(2);
public string? OAuthRedirectUri { get; set; }
public string? SignOutRedirectUri { get; set; }
```

Define local enum to avoid leaking SDK enum into config if preferred:

```csharp
public enum TaminEndpointMode
{
    Production,
    Sandbox
}
```

Map to `Vorn.Tamin.TaminEndpoint`.

Important: current code passes `baseUri` into `new TaminSession(...)` but does not pass the SDK `endpoint` argument, so the SDK will use the default production generated gateway even when the base URL points to sandbox. Fix this.

### 4.4 Credential model gap

`TaminSessionResult` currently names one field `DoctorNationalCode`, but `TaminCredentialSessionStore.Success(...)` fills it from `credential.ProviderUsername`.

Plan:
1. Decide what each credential field means:
   - `ProviderDoctorIdentifier`: provider doctor ID / medical council number / `docId`;
   - `ProviderUsername`: login username; often not necessarily doctor national code;
   - `ProviderClinicIdentifier`: `siamId`;
   - `CredentialSecretRef`: password/secret;
   - missing: explicit doctor national code if different from username.
2. Add one of:
   - `ProviderDoctorNationalCode` property to `DoctorProviderCredential`; or
   - use credential configuration values to store it; or
   - resolve it from Users/Doctor profile.
3. Update `TaminSessionResult`:
   - `DoctorTaminId`
   - `DoctorNationalCode`
   - `ClinicSiamId`
   - `Username`
4. Do not silently use `ProviderUsername` as national code unless that is an explicit configuration choice.

### 4.5 Provider date formatting gap

Current mapper uses:

```csharp
(prescription.SubmittedAt ?? prescription.CreatedAt).ToString("yyyy-MM-dd")
```

The SDK validates provider date fields as eight-character Jalali strings. Add:

```txt
ITaminDateFormatter
TaminDateFormatter
```

Behavior:
- input: `DateTime` / `DateOnly`;
- output: `yyyyMMdd` Jalali, e.g. `14030601`;
- validate outbound strings before calling SDK;
- do not send Gregorian ISO dates to Tamin.

### 4.6 Raw JSON reader

Add:

```csharp
internal static class TaminProviderJsonReader
```

Capabilities:
- read root envelope and unwrap `data`;
- enumerate arrays from:
  - root array;
  - `items`;
  - `data`;
  - `data.items`;
  - `result`;
  - `result.items`;
- read paging metadata:
  - `page`
  - `page_size`
  - `total`
  - `total_count`
  - `has_next`
  - `next_page`
  - `last_page`
  - `pages`
- read common code/name fields:
  - drug: `drug_code`, `drugCode`, `code`, `id`
  - drug name: `drug_name`, `drugName`, `name`, `title`
  - service: `service_code`, `serviceCode`, `srvCode`, `code`
  - service name: `service_name`, `serviceName`, `srvName`, `name`, `title`
  - active: `is_active`, `active`, `status`
  - category/group/type: `category`, `service_group`, `service_type`, `group`, `type`
- preserve raw item JSON.

---

## 5. Exhaustive method inventory and implementation decisions

### 5.1 Auth and session

| SDK method | Current Vitarish coverage | Decision | Implementation note |
|---|---:|---|---|
| `new TaminSession(...)` | Partial | Implement via `ITaminSessionFactory` | Use for cached token and reference-data sessions. Pass correct SDK endpoint. |
| `TaminSession.CreateAsync(...)` | Yes, in `TaminCredentialSessionStore` | Keep, centralize | Move construction details into factory. Store token and expiry. |
| `TaminSession.RefreshTokenAsync(...)` | No | Implement later if refresh token is stored | Current credentials only store access token. Add refresh-token storage before exposing. |
| `TaminSession.ValidateTokenAsync(...)` | No | Implement infrastructure method | Useful for credential health and test credential. |
| `AuthClient.CreateAuthorizationUrl(...)` | No | Implement interface-ready method | Needed for future PKCE onboarding. Store verifier/state outside provider adapter. |
| `AuthClient.ExchangeCodeAsync(...)` | No | Implement interface-ready method | Needs secure storage of verifier/state. |
| `AuthClient.RefreshTokenV2Async(...)` | No | Implement after refresh token support | Requires audience/national code. |
| `AuthClient.CreateSignOutUrl(...)` | No | Implement interface-ready method | Admin/user action. |
| `AuthClient.SignOutAsync(...)` | No | Implement interface-ready command | Clear cached tokens after success. |
| `PkceChallenge.Create()` | No | Implement inside onboarding service | Store verifier securely. |
| `PkceChallenge.FromVerifier(...)` | No | Implement inside callback service | Never expose verifier to UI after creation. |

### 5.2 Reference data

| SDK method | Current Vitarish coverage | Decision | Implementation note |
|---|---:|---|---|
| `GetDrugListAsync(searchText, drugCode, page, pageSize, activeOnly)` | Partial, no parameters | Implement now | Add search/page/list-all operations. |
| `GetServiceListAsync(serviceType, serviceGroup, searchText, page, pageSize, activeOnly)` | Partial, no parameters | Implement now | Add search/page/list-all operations. |
| `GetAllServicesAsync(query)` | No | Implement now | Useful for raw official service list and sync fallback. |
| `GetPrescriptionTypeAsync(query)` | No | Implement now | Import as `tamin.prescription-type`. |
| `GetParaclinicTarefAsync(query)` | No | Implement now | Import as `tamin.paraclinic-tariff`. |
| `GetDrugAmountAsync(query)` | No | Implement now | Import as `tamin.drug-amount`; also currently underlies `GetDrugListAsync` in SDK. |
| `GetDrugInstructionAsync(query)` | No | Implement now | Import as `tamin.drug-instruction`. |

### 5.3 Prescription writing/query/mutation

| SDK method | Current Vitarish coverage | Decision | Implementation note |
|---|---:|---|---|
| `RegisterVisitPrescriptionAsync` | Yes | Keep, fix date/identity | Uses `PrescriptionItemCategory.Visit`. |
| `RegisterDrugPrescriptionAsync` | Yes | Keep, enrich item mapping | Include more drug metadata from `MetadataJson` later: frequency, route, usage note, repeat count. |
| `RegisterParaclinicPrescriptionAsync` | Yes as default for several categories | Keep, improve category mapping | Map lab/imaging/paraclinic/equipment deliberately. |
| `RegisterMedicalServicePrescriptionAsync` | Yes | Keep, improve service metadata | Add service group/effective date/priority if available. |
| `RegisterReferralPrescriptionAsync` | Not reached despite domain category | Implement | Requires target specialty/provider type/reason; likely from item metadata. |
| `RegisterPhysiotherapyPrescriptionAsync` | Yes for category but incomplete | Improve | Current mapper only sends item descriptions; add session count/effective date. |
| `GetRegisteredPrescriptionAsync` | Yes | Keep, fix parameters | Current adapter passes tracking code where SDK expects doctor national code in one call path; verify signature and usage. |
| `EditElectronicPrescriptionAsync` | Yes | Keep, improve edit item mapper | Current mapper sends only code and quantity; validate required Kiota fields. |
| `DeleteElectronicPrescriptionAsync` | Yes | Keep | Consider remote state reconciliation after success. |
| `CheckPrescriptionWarningAsync` | No | Implement now | Add warning request/result DTO and capability. |
| `DentistryClient.CheckRulesAsync` | No | Implement as alias | Same payload/result as warning check. |

### 5.4 Eligibility

| SDK method | Current Vitarish coverage | Decision | Implementation note |
|---|---:|---|---|
| `EligibilityClient.LookupPrivatePracticeAsync` | Yes | Keep, fix identity field | Requires `SiamId`, `DoctorId`, `DoctorNationalCode`, `PatientNationalCode`. |

### 5.5 Referral

| SDK method | Current Vitarish coverage | Decision | Implementation note |
|---|---:|---|---|
| `ReferralClient.RegisterPrescriptionAsync` | No | Implement | Either through `TaminPrescriptionMapper` or workflow gateway. |
| `ReferralClient.GetCountsAsync` | No | Implement interface-ready query | Map to raw result initially. |
| `ReferralClient.FindNoteReferralAsync` | No | Implement interface-ready query | Requires `masterId`, `doctorId`, `trackingCode`. |
| `ReferralClient.FetchCartableAsync` | No | Implement interface-ready query | Requires doctor national code, patient national code, tracking code. |
| `ReferralClient.GetReferralListAsync` | No | Implement interface-ready query | Requires referral ID. |
| `ReferralClient.GetNoteDetailAsync` | No | Implement interface-ready query | Requires ID and master parent. |
| `ReferralClient.GetPatientOpenCountAsync` | No | Implement interface-ready query | Requires patient national code. |

### 5.6 Nurse

| SDK method | Current Vitarish coverage | Decision | Implementation note |
|---|---:|---|---|
| `NurseClient.GetTodoListAsync` | No | Implement interface-ready query | Requires SIAM ID and nurse national code. |
| `NurseClient.RecordActionAsync` | No | Implement interface-ready command | Requires SIAM ID, nurse national code, note detail IDs. Treat as command; avoid blind retry. |

### 5.7 Hospitalization

| SDK method | Current Vitarish coverage | Decision | Implementation note |
|---|---:|---|---|
| `HospitalizationClient.CreateAsync` | No | Implement interface-ready command | Requires hospitalization-specific detail rows and ICD10s. Not same as ordinary service prescription. |
| `HospitalizationClient.GetSecretaryListAsync` | No | Implement interface-ready query | Requires SIAM ID and secretary national code. |

### 5.8 Empty SDK placeholders

| SDK surface | Current Vitarish coverage | Decision |
|---|---:|---|
| `IdentityClient` | No | Explicitly unsupported until SDK exposes methods. |
| `PharmacyClient` | No | Explicitly unsupported until SDK exposes methods. |
| `ParaclinicClient` | No | Explicitly unsupported until SDK exposes methods. |
| Price helpers | No | Explicitly unsupported in this SDK version. |
| Allowed-count helpers | No | Explicitly unsupported in this SDK version. |

---

## 6. Detailed implementation phases

## Phase 0 — Baseline safety

### 0.1 Create a method coverage test matrix

Add a living file:

```txt
docs/platform/modules/medical/providers/tamin-method-coverage.md
```

Columns:

```txt
SDK surface
SDK method
Vitarish infrastructure method
Application abstraction
API route
Client method
UI surface
Status
Notes
```

Initialize every method from Section 5.

### 0.2 Add tests that lock current behavior

Before refactor:
- `TaminCredentialSessionStore` authentication succeeds with token.
- Cached token path creates session.
- `TaminInsuranceAdapter.FetchReferenceDataAsync("drug")` calls drug list.
- `FetchReferenceDataAsync("service")` calls service list.
- `SubmitPrescriptionAsync` rejects mixed categories.
- `SubmitPrescriptionAsync` maps drug, visit, medical service, paraclinic, physiotherapy.
- `Fetch/Edit/Cancel` require valid remote header ID.
- Error mapping still maps `ConnectionError`, `UserLoginException`, `HttpRequestException`, and timeout.

### 0.3 Build must remain green after every phase

Run at minimum:

```bash
dotnet restore
dotnet build
dotnet test
```

If repo uses solution filters or `.slnx`, use the project’s existing build entry point.

---

## Phase 1 — Session and options hardening

### 1.1 Add endpoint mode to options

Modify:

```txt
src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/TaminOptions.cs
```

Add endpoint mapping and page settings.

Acceptance:
- sandbox uses SDK `TaminEndpoint.Sandbox`;
- production uses SDK `TaminEndpoint.Production`;
- invalid base URL fails at startup or first provider operation with a clear integration error.

### 1.2 Add `ITaminSessionFactory`

New files:

```txt
ITaminSessionFactory.cs
TaminSessionFactory.cs
```

Move this logic out of `TaminCredentialSessionStore`:
- create `HttpClient`;
- resolve `EffectiveBaseUrl`;
- resolve SDK endpoint;
- create `TaminSession`;
- create `AuthClient`.

`TaminCredentialSessionStore` should become responsible for credentials and tokens only, not constructing every SDK surface itself.

### 1.3 Fix doctor identity mapping

Add a decision:
- either `ProviderUsername` is definitely the doctor national code; or
- create a dedicated field.

Preferred:
- `ProviderUsername`: login identity;
- `ProviderDoctorIdentifier`: Tamin doctor ID / `docId`;
- `ProviderDoctorNationalCode`: national code;
- `ProviderClinicIdentifier`: SIAM ID.

Update:
- `TaminSessionResult`;
- credential save command;
- credential setup UI later;
- credential test query.

### 1.4 Add Jalali date formatter

New files:

```txt
ITaminDateFormatter.cs
TaminDateFormatter.cs
```

Use `System.Globalization.PersianCalendar`.

Acceptance:
- `2026-06-01` converts to the correct eight-digit Jalali date;
- mapper never sends `yyyy-MM-dd` to SDK;
- unit tests cover conversion and validation.

---

## Phase 2 — Complete reference-data infrastructure

### 2.1 Add operation DTOs

New file:

```txt
TaminReferenceDataOperationContracts.cs
```

Suggested records:

```csharp
internal sealed record TaminDrugSearchOperationRequest(
    string? SearchText,
    string? DrugCode,
    int Page = 1,
    int PageSize = 20,
    bool ActiveOnly = true);

internal sealed record TaminDrugListAllOperationRequest(
    string? SearchText,
    string? DrugCode,
    int PageSize,
    bool ActiveOnly,
    int? MaxPages = null);

internal sealed record TaminServiceSearchOperationRequest(
    string? ServiceType,
    string? ServiceGroup,
    string? SearchText,
    int Page = 1,
    int PageSize = 20,
    bool ActiveOnly = true);

internal sealed record TaminServiceListAllOperationRequest(
    string? ServiceType,
    string? ServiceGroup,
    string? SearchText,
    int PageSize,
    bool ActiveOnly,
    int? MaxPages = null);

internal sealed record TaminRawReferenceQueryOperationRequest(
    string CodeSystem,
    IReadOnlyDictionary<string, string?> Query);
```

Suggested result:

```csharp
internal sealed record TaminReferencePageResult(
    string CodeSystem,
    int? Page,
    int? PageSize,
    int? TotalCount,
    bool? HasNextPage,
    string DataVersion,
    IReadOnlyList<TaminReferenceItem> Items,
    string RawJson);

internal sealed record TaminReferenceItem(
    string Code,
    string DisplayName,
    string? Category,
    bool IsActive,
    string? ProviderKind,
    string RawJson);
```

### 2.2 Implement one-page methods

In `TaminMedicalInfrastructureGateway`:

```csharp
SearchDrugsAsync(...)
GetDrugPageAsync(...)
SearchServicesAsync(...)
GetServicePageAsync(...)
GetAllServicesAsync(...)
GetPrescriptionTypesAsync(...)
GetParaclinicTariffsAsync(...)
GetDrugAmountsAsync(...)
GetDrugInstructionsAsync(...)
```

Mapping:
- `SearchDrugsAsync` calls `session.Service.GetDrugListAsync(searchText, drugCode, page, pageSize, activeOnly, ct)`.
- `SearchServicesAsync` calls `session.Service.GetServiceListAsync(serviceType, serviceGroup, searchText, page, pageSize, activeOnly, ct)`.
- raw helpers call corresponding SDK methods with a dictionary.

### 2.3 Implement all-page methods

Use `IAsyncEnumerable<TaminReferencePageResult>` for large lists.

Algorithm:
1. Start with page 1.
2. Use configured `PageSize`.
3. Call the one-page method.
4. Yield page result.
5. Stop if:
   - item count is zero;
   - provider says `HasNextPage == false`;
   - provider total count is known and retrieved count >= total;
   - item count < page size and no total says otherwise;
   - page reaches `MaxReferencePages`.
6. Throw/mapped failure if max pages is reached while result appears incomplete.

Do not materialize every page unless caller explicitly asks for a full sync.

### 2.4 Extend `FetchReferenceDataAsync`

Current contract only accepts `CodeSystem` and `SinceVersion`. Keep it compatible, but teach Tamin adapter more code systems:

```txt
drug
drugs
service
services
all-services
prescription-type
prescription-types
paraclinic-tariff
paraclinic-taref
drug-amount
drug-amounts
drug-instruction
drug-instructions
```

For `drug` and `service`, the default behavior should list all pages, not only page 1, when called from provider sync.

For UI search, do not use `FetchReferenceDataAsync`; add dedicated search operations later.

### 2.5 Fix sync idempotency

Modify:

```txt
src/Modules/Medical/Infrastructure/Handlers/SyncProviderCodesHandler.cs
```

Current lookup uses only `SourceType` and `SourceVersion`.

Change idempotency lookup to also include `SourceLocator == systemKey`:

```csharp
b.SourceType == ImportSourceType.InsuranceProvider
&& b.SourceVersion == request.DataVersion
&& b.SourceLocator == systemKey
```

This prevents collisions across:
- `tamin.drug`
- `tamin.service`
- `tamin.drug-instruction`
- `tamin.drug-amount`
- etc.

### 2.6 Store metadata

Current `SyncProviderCodeItem` only stores code/display/category/isActive, and `MedicalCode` import ignores category.

Options:
1. Extend `MedicalCode` with `MetadataJson` if domain permits.
2. Add provider metadata table:
   - `ProviderReferenceCodeMetadata`
   - fields: `ProviderKey`, `CodeSystem`, `Code`, `RawJson`, `Category`, `SyncedAt`, `ImportBatchId`.
3. Minimal first step: add `Description`/`LongDescription` or category where existing model supports it.

Recommended:
- add provider metadata table later if domain model should remain clean;
- for this phase, preserve raw payload in import batch artifact if available.

---

## Phase 3 — Complete prescription and warning coverage

### 3.1 Update Tamin capability declaration

In `TaminInsuranceAdapter`, add capabilities that are actually implemented after this phase:

```csharp
SupportsReferralPrescription
SupportsPhysiotherapyPrescription
SupportsWarningInquiry
SupportsDentalPrescription
```

Do not add:
- identity;
- price;
- allowed count;
- digital signature;
- Ditas;
- paper fallback.

### 3.2 Refactor prescription mapping into explicit methods

Current `TaminPrescriptionMapper.RegisterPrescriptionAsync` uses a category switch. Keep the switch but delegate to explicit mapping methods:

```csharp
RegisterVisitAsync(...)
RegisterDrugAsync(...)
RegisterParaclinicAsync(...)
RegisterMedicalServiceAsync(...)
RegisterReferralAsync(...)
RegisterPhysiotherapyAsync(...)
```

Each mapping method should:
- validate required provider identifiers;
- use Jalali date formatter;
- preserve string identifiers;
- read optional metadata from `PrescriptionItem.MetadataJson` if available;
- throw/return validation failure before SDK call when required provider fields are missing.

### 3.3 Add metadata conventions for prescription items

For categories that need more than the current domain fields:

#### Drug metadata

```json
{
  "frequency": "...",
  "route": "...",
  "usageNote": "...",
  "repeatCount": 0,
  "drugAmountId": "...",
  "drugInstructionId": "..."
}
```

#### Service metadata

```json
{
  "serviceGroup": "...",
  "effectiveDate": "14030601",
  "priority": "...",
  "description": "..."
}
```

#### Referral metadata

```json
{
  "targetSpecialty": "...",
  "targetProviderType": "...",
  "reason": "...",
  "description": "..."
}
```

#### Physiotherapy metadata

```json
{
  "sessionCount": 10,
  "effectiveDate": "14030601",
  "description": "..."
}
```

#### Hospitalization metadata

```json
{
  "siamId": "...",
  "message": "...",
  "referralDetails": [
    {
      "patientNationalCode": "...",
      "referralHijriDate": "14030601",
      "siamId": "...",
      "icd10s": [
        { "icdCode": "...", "icdId": "...", "icdName": "..." }
      ],
      "message": "..."
    }
  ]
}
```

### 3.4 Implement warning check

Add application abstraction DTOs later; infrastructure first:

```csharp
TaminWarningOperationRequest
TaminWarningOperationResult
```

Map to SDK:

```csharp
session.Prescription.CheckPrescriptionWarningAsync(...)
```

or

```csharp
session.Dentistry.CheckRulesAsync(...)
```

The result can initially be raw JSON plus normalized severity/message list if easy.

### 3.5 Verify fetch/edit/delete parameter semantics

Current Vitarish `FetchPrescriptionAsync` calls:

```csharp
session.Prescription.GetRegisteredPrescriptionAsync(
    headerId,
    submission.RemoteTrackingCode,
    patientNationalId,
    cancellationToken)
```

But the SDK signature is:

```csharp
GetRegisteredPrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, ...)
```

Plan:
- review and fix all call sites;
- pass doctor national code and doctor ID, not tracking code or patient national ID;
- keep tracking code in Vitarish result mapping and reconciliation metadata, not SDK path parameters unless SDK changes.

### 3.6 Update edit item mapper

Current `TaminEditedPrescriptionItemMapper` emits only:

```txt
drug_code/service_code
quantity
```

Plan:
- check SDK `TaminRequestMapper.ToNoteDetails(...)` expected fields;
- include category-specific properties where required;
- include original detail IDs if provider edit requires them;
- add unit tests for drug and service edit payloads.

---

## Phase 4 — Referral, nurse, hospitalization operations

These are provider-specific and should be implemented behind the Tamin infrastructure gateway first. Do not add all of them directly to generic `IInsuranceProviderAdapter`.

### 4.1 Referral operations

Add operation contracts:

```csharp
TaminReferralCountOperationRequest
TaminFindNoteReferralOperationRequest
TaminFetchReferralCartableOperationRequest
TaminReferralListOperationRequest
TaminReferralNoteDetailOperationRequest
TaminPatientOpenReferralCountOperationRequest
```

Implement:
- `GetReferralCountsAsync` -> `session.Referrals.GetCountsAsync(...)`
- `FindNoteReferralAsync` -> `session.Referrals.FindNoteReferralAsync(...)`
- `FetchReferralCartableAsync` -> `session.Referrals.FetchCartableAsync(...)`
- `GetReferralListAsync` -> `session.Referrals.GetReferralListAsync(...)`
- `GetReferralNoteDetailAsync` -> `session.Referrals.GetNoteDetailAsync(...)`
- `GetPatientOpenReferralCountAsync` -> `session.Referrals.GetPatientOpenCountAsync(...)`

Return:
- raw JSON;
- normalized count where obvious;
- operation name;
- provider identifiers.

### 4.2 Nurse operations

Add contracts:

```csharp
TaminNurseTodoListOperationRequest
TaminNurseActionOperationRequest
```

Implement:
- `GetNurseTodoListAsync` -> `session.Nurse.GetTodoListAsync(...)`
- `RecordNurseActionAsync` -> `session.Nurse.RecordActionAsync(...)`

Resilience:
- `GetTodoList` is safe query;
- `RecordAction` is a command and should not be blindly retried unless idempotency is confirmed.

### 4.3 Hospitalization operations

Add contracts:

```csharp
TaminRegisterHospitalizationOperationRequest
TaminHospitalizationSecretaryListOperationRequest
```

Implement:
- `RegisterHospitalizationPrescriptionAsync` -> `session.Hospitalization.CreateAsync(...)`
- `GetHospitalizationSecretaryListAsync` -> `session.Hospitalization.GetSecretaryListAsync(...)`

Do not treat hospitalization create as an ordinary service prescription. It has required:
- SIAM ID;
- prescription date;
- referral details;
- ICD10 items.

---

## Phase 5 — Later application/API/client interface wiring

After infrastructure coverage is complete and tested, add public module contracts selectively.

### 5.1 Application abstractions

Add focused abstractions under:

```txt
src/Modules/Medical/Application/Abstractions/Prescriptions/
```

Recommended:

```csharp
public interface IProviderReferenceDataAdapter
{
    Task<AdapterResult<ProviderReferencePageResponse>> SearchDrugsAsync(...);
    Task<AdapterResult<ProviderReferencePageResponse>> SearchServicesAsync(...);
    IAsyncEnumerable<ProviderReferencePageResponse> ListDrugPagesAsync(...);
    IAsyncEnumerable<ProviderReferencePageResponse> ListServicePagesAsync(...);
    Task<AdapterResult<ProviderReferenceDataResponse>> FetchReferenceDataAsync(...);
}
```

For Tamin-specific provider operations, prefer a separate abstraction:

```csharp
public interface ITaminProviderWorkflowAdapter
{
    Task<AdapterResult<ProviderRawResponse>> CheckWarningsAsync(...);
    Task<AdapterResult<ProviderRawResponse>> GetReferralCountsAsync(...);
    Task<AdapterResult<ProviderRawResponse>> GetNurseTodoListAsync(...);
    Task<AdapterResult<ProviderRawResponse>> GetHospitalizationSecretaryListAsync(...);
}
```

Do not force non-Tamin providers to implement Tamin-only workflows.

### 5.2 Contracts

Add requests/responses under:

```txt
src/Modules/Medical/Contracts/Requests/
src/Modules/Medical/Contracts/Responses/
```

Suggested public contract names:

```csharp
ProviderDrugSearchRequest
ProviderServiceSearchRequest
ProviderReferencePageResponse
ProviderReferenceItemResponse
ProviderReferenceSyncRequest
ProviderWarningCheckRequest
ProviderRawOperationRequest
ProviderRawOperationResponse
```

Later Tamin-specific requests:

```csharp
TaminReferralCountRequest
TaminReferralLookupRequest
TaminNurseTodoRequest
TaminNurseActionRequest
TaminHospitalizationSecretaryListRequest
```

### 5.3 API routes

Add route constants under:

```txt
src/Modules/Medical/Contracts/Routes/MedicalApiRoutes.cs
```

Suggested routes:

```txt
/providers/{providerKey}/reference/drugs/search
/providers/{providerKey}/reference/drugs/page
/providers/{providerKey}/reference/drugs/sync-all
/providers/{providerKey}/reference/services/search
/providers/{providerKey}/reference/services/page
/providers/{providerKey}/reference/services/sync-all
/providers/{providerKey}/reference/{codeSystem}/sync
/providers/{providerKey}/warnings/check
/providers/tamin/referrals/count
/providers/tamin/referrals/find-note
/providers/tamin/referrals/cartable
/providers/tamin/referrals/list
/providers/tamin/referrals/note-detail
/providers/tamin/referrals/patient-open-count
/providers/tamin/nurse/todo
/providers/tamin/nurse/actions
/providers/tamin/hospitalization/secretary-list
```

### 5.4 Client interface

After routes exist, extend:

```txt
src/Modules/Medical/Client/IMedicalClient.cs
src/Modules/Medical/Client/MedicalClient.cs
```

Add only stable public needs first:
- provider drug search;
- provider service search;
- provider reference sync by code system;
- warning check.

Defer raw Tamin referral/nurse/hospitalization methods until UI/user stories exist.

---

## 7. Data and sync strategy

### 7.1 Code system keys

Use explicit keys:

```txt
tamin.drug
tamin.service
tamin.all-service
tamin.prescription-type
tamin.paraclinic-tariff
tamin.drug-amount
tamin.drug-instruction
```

Potential later:
```txt
tamin.referral-specialty
tamin.icd10
```

only if SDK exposes relevant reference data.

### 7.2 Version strategy

Provider may not return a stable `version`.

Use this priority:
1. explicit provider `version`;
2. explicit provider `last_updated` or equivalent;
3. hash of normalized full payload for full sync;
4. timestamped import version only for forced syncs.

Do not default every sync to `unknown`; it breaks idempotency and traceability.

### 7.3 Page sync strategy

For full sync:
- fetch pages sequentially by default;
- respect cancellation token;
- log each page;
- avoid enormous memory accumulation;
- commit after full import to avoid partial active release;
- keep raw page logs if possible.

For search:
- only one page;
- no import;
- fast timeout;
- query-only resilience.

### 7.4 Existing local Medical search

The existing module already has local search endpoints:
- `SearchDrugsAsync`;
- `SearchMedicalServicesAsync`;
- `SearchPrescriptionReferenceCodesAsync`.

Keep them local.

Add provider-search endpoints only when the user needs live provider search. Distinguish names clearly:
- `SearchDrugsAsync` = local Medical catalog;
- `SearchProviderDrugsAsync` = live provider lookup;
- `SyncProviderReferenceDataAsync` = import provider catalog into local catalog.

---

## 8. Error mapping

Current `TaminInsuranceAdapter` maps:
- `ConnectionError`;
- `UserLoginException`;
- `HttpRequestException`;
- `TaskCanceledException`.

Expand to include SDK validation/auth exceptions:
- `TaminValidationException`;
- `AuthTokenNotSuppliedException`;
- `TaminAuthRequestException`;
- `MissingParamException`;
- `MissingConfigException`;
- `InvalidConfigException`;
- `PrescriptionNotCreatedException`;
- `TaminRouteNotDefinedException`;
- `TaminWorkflowNotImplementedException`.

Mapping rules:
- validation/config mistakes -> `ErrorCategory.Validation` or `BusinessRule`, non-retryable;
- auth/token/session -> `ErrorCategory.Authentication`, non-retryable unless refresh flow exists;
- route not defined -> provider contract/config error, non-retryable;
- provider HTTP 5xx/timeouts -> retryable;
- unsupported SDK placeholder -> not supported, not failure;
- duplicate-risk state-changing operations -> business/reconciliation, not blind retry.

---

## 9. Observability and transaction logging

Every infrastructure method should have a stable operation name.

Suggested constants:

```csharp
Tamin.Authenticate
Tamin.Token.Refresh
Tamin.Token.Validate
Tamin.Reference.Drug.Search
Tamin.Reference.Drug.Page
Tamin.Reference.Drug.ListAll
Tamin.Reference.Service.Search
Tamin.Reference.Service.Page
Tamin.Reference.Service.ListAll
Tamin.Reference.AllServices
Tamin.Reference.PrescriptionTypes
Tamin.Reference.ParaclinicTariffs
Tamin.Reference.DrugAmounts
Tamin.Reference.DrugInstructions
Tamin.Eligibility.PrivatePracticeLookup
Tamin.Prescription.RegisterVisit
Tamin.Prescription.RegisterDrug
Tamin.Prescription.RegisterParaclinic
Tamin.Prescription.RegisterMedicalService
Tamin.Prescription.RegisterReferral
Tamin.Prescription.RegisterPhysiotherapy
Tamin.Prescription.RegisterHospitalization
Tamin.Prescription.Fetch
Tamin.Prescription.Edit
Tamin.Prescription.Delete
Tamin.Prescription.WarningCheck
Tamin.Referral.Counts
Tamin.Referral.FindNote
Tamin.Referral.FetchCartable
Tamin.Referral.List
Tamin.Referral.NoteDetail
Tamin.Referral.PatientOpenCount
Tamin.Nurse.TodoList
Tamin.Nurse.RecordAction
Tamin.Hospitalization.SecretaryList
```

For each operation log:
- provider key;
- operation name;
- correlation ID;
- doctor ID when applicable;
- clinic ID when applicable;
- patient national code masked;
- request page/pageSize for reference data;
- item count returned;
- status;
- latency;
- error category;
- raw provider error code when available.

---

## 10. Test plan

### 10.1 Unit tests

Add tests for:
- options endpoint mapping;
- Jalali date formatter;
- session factory creates production/sandbox sessions with correct endpoint;
- credential result fields are not swapped;
- reference JSON reader:
  - root array;
  - `items`;
  - `data.items`;
  - empty array;
  - page metadata;
  - active/status interpretation.
- reference data mapper:
  - drug code/name fields;
  - service code/name fields;
  - unknown field fallback;
  - raw JSON preservation.
- all-page loops:
  - stops on empty page;
  - stops on short page;
  - stops on `has_next=false`;
  - stops when total reached;
  - fails safely on max-page guard.
- mapper for each prescription category:
  - visit;
  - drug;
  - lab/imaging/paraclinic;
  - medical service;
  - referral;
  - physiotherapy;
  - hospitalization.
- warning request mapping.
- referral/nurse/hospitalization request validation.
- error mapper for SDK-specific exceptions.

### 10.2 Integration-style tests with fake HTTP

Use fake `HttpMessageHandler` or SDK test gateway if available.

Verify actual calls:
- drug search builds expected query parameters;
- service search builds expected query parameters;
- prescription types endpoint is hit;
- paraclinic tariffs endpoint is hit;
- drug amount endpoint is hit;
- drug instruction endpoint is hit;
- warning endpoint posts expected body;
- nurse action endpoint posts expected body;
- referral count path includes patient national code and doctor ID;
- hospitalization list path includes SIAM ID and secretary national code.

### 10.3 Compile coverage tests

After adding methods to any interface, update:
- `TaminInsuranceAdapter`;
- `ResilientAdapterDecorator`;
- `NullInsuranceAdapter`;
- `SalamatInsuranceAdapter`;
- `DitasInsuranceAdapter`.

Do not leave non-Tamin adapters broken. If the new interface is Tamin-specific, the generic providers do not need changes.

### 10.4 Contract/API tests later

When public API routes are added:
- OpenAPI generation includes stable operation IDs;
- request/response DTOs are under Medical contracts;
- generated frontend client compiles;
- no provider SDK types appear in OpenAPI.

---

## 11. File-by-file task checklist

### Application

```txt
src/Modules/Medical/Application/Abstractions/Prescriptions/IInsuranceProviderAdapter.cs
```

Short term:
- keep as-is except maybe extend `ReferenceDataRequest` with optional query/page fields only if backward compatible.

Better:
- add new provider reference interface instead of bloating this file.

```txt
src/Modules/Medical/Application/Commands/Prescriptions/ReferenceDataCommands.cs
```

Add optional parameters only if sync needs them:
- `ForceRefresh`
- `PageSize`
- `MaxPages`
- `Query`

```txt
src/Modules/Medical/Application/Commands/ProviderSyncCommands.cs
```

Extend `SyncProviderCodeItem` only if domain can store metadata:
- `Description`
- `MetadataJson`
- `ProviderKind`

### Domain

```txt
src/Modules/Medical/Domain/Enums/Prescriptions/InsuranceProviderCapabilityType.cs
```

Possibly add capabilities:
- `SupportsReferralQueries`
- `SupportsNurseWorkflow`
- `SupportsHospitalizationWorkflow`
- `SupportsTokenValidation`
- `SupportsPkceAuthentication`

Do not add unless UI/orchestration needs capability gating.

### Infrastructure

```txt
src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/TaminOptions.cs
```

Add endpoint and paging options.

```txt
src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/ITaminCredentialSessionStore.cs
```

Update result fields to remove ambiguity.

```txt
src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/TaminCredentialSessionStore.cs
```

Use session factory and fix doctor national code mapping.

```txt
src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/TaminPrescriptionMapper.cs
```

Split methods and fix date/identity/metadata mapping.

```txt
src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/TaminEditedPrescriptionItemMapper.cs
```

Improve edit payloads.

```txt
src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/TaminInsuranceAdapter.cs
```

Keep canonical methods; delegate to new gateway; expand reference data and capabilities.

```txt
src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/TaminErrorMapper.cs
```

Map SDK-specific exceptions.

```txt
src/Modules/Medical/Infrastructure/Providers/Prescriptions/ResilientAdapterDecorator.cs
```

If generic interface grows, decorate every new method. Otherwise no change.

```txt
src/Modules/Medical/Infrastructure/Handlers/Prescriptions/ProviderCodeSyncHandler.cs
```

Support new code systems and all-page sync.

```txt
src/Modules/Medical/Infrastructure/Handlers/SyncProviderCodesHandler.cs
```

Fix idempotency by source locator and version.

```txt
src/Modules/Medical/Infrastructure/DependencyInjection/DependencyInjectionExtensions.cs
```

Register:
- `ITaminSessionFactory`;
- `ITaminMedicalInfrastructureGateway`;
- mappers/readers/formatters;
- named HTTP client policies if needed.

### Contracts/API/client later

```txt
src/Modules/Medical/Contracts/Requests/MedicalRequests.cs
src/Modules/Medical/Contracts/Responses/MedicalResponses.cs
src/Modules/Medical/Contracts/Routes/MedicalApiRoutes.cs
src/Modules/Medical/Api/MedicalApiEndpointMapping.cs
src/Modules/Medical/Client/IMedicalClient.cs
src/Modules/Medical/Client/MedicalClient.cs
```

Add stable public DTOs/routes only after infrastructure is complete.

---

## 12. Acceptance criteria

### Infrastructure coverage

- Every public implemented `Vorn.Tamin` method listed in Section 5 has one of:
  - a concrete Vitarish infrastructure method;
  - a documented unsupported placeholder because SDK has no operation;
  - a deliberate deferred item with reason.

### Reference data

- Can search Tamin drugs live by text/code/page/pageSize/activeOnly.
- Can search Tamin services live by type/group/text/page/pageSize/activeOnly.
- Can list all drug pages with cancellation and max-page guard.
- Can list all service pages with cancellation and max-page guard.
- Can fetch/sync:
  - drugs;
  - services;
  - all services;
  - prescription types;
  - paraclinic tariffs;
  - drug amounts;
  - drug instructions.
- Sync idempotency is scoped by provider, code system, and version/source locator.
- Raw provider payload can be recovered for support.

### Prescription flows

- Existing submit/fetch/edit/cancel flows still work.
- Visit/drug/paraclinic/medical-service mappings still work.
- Referral and physiotherapy are intentionally mapped with required metadata.
- Date fields are Jalali eight-digit strings.
- Fetch/edit/delete pass doctor national code and doctor ID correctly.

### Additional workflows

- Warning check is implemented.
- Referral queries are implemented behind infrastructure gateway.
- Nurse to-do and action recording are implemented behind infrastructure gateway.
- Hospitalization create/list are implemented behind infrastructure gateway.
- Identity/pharmacy/paraclinic placeholder surfaces remain explicitly unsupported.

### Quality

- No `Vorn.Tamin` SDK DTO appears in Medical public contracts or OpenAPI.
- No `JsonElement` leaks past Infrastructure/Application abstraction unless intentionally wrapped as raw provider response.
- All new public API operation IDs are stable when added later.
- All new operations have transaction logging, correlation IDs, error mapping, and cancellation.

---

## 13. Suggested implementation order

1. Add method coverage document and tests.
2. Add `TaminOptions.Endpoint`, page settings, and session factory.
3. Fix doctor identity and Jalali date formatting.
4. Add reference-data gateway with drug/service search and all-page listing.
5. Extend `FetchReferenceDataAsync` and provider sync for all reference code systems.
6. Fix sync idempotency by source locator.
7. Add warning check.
8. Add referral prescription mapping.
9. Add physiotherapy mapping improvements.
10. Add referral query methods.
11. Add nurse methods.
12. Add hospitalization methods.
13. Add later application/API/client methods only for UI-backed use cases.
14. Update OpenAPI and generated clients after public contracts are stable.

---

## 14. Risks and mitigations

### Risk: provider schema changes

Mitigation:
- parse leniently;
- preserve raw JSON;
- keep mapping tests with sample payloads.

### Risk: wrong doctor identity fields

Mitigation:
- separate login username, doctor ID, doctor national code, and SIAM ID;
- add credential setup validation.

### Risk: unsafe retries for commands

Mitigation:
- queries can use retry;
- commands should use outbox/idempotency/reconciliation;
- never blindly retry nurse action or prescription submission without duplicate-risk handling.

### Risk: huge provider reference data

Mitigation:
- page streaming;
- max-page guard;
- cancellation;
- batch import;
- avoid full materialization when possible.

### Risk: generic interface pollution

Mitigation:
- keep Tamin-specific methods behind `ITaminMedicalInfrastructureGateway`;
- only promote provider-neutral methods to generic abstractions.

### Risk: current Vorn.Tamin README and source disagree on some workflow status

Mitigation:
- treat source code as implementation truth;
- write integration tests against the actual SDK behavior;
- mark methods that throw `TaminWorkflowNotImplementedException` at runtime as unsupported if discovered during tests.

---

## 15. Final implementation matrix

| Area | Method group | Implement in infrastructure now | Public interface later |
|---|---|---:|---:|
| Auth/session | login/session/token validation/PKCE/signout | Yes | Selectively |
| Reference data | drug search/page/all | Yes | Yes |
| Reference data | service search/page/all | Yes | Yes |
| Reference data | prescription types | Yes | Admin/sync only |
| Reference data | paraclinic tariffs | Yes | Admin/sync only |
| Reference data | drug amounts | Yes | Admin/sync + drug UI |
| Reference data | drug instructions | Yes | Admin/sync + drug UI |
| Prescription | visit | Already, fix | Already |
| Prescription | drug | Already, fix | Already |
| Prescription | paraclinic | Already, improve | Already |
| Prescription | medical service | Already, improve | Already |
| Prescription | referral | Add | Later |
| Prescription | physiotherapy | Improve | Later |
| Prescription | fetch/edit/delete | Already, fix params | Already |
| Warning | prescription/dental rules | Add | Later |
| Eligibility | private practice | Already, fix identity | Already |
| Referral | counts/lookups/lists | Add | Later |
| Nurse | todo/action | Add | Later |
| Hospitalization | create/list | Add | Later |
| Identity | placeholder only | Unsupported | No |
| Pharmacy | placeholder only | Unsupported | No |
| Paraclinic delivery | placeholder only | Unsupported | No |
| Pricing/allowed count | not in SDK | Unsupported | No |
