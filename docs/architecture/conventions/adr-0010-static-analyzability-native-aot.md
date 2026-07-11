# Static Analyzability & Native AOT Engineering Standard

Date: 2026-07-11
Status: Active
Scope: Platform .NET backend implementation guidance
Owner: Architecture
Implements: [ADR-0010: Prefer Statically Analyzable and Native AOT–Compatible Design for the .NET Backend](../decisions/0010-prefer-statically-analyzable-native-aot-compatible-design.md)

## Purpose

This document contains the concrete rules, code patterns, CI validation, and review checklist that operationalize the ADR above. Unlike the ADR, this is expected to change over time — update it in place for routine refinements (new SDK, new IL warning codes, new approved packages). Anything that changes the *decision itself* (e.g. dropping the EF Core exception, abandoning the AOT-friendly default) belongs in a new ADR that supersedes the original.

Guiding principle: prefer explicit and statically analyzable designs, contain unavoidable runtime dynamism, isolate incompatible infrastructure, and continuously prove the system's actual compatibility state — don't just claim it.

---

## 1. Scope

Applies to: Minimal API hosts; application services; command and query handlers; domain code; API request/response contracts; JSON serialization; validation; dependency injection; configuration; authentication and authorization integration; background workers; messaging; logging and telemetry; object mapping; plugin or extension mechanisms; persistence boundaries; third-party package selection; code generation; build scripts; architecture tests; CI workflows.

Does not require the current production host to be published with Native AOT.

## 2. Design Principles

### 2.1 Prefer compile-time generation over runtime discovery

When equivalent solutions exist, use this preference order:

1. compile-time source generation
2. explicit registration
3. narrowly scoped and controlled reflection
4. broad runtime scanning or dynamic generation

```text
Preferred:    Source-generated JSON metadata
Acceptable:   Explicit JsonTypeInfo registration
Discouraged:  Reflection-based contract discovery over arbitrary assemblies
```

```text
Preferred:    Generated dependency registrations
Acceptable:   Explicit service registration
Discouraged:  Scanning every loaded assembly for implementations
```

Manual boilerplate is not automatically better than scanning — for large repetitive registration sets, source generation is preferred over hand-maintained lists.

### 2.2 Isolate unavoidable runtime dynamism

Runtime dynamism must be contained behind a narrow boundary; a dynamic implementation must not force unrelated layers to become dynamic.

- EF Core remains inside persistence infrastructure.
- A reflection-based third-party SDK must be wrapped behind an application-owned interface.
- Plugin discovery must occur within a dedicated extension subsystem.
- Dynamic scripting must not become a dependency of core domain logic.

### 2.3 Do not expose runtime-oriented abstractions across architectural boundaries

The following must not cross from infrastructure into application or domain boundaries without an explicitly approved exception: `IQueryable<T>`; EF Core `DbContext`; EF Core entities used as API contracts; provider-specific database types; change-tracking state; lazy-loading proxies; database transaction implementations; reflection metadata used as business data; runtime-generated proxy types; dynamic objects used as stable contracts.

Application-facing interfaces should describe business operations, use cases, or required data.

Preferred:

```csharp
public interface IPatientDetailsQuery
{
    Task<PatientDetails?> ExecuteAsync(
        PatientId patientId,
        CancellationToken cancellationToken);
}
```

Discouraged:

```csharp
public interface IRepository<TEntity>
{
    IQueryable<TEntity> Query();
}
```

### 2.4 Prefer use-case and aggregate boundaries over generic persistence abstractions

EF Core must be isolated, but not hidden behind a lowest-common-denominator generic repository merely to claim replaceability. Persistence interfaces should model the needs of the application:

```csharp
public interface IPatientStore
{
    Task AddAsync(Patient patient, CancellationToken cancellationToken);

    Task<Patient?> FindForUpdateAsync(
        PatientId patientId,
        CancellationToken cancellationToken);
}
```

```csharp
public interface IPatientSearch
{
    Task<IReadOnlyList<PatientSearchResult>> SearchAsync(
        PatientSearchCriteria criteria,
        CancellationToken cancellationToken);
}
```

A future persistence adapter must implement the required semantics explicitly — we don't assume all persistence technologies share the same query, tracking, concurrency, or transaction model.

### 2.5 Prefer explicit contracts

Prefer immutable request/response records, explicit message contracts, explicit configuration objects, explicit polymorphic registrations, typed identifiers, generated clients, known serialization contexts.

Avoid using as stable contracts: `dynamic`; arbitrary dictionaries; untyped JSON objects; reflection-discovered members; anonymous types crossing method boundaries; loosely defined metadata bags.

Flexible property bags are allowed only when flexibility is itself a business requirement.

### 2.6 Treat reflection as controlled infrastructure, not a forbidden feature

Reflection is acceptable when: the target type set is bounded; the required metadata is known; trimming behavior is understood; the code is isolated; no practical compile-time alternative exists; the benefit exceeds the complexity of replacing it; the behavior is covered by publish and runtime tests.

Reflection is discouraged as the *default* mechanism for: service discovery; endpoint discovery; validator discovery; mapper discovery; arbitrary object serialization; module initialization; business rule execution; database mapping; application command routing.

> Avoid opaque runtime discovery. Permit controlled reflection with known targets and verified preservation requirements.

### 2.7 Do not equate AOT compatibility with performance

AOT-friendly implementation is not automatically faster. Every performance claim must identify the relevant dimension: startup time; cold-start latency; memory footprint; steady-state throughput; request latency; allocation rate; deployment size; CPU usage; build time.

A change must not be justified with "better performance" unless supported by measurement or a well-understood mechanism directly relevant to the affected path. For long-running services, JIT tiering and dynamic profile-guided optimization may outperform ahead-of-time code in some workloads. The default claim here is improved predictability and static visibility, not universal speed.

## 3. API and Serialization Rules

**Use System.Text.Json source-generation contexts.** API request/response types must be registered in application-owned `JsonSerializerContext` definitions unless a documented exception applies.

```csharp
[JsonSerializable(typeof(CreatePatientRequest))]
[JsonSerializable(typeof(CreatePatientResponse))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class ApiJsonSerializerContext : JsonSerializerContext
{
}
```

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(
        0,
        ApiJsonSerializerContext.Default);
});
```

**Explicitly register polymorphism** — do not discover polymorphic subtypes by scanning assemblies at runtime.

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(EmailNotification), "email")]
[JsonDerivedType(typeof(SmsNotification), "sms")]
public abstract record NotificationRequest;
```

**Keep wire contracts independent of persistence entities.** API contracts must not depend on EF Core entities, lazy-loading proxies, navigation fix-up, or database-generated runtime types. DTO mapping must be explicit or compile-time generated. Reflection-based mapping libraries require explicit approval unless verified trimming-safe and Native AOT-compatible.

**Test serialized compatibility.** Serialization tests must exercise the configured source-generated contexts, not just unrestricted reflection-based defaults. Cover: serialization; deserialization; required properties; enum handling; polymorphic discriminators; nullable behavior; backward-compatible contract changes where applicable.

## 4. Dependency Injection Rules

- **Prefer explicit or generated registration.** Small service sets registered explicitly; large repetitive sets use source-generated registration where practical. Broad assembly scanning is discouraged.
- **Registration modules must have clear ownership:**

```csharp
public static class PatientsModule
{
    public static IServiceCollection AddPatientsModule(
        this IServiceCollection services)
    {
        services.AddScoped<CreatePatientHandler>();
        services.AddScoped<UpdatePatientHandler>();
        services.AddScoped<IPatientSearch, EfPatientSearch>();

        return services;
    }
}
```

```csharp
builder.Services
    .AddPatientsModule()
    .AddAppointmentsModule()
    .AddMessagingModule();
```

- **Avoid registration by naming convention** (`*Service`, `*Handler`, `*Repository`, `*Validator`) — names are not reliable architectural contracts.
- **Prevent hidden service-locator patterns.** Do not replace explicit dependencies with arbitrary `IServiceProvider` resolution, runtime type-based resolution, string-keyed lookup, or reflection-created handler instances. Keyed services are acceptable when the key set is explicit and belongs to the application model.

## 5. Endpoint Design Rules

**Use explicit endpoint modules:**

```csharp
public static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/patients");

        group.MapGet("/{id:guid}", GetPatientAsync);
        group.MapPost("/", CreatePatientAsync);

        return endpoints;
    }
}
```

Endpoint mapping must not depend on scanning assemblies for methods, attributes, or marker interfaces unless a documented exception is approved.

Endpoints should handle transport-level binding, authentication/authorization metadata, transport validation, use-case invocation, and HTTP result mapping — not persistence queries, runtime type discovery, or infrastructure orchestration.

## 6. Validation Rules

- Validators must be explicitly registered or generated; runtime scanning for all validator types is discouraged.
- Keep validation categories separate: transport validation; application input validation; domain invariants; persistence constraints. AOT preparation must not collapse these into one generic mechanism.
- Architecture tests should be able to determine which requests require validators, whether each validator is registered, whether validators depend on infrastructure, and whether domain invariants remain inside domain code.
- Validation scripts should flag: unregistered request validators; reflection-based validator scanning; validators defined in the wrong layer; validators depending directly on `DbContext`; endpoint request types missing from JSON source-generation contexts; application requests exposed without required validation.

## 7. Configuration Rules

- Use strongly typed options; prefer generated or explicit binding; avoid runtime binding to arbitrary object graphs.
- Validate configuration at startup:

```csharp
builder.Services
    .AddOptions<MessagingOptions>()
    .BindConfiguration(MessagingOptions.SectionName);
// Register the source-generated validator
builder.Services.AddSingleton<IValidateOptions<MessagingOptions>, MessagingOptionsValidator>();
```

- Configuration keys should be owned by their corresponding module and exposed through constants or typed registration methods.

## 8. Logging and Observability Rules

**Prefer source-generated logging** for frequent or performance-sensitive logs:

```csharp
internal static partial class PatientLogs
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Created patient {PatientId}")]
    internal static partial void PatientCreated(
        ILogger logger,
        Guid patientId);
}
```

**Audit third-party observability packages** for reflection use, runtime proxy generation, trimming warnings, dynamic assembly loading, Native AOT support statements, and fallback behavior under trimming. Observability must not become an untracked blocker.

## 9. Mapping Rules

Preference order:

1. direct projection in the owning use case
2. hand-written mapping for small or behavior-rich mappings
3. compile-time-generated mapping
4. reflection-based runtime mapping only by exception

Mapping must not hide database access, lazy loading, or application behavior. Project directly into response models where practical:

```csharp
var result = await db.Patients
    .Where(patient => patient.Id == patientId)
    .Select(patient => new PatientDetails(
        patient.Id,
        patient.FirstName,
        patient.LastName))
    .SingleOrDefaultAsync(cancellationToken);
```

The projection may remain inside the EF Core infrastructure implementation while its returned contract belongs to the application boundary.

## 10. Persistence Boundary Rules

- **EF Core is an accepted blocker** for the current production architecture. This exception does not apply automatically to other libraries.
- **Keep EF Core references localized** to persistence infrastructure projects, persistence-specific migrations projects, integration tests, and host composition where registration is unavoidable. Domain and application projects must not reference EF Core packages.
- **Do not return `IQueryable` outside infrastructure.** All queries execute within the persistence implementation; filtering, pagination, sorting, and projection are represented through explicit criteria or use-case-specific methods.
- **Preserve semantic boundaries.** A future adapter replacement must account for change tracking, optimistic concurrency, transactions, batching, identity map behavior, relationship handling, value conversion, query filters, migrations, retry behavior, and provider-specific semantics. Don't claim an interface makes these automatically portable.
- **Allow host-specific persistence decisions.** Not every host must use EF Core. Narrow workers, webhook receivers, gateways, or specialized APIs may use a different adapter where the operational benefit is material, data requirements are narrow, ownership is clear, duplicated persistence behavior is avoided, and consistency requirements are understood.

## 11. Third-Party Dependency Policy

Every new runtime dependency must be evaluated for: trimming compatibility; Native AOT compatibility; use of dynamic assembly generation; runtime proxy generation; reflection breadth; source-generator availability; maintenance status; transitive dependencies; availability of a non-dynamic mode; behavior under published deployment.

A package need not be rejected solely because it isn't Native AOT-compatible — but an incompatible package must satisfy at least one of:

- required for a core business capability
- no reasonable compatible alternative exists
- isolated behind an application-owned boundary
- incompatibility is documented
- removal path is understood
- runtime and deployment behavior is tested

Convenience alone is insufficient justification for introducing a broad new blocker.

## 12. Build and CI Validation

**Standard build:** compiles all production projects and tests.

**Trimming analysis:** at least one CI job publishes the applicable host or compatibility probe with trimming enabled:

```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>full</TrimMode>
</PropertyGroup>
```

Trimming warnings must be reviewed, categorized, fixed where practical, explicitly suppressed only with justification, and associated with an owner. Suppressions must not be added merely to make CI green.

**Native AOT compatibility probe:** the repo must contain a probe referencing as much production code as practical while substituting/excluding EF Core. It should exercise: host startup; dependency registration; endpoint mapping; JSON serialization; validation; configuration; authentication primitives where practical; application handlers; messaging contracts; logging; non-EF infrastructure.

```bash
dotnet publish \
  src/Architecture/AotCompatibilityProbe/AotCompatibilityProbe.csproj \
  --configuration Release \
  --runtime linux-x64 \
  -p:PublishAot=true
```

A successful compile alone is insufficient — CI should run the produced executable and smoke-test representative endpoints.

**Warning policy:** AOT/trimming warnings should fail the compatibility job unless explicitly baselined. Relevant families (evolves with the SDK):

```text
IL2026  IL2046  IL2050  IL2055  IL2060
IL2062  IL2065  IL2067  IL2070  IL2072
IL2075  IL2080  IL2091  IL3050  IL3053
```

Centralize the warning policy rather than duplicate it across project files.

**Dependency audit:** CI should produce/validate a list of packages used by the backend and flag: newly added packages; newly introduced reflection-heavy libraries; packages that emit trimming warnings; packages added directly to application or domain layers; runtime code-generation packages; duplicate libraries serving the same responsibility.

**Architecture tests** should verify at least: domain/application projects don't reference EF Core; application projects don't reference ASP.NET Core; API contracts are registered in JSON contexts; endpoints are explicitly mapped; forbidden assembly-scanning calls are absent outside approved locations; `IQueryable<T>` doesn't cross infrastructure boundaries; `DbContext` isn't injected into endpoints or application handlers; runtime mapper packages aren't introduced without approval; dynamic assembly generation is confined to approved projects; compatibility suppressions include justification.

## 13. Repository Validation Scripts

Single entry point for local and CI validation:

```text
scripts/
  validate-architecture.ps1
  validate-aot-compatibility.ps1
  validate-json-contexts.ps1
  validate-dependencies.ps1
  validate-all.ps1
```

(Equivalent shell scripts for non-Windows environments.)

| Script | Responsibilities |
|---|---|
| `validate-architecture` | Run architecture tests; inspect forbidden project references; detect leaked persistence types; detect `IQueryable<T>` in public application APIs; detect endpoint-level `DbContext` usage; detect disallowed runtime scanning outside exception paths. |
| `validate-json-contexts` | Identify endpoint request/response types; verify registration in an approved `JsonSerializerContext`; detect unrestricted serialization calls in application-owned API code; report unregistered polymorphic types; verify contexts compile. Prefer Roslyn tooling / semantic analysis over text matching where practical. |
| `validate-dependencies` | Compare package references against the approved baseline; identify new direct dependencies; identify packages associated with runtime code generation; identify dependency references in incorrect layers; produce a reviewable compatibility report. |
| `validate-aot-compatibility` | Publish the compatibility probe; treat unapproved warnings as errors; run the native executable; execute smoke tests; record binary size and startup metrics for trend analysis; distinguish accepted-blocker failures from regressions elsewhere. |
| `validate-all` | Run all validation; provide one local command matching CI behavior; return non-zero exit code on policy violations; produce actionable messages with file paths and remediation guidance. |

## 14. Static Analysis and Enforcement Strategy

Use the strongest practical mechanism, in order:

1. compiler errors
2. source-generator diagnostics
3. Roslyn analyzers
4. architecture tests
5. MSBuild validation
6. semantic repository scripts
7. text-based checks (simple patterns only)

Text searches alone are not sufficient enforcement for complex architectural rules. Consider custom analyzers for recurring violations such as: exposing `IQueryable<T>` from non-infrastructure assemblies; injecting `DbContext` into endpoints; serializing unregistered API contract types; calling broad assembly-scanning methods outside approved modules; using `dynamic` in public application contracts; adding reflection-based mapping attributes; using unrestricted `JsonSerializer` APIs in API code.

## 15. Exception Registry & Process

Exceptions to this standard are allowed but must be documented, narrow, and owned. Each exception must record: the specific incompatible mechanism; why it's required; affected projects; why alternatives were rejected; runtime impact; trimming/AOT impact; how the behavior is tested; how the incompatibility is isolated; the owner; the review trigger; the potential removal path.

A statement like "reflection is required by the framework" is not sufficient on its own.

### Active exceptions

**Exception: EF Core persistence implementation**

| Field | Value |
|---|---|
| Scope | `Infrastructure.Persistence.EfCore` and migration tooling |
| Reason | Required persistence capabilities and current production maturity |
| Impact | Prevents complete Native AOT publication of the primary API host |
| Containment | No EF Core references in domain or application projects |
| Validation | All non-persistence code remains included in the Native AOT probe |
| Owner | Architecture |
| Review trigger | Each EF Core major release and any persistence architecture revision |

## 16. Decision Framework for Difficult Design Choices

When choosing between competing designs, evaluate in this order:

1. **Correctness** — does the design preserve business rules, data semantics, security, concurrency behavior, and failure handling?
2. **Architectural ownership** — does each responsibility remain in its owning layer or module?
3. **Simplicity** — is the solution understandable and maintainable without hidden behavior?
4. **Static visibility** — can important behavior be verified at build time?
5. **Runtime predictability** — does the design avoid unnecessary environment-dependent discovery or generated behavior?
6. **Replaceability** — is an incompatible dependency isolated behind a meaningful boundary?
7. **Performance** — is the performance concern measured and relevant?
8. **Native AOT compatibility** — does the solution preserve or improve future AOT feasibility without compromising the above?

Native AOT compatibility must not override correctness, domain integrity, or maintainability.

## 17. Worked Examples

| Topic | Decision | Reject | Reason |
|---|---|---|---|
| Dependency registration | Explicit or generated module registration | Broad assembly scanning across all loaded assemblies | Registration should be inspectable and deterministic |
| JSON contracts | Source-generated contexts | Arbitrary reflection-based serialization | Contracts are known at compile time and belong to the API boundary |
| Object mapping | Direct projection, manual mapping, or generated mapping | A runtime mapper adopted solely to reduce a small amount of code | Mapping logic should stay visible and trimming-safe |
| Persistence abstraction | Use-case-specific queries and aggregate-specific stores | Returning `IQueryable<T>` through a generic repository | The application must not depend on EF Core query semantics |
| Validation registration | Explicit or static validator registration | Scanning every assembly for validator implementations at startup | Validation coverage should be deterministically testable |
| Plugin systems | Dynamic loading only within an explicitly designed plugin boundary | Letting normal application composition behave like a plugin system | Dynamic extensibility is valuable only where it's an actual requirement |
| Performance-sensitive code | Benchmark both implementations | Choosing the AOT-friendly implementation because it's assumed faster | Static compatibility and runtime performance are separate properties |

## 18. Rollout Plan

**Phase 1 — Establish visibility:** add the compatibility exception registry (§15); identify direct/transitive backend dependencies; document known trimming/AOT blockers; add an architecture test preventing EF Core references in application/domain projects.

**Phase 2 — Stabilize API contracts:** add source-generated JSON contexts; register all endpoint request/response contracts; add serialization compatibility tests; remove persistence entities from API contracts.

**Phase 3 — Make composition explicit:** replace broad endpoint scanning; replace broad service scanning where practical; introduce explicit module registration; generate repetitive registrations rather than maintaining large manual lists.

**Phase 4 — Add compatibility validation:** add trimming publish validation; add the Native AOT compatibility probe; run the produced native executable in CI; add representative endpoint smoke tests.

**Phase 5 — Strengthen static enforcement:** add Roslyn analyzers for recurring violations; add dependency baseline checks; add validation coverage checks; add JSON context coverage checks.

**Phase 6 — Reduce remaining blockers:** review third-party packages; isolate incompatible libraries; replace low-value dynamic dependencies; reassess EF Core and alternative persistence adapters when justified.

## 19. Review Checklist

Use during architecture and code review.

**Contracts** — Are API and message contracts explicit? Registered in source-generated serialization contexts? Is polymorphism explicitly declared? Are persistence entities absent from external contracts?

**Composition** — Is service registration explicit or generated? Is endpoint mapping explicit? Is runtime scanning narrowly scoped? Is `IServiceProvider` being used as a service locator?

**Reflection and generation** — Is reflection necessary? Is its target set bounded? Is trimming behavior known? Could source generation replace it? Is the behavior tested in the compatibility probe?

**Persistence** — Does EF Core remain inside infrastructure? Does any application API expose `IQueryable<T>`? Is `DbContext` injected outside persistence infrastructure? Does the abstraction represent a use case or merely generic CRUD? Are required data semantics documented?

**Dependencies** — Is the package compatible with trimming and Native AOT? Does it generate runtime proxies or assemblies? Is there a generated or static mode? Is the dependency isolated? Has it been added to the compatibility baseline?

**Performance** — What performance dimension is being improved? Is there benchmark or profiling evidence? Is startup performance being confused with throughput? Does added complexity justify the measured gain?

**Validation** — Will CI detect regression? Is the production code included in the AOT probe? Are warnings treated as actionable? Is any suppression justified and owned?

## 20. Common Failure Modes to Watch For

These are the ways this standard tends to erode in practice — worth revisiting periodically:

- The team turns this into an informal "zero reflection" rule, beyond what §2.6 actually asks for.
- Manual registration creeps into unnecessary boilerplate instead of using source generation where it's warranted.
- AOT compatibility gets informally cited as a performance win without measurement (see §2.7).
- Compatibility scripts lean too heavily on text matching instead of semantic analysis (see §13, §14).
- The AOT probe drifts away from actual production composition and stops being representative.
- Persistence abstractions become too generic in the name of "replaceability" and weaken the domain model (see §2.4).
- Warning suppressions accumulate without an owner (see §12).

These risks are controlled through review, automation, and the exception process — not by adding more rules.
