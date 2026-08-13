# ADR-0012: Module-owned agents with guarded composition

## Status

Accepted for framework-first implementation on 2026-08-12.

## Decision

Modules and apps may publish an optional `Agents` project. `Intelligence.Agents` is a provider-neutral outward adapter over `Intelligence.Application`: profiles provide declarative inheritance for identity, instructions, model settings, limits, result schema, and capability keys; capability packs provide independently reusable model-facing tools and instructions. An app-owned Agents project composes selected module packs and owns the final agent identity and result shape. Registration is explicit through `AddAgentComposition` and supplied pack instances; no discovery or service-provider construction occurs during registration.

Profiles and packs never authorize execution, bind tenants or subjects, approve or execute mutations, choose credentials, retry, enforce idempotency or concurrency, or write audit records. Those concerns remain in the existing authenticated API, application use cases, and confirmation workflows. Capability exposure is recomputed at composition and callback time; denied capabilities contribute no model-facing surface and exposure revisions participate in conversation compatibility.

The composer uses explicit catalogs and produces a flattened runtime manifest. Provider SDKs, HTTP, persistence, infrastructure, reflection discovery, and service-provider activation remain outside Agents projects. The runtime receives only the flattened provider-neutral definition, tools, limits, and execution callback context.

Versioned conversations persist an optional lowercase SHA-256 compatibility key. It is checked before lease acquisition and provider execution. A mismatch maps to the existing HTTP 409 conflict contract; legacy null-key callers retain their existing behavior. Canonical JSON schemas and stable effective instructions are hashed so formatting-only changes do not reset a conversation.

## Consequences and boundaries

This is an explicit, time-boxed framework exception. Clinic now consumes the framework through its explicit `src/Apps/Clinic/Agents` project; the migration relocates only profiles, capability packs, and composition, while application-owned agent commands, policies, ports, approval tiers, public routes, generated clients, provider selection, dynamic plugins, and persistence ownership remain in their existing boundaries. If no additional production agent adopts the framework within the next two agent features or one major release, remove or redesign the framework.

Architecture validation checks Agents project references, forbidden packages/topology, and dynamic discovery. The project is included in the solution and default build graph, but individual modules do not receive empty Agents projects.

## Rejected alternatives

- Putting profile inheritance or capability composition in `Intelligence.Application` would make the runtime boundary know an outward adapter's policy and reuse model.
- Letting profiles carry authorization or mutation policy would allow inherited configuration to grant execution authority.
- Assembly scanning or dynamic plugin discovery would weaken Native AOT analysis and explicit ownership.
- Provider-specific profile types would leak the runtime SDK into module contracts.
