---
name: platform-transport-contract
description: Owns HTTP/OpenAPI and WebSocket/SSE wire contracts, DTOs, routes, operation IDs, errors, protocol lifecycle, compatibility, and generated-client inputs.
---

# Platform Transport Contract

## Rules

1. Adapt Contract shapes to Application commands/queries/sessions; keep policy out of handlers.
2. Use approved route groups/helpers; extend conventions narrowly instead of bypassing validators.
3. Preserve operation IDs and public routes unless versioning explicitly approves a break.
4. Use Contract-owned serialization and stable errors/statuses; never expose Domain, persistence, UI, or vendor models.
5. For realtime, specify handshake/control order, text/binary frames, per-message/total limits, ordering, stop/cancel, terminal envelope, close behavior, and malformed input handling.
6. Define auth/security metadata; enforce trust decisions through the trust-boundary owner.
7. Change authoritative contracts/specs first and regenerate clients, DTOs, hooks, keys, and mocks; never repair generated output manually.
8. Validate endpoint coverage, boundaries, serialization, route stability, generation, typecheck, and protocol behavior.

## Stop

- Transport owns application policy or leaks implementation models.
- Realtime lifecycle/terminal behavior is incomplete.
- Generated output needs manual repair.
- A breaking public change lacks versioning/migration intent.
