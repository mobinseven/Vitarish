# ADR-0011: Run Clinic agents through a server-side OpenAI Agents SDK runtime

## Status

Accepted for implementation.

## Decision

The versioned public conversation execution route is owned by Intelligence at `/api/modules/intelligence/agent-conversations`. Clinic remains the registered agent workflow provider and remains authoritative for patient scope, enabled capabilities, tool schemas, tool authorization, proposal creation, and approval workflows. The former Clinic run route remains as a compatibility shim for one release window and must be removed after all callers migrate to the Intelligence route. The agent orchestration loop runs in the server-only `src/Hosts/Web/apps/agent-runtime` Next.js service using the official OpenAI Agents SDK for JavaScript/TypeScript.

The .NET Intelligence boundary sends a provider-neutral agent definition, input, limits, execution context, and Clinic-owned tool definitions to the runtime. SDK tools call back to the Clinic internal tool endpoint. The callback is outside the public OpenAPI surface and accepts only a short-lived HMAC credential bound to the execution id and canonical execution metadata. Browser code, API keys, and provider SDKs are not part of the Clinic Web UI or shared React packages.

## Consequences

- `clinic-web` remains responsible for the SSR/client UI surface and API hooks; it does not own agent orchestration or credentials.
- New browser callers use the Intelligence-owned route; Clinic's route is compatibility-only and has no independent execution behavior.
- `intelligence-web` remains responsible for browser realtime dictation and generic intelligence UI; it does not become a server integration package.
- Clinic tools remain close to their Clinic use cases and retain the existing authorization, proposal, confirmation, and rejection behavior.
- The runtime is a separately deployable service and requires the OpenAI API key plus the same configured shared secret as Main. Missing or invalid configuration fails closed.
- The .NET-to-runtime and runtime-to-Clinic callback payloads are internal compatibility boundaries and must be changed additively or versioned.
- The runtime must enforce the limits supplied by the Clinic application and must not log prompts, tool arguments, credentials, or clinical payloads.

## Rejected alternatives

- Embedding the official SDK in `clinic-web` or `intelligence-web`: those are UI package boundaries and would mix browser-facing ownership with server credentials and patient-agent authorization.
- Keeping the custom .NET Responses loop: it would not use the official Agents SDK and would leave provider orchestration in the wrong runtime for the selected SDK.
- Moving Clinic tools into the runtime: this would duplicate authorization and business-use-case ownership and make the callback boundary less trustworthy.

## Compatibility and removal

`POST /api/modules/clinic/patients/{patientId}/agent` remains available only for the compatibility window. It delegates to the same Clinic provider used by Intelligence and is removed when all supported Web clients use `POST /api/modules/intelligence/agent-conversations` (target release: the next major API release after the Intelligence route has shipped). The generated Clinic client operation is retained until that release, but new code must not add dependencies on it.
