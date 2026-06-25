# ADR-003 Search boundary test plan

Date: 2026-06-25
Status: Suggested
App: Shared platform
Module: Search
Capability: Interface-to-HTTP client isolation for search and index status
Related ADRs: ADR-003
Confidence: Medium

## Objective
Verify that Search Interface components call Search Client contracts instead of `IMediator`, Application queries, Domain models, or Infrastructure.

## Required unit/contract tests
- Search client maps success, failure, cancellation, and deserialization outcomes for search requests.
- Search client maps success, failure, cancellation, and deserialization outcomes for index-status requests.
- Search request/result DTOs round-trip with representative metadata payloads.

## Required integration tests
- API maps `POST /api/modules/search` contract requests to Application `SearchQuery` and returns `SearchResultDto`.
- API maps `GET /api/modules/search/index-status` to Application `GetIndexStatusQuery` and returns status report DTOs.

## Required hot scenario
- Stale index/eventual consistency: with runtime indexing enabled, create or update indexed content, search before and after indexing catches up, and verify UI displays either current results or an intentional stale/degraded state with observable logs.
