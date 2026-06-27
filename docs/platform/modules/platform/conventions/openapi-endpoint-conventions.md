# OpenAPI Endpoint Conventions

## Ownership

OpenAPI document generation and document exposure are owned by the host composition root. Modules contribute endpoint metadata only through the platform helpers in `src/Modules/Base/Core/Endpoints/`.

The main host registers `AddOpenApi()` next to `AddEndpointsApiExplorer()` and maps `/openapi/{documentName}.json` only when `IHostEnvironment.IsDevelopment()` is true. Production exposure must either remain disabled or be explicitly authorization-protected in the host.

## Approved mapping helpers

Module APIs must map endpoints through `MapProtectedModuleGroup(...)` or `MapAnonymousModuleGroup(..., justification)` and then use the OpenAPI-aware helpers:

- `MapModuleGet`
- `MapModulePost`
- `MapModulePut`
- `MapModuleDelete`
- `MapModulePatch`
- `MapModuleHead`

These helpers attach operation IDs, group/tags, success response metadata, standard error metadata, and protected security metadata.

## Profiles

Use the smallest profile that describes the transport shape:

- `ProtectedJson` for normal authenticated JSON commands and queries.
- `AnonymousWebhook` for external provider callbacks that cannot carry module bearer tokens. A non-empty justification is mandatory.
- `MultipartUpload` for file uploads. Declare multipart request metadata and success response type/status.
- `BinaryDownload` for file downloads and HEAD checks. Declare binary response metadata.
- `Streaming` for server-streamed responses. Declare the streaming content type.
- `LongRunningCommand` for accepted/background command endpoints.

## Operation IDs and tags

Operation IDs are stable PascalCase values in the shape `<Module><Action><Resource>`, for example `UsersSearchUsers`, `FilesUploadFile`, `MedicalSearchDrugs`, and `SocialIngestWebhook`. Tags and group names come from the module route group and must remain deterministic.

## Request and response schemas

OpenAPI request and response schemas must be Contract-owned serialization shapes. API files may map Domain/Application results to Contracts, but must not expose Domain entities, EF models, Infrastructure models, vendor SDK models, or UI view models as endpoint response schemas.

## Exclusions

Do not call `ExcludeFromDescription()` directly. Non-module UI, infrastructure, Razor, WebAuthn, Hangfire, or identity endpoints must use `ExcludeFromOpenApiWithJustification(...)` with a non-empty reason. Exclusion is allowed only when the endpoint is not part of the module contract surface or when a separate owner documents it.

## Common remediation

- Raw `group.MapGet(...)`: replace with `group.MapModuleGet(route, handler, "ModuleActionResource", typeof(ResponseContract))`.
- Missing operation ID: pass a stable operation ID string to the helper.
- Missing responses: pass the success type/status and use the appropriate profile.
- Anonymous endpoint without justification: map the group with `MapAnonymousModuleGroup(prefix, "reason")` and use `AnonymousWebhook` if applicable.
- File upload: use `MultipartUpload`, preserve antiforgery behavior explicitly, and document the form file field.
- File download or HEAD: use `BinaryDownload`/`MapModuleHead` and declare binary response metadata.
- Streaming: use `Streaming` and declare the stream content type.

## Validation gates

Run:

```bash
python3 tools/architecture/validate_openapi_endpoint_coverage.py
python3 tools/architecture/validate_module_api_boundaries.py
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj --filter InternalModuleApiArchitectureTests
```

The OpenAPI coverage validator reports the file, line, violation kind, endpoint expression, and remediation.
