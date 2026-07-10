# Platform ADR-0003 Boundary Test Plan

Date: 2026-06-28
Status: Draft
App: Main
Module: Platform
Capability: Interface/backend isolation for theme defaults
Owner: Platform module
Related journey: —
Related roadmap: —
Related ADRs: ADR-0003
Related tasks: —
Source code/files: `src/Modules/Platform/Interface`, `src/Modules/Platform/Contracts`, `src/Modules/Platform/Domain`
Confidence: Medium

## 1. Objective

Verify that the Platform Interface uses frontend-safe theme defaults from Contracts and does not reference Platform Domain for static fallback colors. Platform has the smallest scope of all modules — minimal risk but must maintain the boundary rule.

## 2. Scope

### In Scope

- Architecture boundary check: Platform Interface has no references to `Vitarish.Platform.Domain`.
- Contract defaults check: `PlatformThemeDefaults` fallback values are serialization/presentation-safe constants.
- Contract serialization: theme-related DTOs and defaults round-trip correctly.

### Out of Scope

- Platform Client/API tests (intentionally deferred until a UI workflow needs HTTP access).
- Theme persistence and database tests.
- Hot scenarios (no Platform HTTP workflow or runtime dependency was introduced).

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| `Platform.Interface` project references | May reference Domain | Frontend layer breaks isolation | Static boundary assertion |
| `PlatformThemeDefaults` | Fallback hex colors may reference Domain types | Backend leakage into presentation | Constants-only verification |
| Contract serialization | Theme DTOs | Lost fields break UI | Round-trip test |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Static boundary — Interface project references | Interface has no Domain references | `Platform.Interface.csproj` | Static analysis | No `Vitarish.Platform.Domain` reference | ADR-0003 enforcement |
| Static boundary — Interface source imports | No Domain namespace imports | `src/Modules/Platform/Interface/**/*.cs` | Static analysis | No `Vitarish.Platform.Domain` imports | ADR-0003 enforcement |
| Contract defaults — fallback values are constants | `PlatformThemeDefaults` contains only literal hex values | `PlatformThemeDefaults` class | Code inspection | All fallback values are const string hex codes | Presentation safety |
| Contract serialization — theme defaults round-trip | Theme defaults serialize correctly | `PlatformThemeDefaults` | Known hex values | Serialize → deserialize preserves values | UI contract stability |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| PlatformThemeForm fallback values match defaults | Form uses correct defaults | `PlatformThemeForm` | Compare against expected hex values |
| Contract enum stability | Theme-related enums serialize correctly | Platform Contracts enums | Prevent drift |

## 6. Required Hot Test Scenarios

No hot test is required for this slice because no Platform HTTP workflow or runtime dependency was introduced. Platform Client/API are intentionally deferred until a current UI workflow needs HTTP access.

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| Theme selection renders correctly | Theme defaults apply to UI | Running host with Blazor | Visual validation of theme application |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| Platform Client/API tests | Deferred | No UI workflow needs HTTP access yet |
| Theme persistence tests | Deferred | Database integration concern |
| Connection memory tests | Deferred | Not part of this migration slice |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Static boundary tests | §4 static rows | Regression | Required | `Platform.Tests.Boundary` | Project refs and imports |
| Contract defaults tests | §4 defaults rows | Contract | Required | `Platform.Tests.Contracts` | Fallback value verification |

## 10. Existing Coverage / Gaps

No test code exists in the working tree. All tests are created fresh based on this plan.

**Coverage targets by layer:**

| Layer | Classes under test | Planned test classes | Coverage approach |
|---|---|---|---|
| Contracts | `PlatformThemeDefaults` | 1 (ThemeDefaultsTests) | Constant verification + round-trip |
| Boundary | Project references, imports | 1 (BoundaryValidationTests) | Static analysis |

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Static boundary | Automated, every PR | `python3 tools/architecture/validate_no_blazor.py` | None |
| Contract defaults | Automated, every PR | `dotnet test tests/Modules/Platform.Tests/ --filter "Category=Contract"` | None |
| Full test suite | Automated, every PR | `dotnet test tests/Modules/Platform.Tests/` | None |

## 12. Traceability

| Test area | Linked ADR | Linked journey stage | Linked code path |
|---|---|---|---|
| Static boundary | ADR-0003 (isolation) | — | `src/Modules/Platform/Interface/Platform.Interface.csproj` |
| Contract defaults | — | — | `src/Modules/Platform/Contracts/PlatformThemeDefaults.cs` |
