---
name: platform-transport-contract
description: Governs HTTP/OpenAPI and realtime WebSocket or SSE boundaries, contract DTOs, endpoint conventions, protocol frames, compatibility, error envelopes, and generated clients. Use when adding or changing public/module transport or generated-client inputs. Do not use for UI-only consumption of an unchanged contract.
---

# Platform Transport Contract

## Purpose

Keep transport thin, explicit, compatible, and generated from one authoritative contract source. This skill owns wire shape and transport lifecycle, not business rules or user-facing copy.

## Use when

- HTTP endpoints, OpenAPI metadata, operation IDs, request/response DTOs, errors, auth metadata, WebSocket/SSE routes, control frames, binary frames, or generated clients change.
- A native transport needs a new repository convention or endpoint profile.

## Do not use

- A React feature consumes an unchanged wrapper/hook, or backend behavior changes behind an unchanged contract.

## Procedure

1. Confirm transport adapts Contract shapes to an Application command/query/session; keep business rules out of handlers.
2. Use approved module route groups and endpoint helpers. Extend conventions narrowly instead of bypassing validators.
3. Keep operation IDs and existing public routes stable unless a versioning decision approves a break.
4. Define contract-owned serialization shapes and stable error/status codes; never expose Domain, persistence, UI, or vendor SDK types.
5. For realtime protocols, specify handshake/control order, text versus binary frames, message size, total payload, sequence/order, stop/cancel, terminal envelopes, close behavior, and malformed input handling.
6. Document authentication and security metadata, then hand enforcement details to `platform-trust-boundary`.
7. Change authoritative specs/contracts first; regenerate clients, DTOs, hooks, keys, and mocks. Never edit generated output manually.
8. Run endpoint coverage, module boundary, serialization, route stability, generation, typecheck, and relevant transport tests.

## Hard stops

- Transport handlers own application policy or expose implementation models.
- Realtime lifecycle or terminal behavior is underspecified.
- Generated files require manual repair.
- A breaking public change lacks versioning or migration intent.

## Handoff

Route use-case behavior to `platform-backend-use-case`, auth/privacy to `platform-trust-boundary`, generated-client consumption to `platform-react-feature`, and validation to `platform-test-validation`.
