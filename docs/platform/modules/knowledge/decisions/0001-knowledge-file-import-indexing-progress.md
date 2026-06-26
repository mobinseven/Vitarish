# ADR-0001: Knowledge file import, indexing, progress, and cancellation

Date: 2026-06-26
Status: Accepted
App: Platform
Module: Knowledge
Capability: Knowledge file import, indexing, evaluation progress, and cancellation
Owner: Architecture
Related apps: All frontend hosts exposing Knowledge UI
Related modules: Files, Intelligence, Search
Supersedes:
Superseded by:
Related: docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md

## Context

Knowledge supports corpus creation, source import, file-based source import, source replacement, corpus indexing, asking, diagnostics, and evaluation.

The ADR-003 migration requires `Knowledge.Interface` to stop calling Application commands/queries and backend services directly. The frontend must use `Knowledge.Client` and `Knowledge.Contracts`; protected endpoints in `Knowledge.Api` must adapt contracts to Application commands/queries.

Knowledge import and indexing touch multiple module boundaries:
- Files owns uploaded binary records and file metadata.
- Knowledge owns corpus, source, chunk, retrieval, ask, diagnostics, indexing, and evaluation state.
- Search provides retrieval/indexing infrastructure through Application abstractions.
- Intelligence provides generation/evaluation behavior through backend abstractions.

## Decision

### File transfer ownership

Knowledge will not own generic binary upload/download transport in the initial ADR-003 migration.

Files remains the owner of binary upload and file metadata. Knowledge receives file references from the UI using contract-safe identifiers and metadata snapshots, then imports content through backend orchestration.

Knowledge import endpoints accept a `FileId` and optional source metadata. They do not accept raw browser file streams in the initial migration.

Knowledge Application reads file content through a Knowledge-owned Application abstraction, implemented by Infrastructure/server composition against Files backend capabilities.

### Indexing and evaluation progress

Initial progress behavior is deferred/polled.

Knowledge import, indexing, ask, and evaluation endpoints return a completed result or structured failure for the current synchronous behavior. If a workflow becomes long-running, the first approved extension is polling through protected Knowledge status endpoints.

### Cancellation semantics

All Knowledge client methods accept `CancellationToken`.

All Knowledge API endpoints accept/propagate `CancellationToken`.

Cancellation means the caller no longer wants the operation to continue. Application handlers and infrastructure adapters must observe cancellation where possible.

Cancellation does not guarantee rollback after an operation has passed a commit point. Commands must document their commit boundary:
- import commit boundary: source/document/chunk persistence transaction
- indexing commit boundary: search index write and index-run persistence
- ask commit boundary: trace save, if `SaveTrace` is enabled
- evaluation commit boundary: evaluation run persistence, if persisted

Canceled operations return a structured `Canceled` result when cancellation is observed before successful completion.

### Tenant and owner context

Client and Interface do not invent tenant authority.

Knowledge.Api resolves tenant/owner/request context from approved host context and passes it to Application commands/queries.

Contracts include tenant or owner identifiers only when they are public filtering inputs, not trusted authority.

### Multi-module orchestration

Knowledge Application/API orchestrates workflows that combine Knowledge, Files, Search, and Intelligence.

Knowledge.Client does not depend on Files.Client or Intelligence.Client in the initial migration.

## Consequences

- Knowledge.Interface becomes frontend-safe.
- Knowledge.Contracts remains serialization-only.
- Knowledge.Api owns transport mapping and status mapping.
- Files remains source of truth for binary file upload and metadata.
- Knowledge remains source of truth for corpus/source/chunk/index/evaluation/ask state.
- Progress is simple and polling/deferred until a current need justifies realtime or streaming behavior.

## Alternatives Considered

### Knowledge uploads files directly

Rejected for initial migration because it would duplicate Files binary transfer ownership and require unresolved large upload/resumable/range/progress decisions.

### Knowledge.Client calls Files.Client before import

Rejected for initial migration because ADR-0003 prefers backend orchestration for multi-module workflows unless a current frontend orchestration use case is documented.

### SignalR or streaming progress now

Rejected for initial migration because ADR-0003 requires architecture approval before realtime or streaming progress behavior.

### Put Application input records directly into Contracts

Rejected because some Application inputs contain behavior or server-side context and Contracts must remain inert serialization shapes.

## Validation / Enforcement

- Run ADR-003 static validation after migration.
- `Knowledge.Interface.csproj` must not reference Knowledge.Application, Knowledge.Infrastructure, Knowledge.Domain, Knowledge.Api, or Files.Domain.
- `Knowledge.Interface/**/*.cs` and `Knowledge.Interface/**/*.razor` must not import Application, Domain, Infrastructure, Api, or Files.Domain namespaces.
- Every Knowledge.Client async method accepts `CancellationToken`.
- Knowledge.Api endpoints use protected endpoint mapping through `MapProtectedModuleGroup`.
- Contract serialization tests cover public DTOs/enums/routes.
- Client failure-handling tests cover all documented error categories.
- API mapping tests verify no Domain entities are returned directly.
