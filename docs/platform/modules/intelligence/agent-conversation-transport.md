# Intelligence agent conversation transport

The authoritative public execution contract is:

`POST /api/modules/intelligence/agent-conversations`

The request identifies an agent (`agentKey`), an opaque subject scope (`subjectKind` and `subjectId`), the user message, and an optional opaque conversation ID. Continuation uses only that conversation ID and the encrypted server-side history. The response contains the run ID, conversation ID, and an agent-owned opaque structured result. Feature providers own the result schema and patient authorization; the Intelligence transport does not inspect or persist that payload.

The Clinic patient-agent provider uses `agentKey=clinic.patient` and `subjectKind=patient`. It preserves Clinic authorization, tool execution, structured parsing, and action confirmation APIs; proposed actions are kept by the shared Intelligence lifecycle ([prepared actions](prepared-actions.md)). Scope mismatches are mapped to the same not-found response as the compatibility Clinic route.

The former `POST /api/modules/clinic/patients/{patientId}/agent` operation is compatibility-only. It delegates to the same provider and is scheduled for removal in the next major API release after all supported clients have migrated. OpenAPI specifications and generated clients are derived from the backend contracts; generated files must not be edited by hand.

Conversation history is retained for 90 days from its last update. Intelligence owns a daily Hangfire cleanup job; it deletes only records older than the retention cutoff and never deletes a conversation with a live lease. Tenant, creating-user, and subject deletion workflows call the Intelligence owner-deletion port before removing the owner. Cleanup failures are logged as counts and failure categories only; prompts, ciphertext, keys, and tool payloads are never logged. Backups and Data Protection key-ring continuity remain deployment responsibilities, and rollback of the additive table can lose retained history.

## Streamed turns

`POST /api/modules/intelligence/agent-conversations/stream` (operation `IntelligenceStreamAgentConversation`) is the opt-in streamed twin of the buffered route. It takes the same `RunAgentConversationRequest`, the same protected tenant-access authorization and the same cookie antiforgery, and it resolves the same app-owned conversation endpoint, so both modes run the one conversation workflow (`RunAgentConversation`). The buffered route and its contracts are unchanged.

### Capability switch

Streaming is a per-agent capability on the route registration (`AgentConversationRoute.StreamingEnabled`) and is off for every agent unless its owning app registers it on. It is the one-agent-at-a-time enablement and rollback switch; there is no environment variable or setting. Clients learn it without a trial request from `GET /api/modules/intelligence/agent-conversations/capabilities` (`IntelligenceGetAgentConversationCapabilities`), which lists each registered agent and subject kind with `streamingEnabled`. An endpoint that registers streaming on must pass the request's observer to the workflow. Rolling back turns the flag off: future turns use the buffered route, and turns already running finish or fail without replay.

When streaming is off for the requested agent, the stream route answers `409` with `IntelligenceErrorCode.StreamingNotEnabled` before any execution. A client may choose the buffered route before it submits a turn. It must never switch routes after a turn was submitted, and an unavailable stream route, a disabled capability or a missing terminal frame is never the stale-conversation retry case.

### Stale conversations and error bodies

Both conversation routes answer every error before acceptance with an `IntelligenceError` JSON body (`application/json`), never Problem Details, and publish exactly that body for 400, 404, 409 and 502 in OpenAPI. A request that names a conversation (`conversationId` set) and ends not found answers `404` with `IntelligenceErrorCode.ConversationNotFound` (wire value 17, append-only). The code means only that the conversation is missing, expired or not visible to this caller: a conversation that does not exist and one that exists under another owner, tenant, agent or subject are deliberately indistinguishable, so the response never confirms that a conversation exists. It is returned before any execution, so no turn ran and starting a fresh conversation with the same message is safe. An unknown agent or subject route, and a not-found result for a request that names no conversation, keep `IntelligenceErrorCode.NotFound` (also `404`) and are never a stale-conversation signal. Buffered statuses are unchanged. The streamed route uses the same mapping before headers; after acceptance a failure is a terminal frame and is never replayed. Clients must not replay on any other response, including a stream that ends without a terminal frame.

### Frames

The response is `application/x-ndjson` (`ModuleEndpointOpenApiConventions.JsonStream`), one `IntelligenceStreamChunk<AgentTurnPayload>` per line, flushed per frame. Each frame carries the turn operation ID, a sequence that increases by one from zero, the kind and a typed payload:

| Kind | Payload |
| --- | --- |
| `Started` | None. Sent once, after authorization and conversation lease acquisition. |
| `Progress` | `stage` (`AgentTurnProgressStage`: `ModelRoundStarted`, `ToolStarted`, `ToolCompleted`, `ValidatingResult`, `SavingConversation`) and `ordinal`. Never carries prompt, tool or answer content. |
| `Delta` | `messageId` and displayable answer `text`: provisional text of the one answer field a run declared streamable in advance. Only a run without tools can declare one. The terminal `Completed` result replaces the preview for that message ID. |
| `Completed` | `result`: the authoritative `RunAgentConversationResponse`, sent only after the same final-result checks and persistence as buffered mode succeed. |
| `Failed` | The redacted `IntelligenceError` of the buffered mapper (`IntelligenceAgentConversationResultMapper`), with its HTTP status in `statusCode`. Never an exception message. |
| `Canceled` | None. Sent only on a connection that is still writable, when the server deadline elapsed or the server cancelled the turn. |

Exactly one of `Completed`, `Failed` or `Canceled` ends a turn on an intact connection. Errors before acceptance (validation, unknown agent, authorization or tenant denial, busy conversation, disabled capability) are standard HTTP errors before headers start. After acceptance, errors are terminal frames. The server deadline is the runtime timeout plus a fixed margin for validation and persistence, armed at acceptance.

A runtime timeout or cancel reported by the workflow (the runtime deadline, or a server-initiated cancel on an intact connection) ends the stream with `Canceled`, the same as a cancellation that escapes as an exception; any other failure ends with `Failed`. The buffered route keeps its existing failure status and error code for the same outcomes. A progress write stops with the turn token instead of waiting out the frame write timeout, and the terminal frame has its own short write allowance independent of the turn, so a deadline outcome can still be delivered. A client abort leaves nothing writable: the server cancels toward the runtime and its callbacks and releases the lease. A consumer whose write fails or exceeds the frame write timeout is treated the same way and the connection is aborted. A stream that ends without a terminal frame means the outcome is unknown: the turn may have committed, so the client must not retry it automatically or infer that nothing happened.

### Workflow rules

- Lease acquisition, history preparation, the single rejected-history retry, validation, persistence and lease release stay in the existing workflow. The retry is allowed only before any tool ran and before provisional answer text was shown; once text was shown the turn fails instead (`IAgentTurnObserver.AnswerTextEmitted`). A tool call is never replayed.
- The Main-to-runtime call uses `POST /api/internal/intelligence-agent/run/stream` with the buffered route's request body and credential. The runtime answers NDJSON lines `{"kind":"stage","stage":"ModelRoundStarted|ToolStarted|ToolCompleted","ordinal":n}` optionally `{"kind":"delta","messageId":"...","text":"..."}` lines (only when the run request declared `answerStream: { messageId, fieldPath }`, which Main sends only for a run with an empty tools list), followed by one `{"kind":"result", ...}` line holding the buffered run response. Main reads with `ResponseHeadersRead`, bounds frames (8 MiB per line, 1 KiB for a stage), applies the whole-turn deadline, validates every frame, awaits each stage report before reading on (backpressure) and owns the public sequence. An unknown or malformed frame, a frame after the result, or a stream that ends without the result is a provider failure whose outcome is unknown; it is never mapped to a history rejection and never replayed.

### Ingress

Streamed responses must reach the browser as they are written. The ingress (`src/Hosts/deployment/Caddyfile`) therefore excludes API paths ending in `/stream` from compression and leaves `flush_interval` unset on the API `reverse_proxy` for the test and production bundles: Caddy flushes responses of unknown length (the chunked NDJSON stream) immediately, whereas `flush_interval -1` is documented to keep the upstream request running after a client disconnect, which would defeat cancellation. The security-baseline check requires the plain proxy form. Verified against the pinned Caddy 2.10.0 binary: the first frame arrives within tens of milliseconds over HTTP/1.1 and HTTP/2 with `Accept-Encoding: gzip`, and a client disconnect during a silent upstream interval closes the upstream request at once. Local development and the two-host E2E suite reach Main through the Next.js rewrite in `src/Hosts/Web/scripts/api-proxy-rewrites.mjs`; Next compresses responses by default, so a Web client slice must verify (and if needed disable `compress` for those apps) that frames are not held back there. Main readiness (`/health/ready`) is the only readiness endpoint behind the ingress; there is no stream-specific readiness check, and none should make a paid model call.

### Operations-help entry

The Clinic practice assistant has a read-only, tool-disabled final-answer entry with its own agent key (`AssistantSubjects.OperationsHelpAgentKey`, subject kind `practice`). The entry is chosen by the route the client calls, never by user text. It composes no capability, sends the runtime an empty tools list and the `answerStream` declaration for its single `answer` result field, and accepts only a result with exactly that string field and no tool execution. Its delta `messageId` is the turn's `agentRunId`, so the `Completed` result replaces the preview. Its conversations are separate from the assistant's: resume with the same agent key, and delete through the dedicated route (`ClinicDeleteOperationsHelpConversation`). Streaming is disabled on every Clinic route until the rollout switch is turned on per route.

The entry's instructions are app-owned (`OperationsHelpGuide`): they contain step-by-step answers for the supported questions only (book a visit, see the schedule by day, week or month, confirm or delete a reservation, create a shift, edit or delete a shift), each written with the English and Persian names of screens and buttons that exist in the clinic Web localization, and a test checks every quoted name against it. Any other workflow question is answered as not covered, with the model saying it is not sure. The entry composes no tool, and prompt-level behaviour in Persian and English is checked offline only; answer quality needs a live evaluation.

### Darman latency benchmark

From the Web host, run the scripted check with Node 24. It exercises the benchmark client and the shared NDJSON reader in process. It requires zero provider calls or host startup.

```powershell
node scripts/agent-turn-benchmark-scripted.mjs
```

For a provider run, use the local Compose deployment with the practice streaming capabilities enabled and a disposable synthetic practice. Seed appointments, services, published shifts, and free seats. Use a test session scoped to the practice. Keep the session cookie in the process environment variable `VITARISH_AGENT_BENCHMARK_SESSION_COOKIE`.

Run `agent-benchmark-provider-gateway.mjs` in a container with no published ports. Pass a protected, persistent ledger directory as its only argument. Give only this container the test provider credential and provider egress. Give the benchmark runtime a nonsecret placeholder for its SDK credential, set `OPENAI_BASE_URL` to the gateway on the isolated test network, and set `OPENAI_AGENTS_DISABLE_TRACING=1`. Main and the runtime must use GPT-6 Luna for the benchmark. Use one ledger directory across every paid run in this benchmark. An unclean shutdown leaves a lock. Verify the old gateway is stopped before removing that lock and preserve the journal and its unresolved reservations.

Pass a local JSON file to `node scripts/agent-turn-benchmark.mjs <approved-test-config.json>`. Set `origin` to the test app origin resolving to loopback, `gatewayContainer` to the budget gateway's local Docker container name, and `syntheticPractice` to `true`. Supply `provider`, `model`, and `modelSettings` matching the local deployment. Supply `fixture.name` and the `fixture.practice` and `fixture.help` entries with their `agentKey`, `subjectKind`, and `subjectId`. Set `pairs` to the repetitions per scenario and locale and `capMicrousd` to the authorized cap enforced by the gateway. Preserve the ledger whenever the owner authorizes a higher cap.

Set `fixture.expectation` from the seed. Include `appointmentCount`, `serviceName`, and the expected `shiftAction`. Supply `countTerms` and `helpTerms` with English and Persian lists of accepted answer phrases. The runner records the check result and discards the answer body.

Set `comparisonPhase` to `baseline` for the existing behavior or `candidate` for the proposed change. The baseline records known incorrect answers and continues with fresh turns so the comparison includes the failures. The candidate stops at the first incorrect answer. Both stop on an uncertain transport outcome.

For a streamed HTTP 403 before execution, supply the prior report as `resumeReport` and the verified image digest as `mainImage`. The runner keeps completed samples and records the rejected attempt separately. Run against the same fixture, image, model, and comparison phase with a fresh authenticated session.

The runner alternates transport order and uses a fresh conversation per submission. The gateway checks the provider's free input-token count and reserves the worst-case cost before every billed request, including SDK retries. It retains the reservation if usage is uncertain. Compare baseline and candidate using the same gateway, since token counting adds a network round trip. The runner stops on failure without resubmitting. The shortcut appears in its own cohort. The report includes outcomes, percentile sample counts, and execution IDs for joining sanitized Main phase and runtime usage events. Reconcile the conservative ledger against actual provider costs. Review correctness against the seed independently of transport success. The HTTP timing measures receipt of the first nonblank answer text. Browser evidence measures paint timing. Scripted timings validate measurement behavior only. [OpenAI token counting](https://help.openai.com/en/articles/4936856-understanding-and-counting-tokens)
