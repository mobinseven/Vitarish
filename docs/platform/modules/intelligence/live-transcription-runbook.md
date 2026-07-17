# Live Transcription Runbook

Status: Active<br>
Owner: Platform module: intelligence<br>
Last reviewed: 2026-07-17

## Scope

This runbook covers the clinic procedure-note dictation path backed by Google Cloud Speech-to-Text V2. The browser records WebM/Opus, the authenticated Intelligence WebSocket streams bounded audio to Google using `chirp_2`, and confirmed segments are appended to the current note draft. Audio and transcripts are request-scoped: the transcription path does not persist them, and the note is saved only through the existing Send action.

## Enablement

1. Enable the Google Cloud Speech-to-Text API in the configured project and grant the Main host's service identity only the permissions needed to call Speech-to-Text V2.
2. Set `Vitarish__Intelligence__Google__Speech__ProjectId` and `Vitarish__Intelligence__Google__Speech__Location`. The supported deployment location is `us-central1` for the configured `fa-IR` and `en-US` languages and `chirp_2` model.
3. Provide Application Default Credentials to the host. Prefer the workload identity supplied by the deployment platform. If a credential file is unavoidable, set `GOOGLE_APPLICATION_CREDENTIALS` or `Vitarish__Intelligence__Google__Speech__CredentialsPath` to a mounted secret path; never commit the credential file.
4. Set `Vitarish__Intelligence__LiveTranscription__Enabled=true` and retain bounded limits appropriate for the deployment. Defaults are documented in `src/Hosts/Main/appsettings.Template.json` and `.env`.
5. Add every exact web origin to `Cors__AllowedOrigins`. WebSocket upgrades reject a missing, wildcard, malformed, or unlisted `Origin` before accepting the connection.
6. Serve both the web application and API over HTTPS outside localhost. Dictation requires a browser secure context, microphone permission, `MediaRecorder`, WebSocket support, and a supported WebM MIME type.

The protected capability endpoint is `GET /api/modules/intelligence/transcription/live/capabilities`. It reports `available: false` when the feature is disabled or Google configuration is unusable. The client must not attempt a WebSocket connection in that state.

## Privacy and security invariants

- The capability endpoint and WebSocket require the host's authenticated API policy. The clinic UI exposes dictation only to users who can write the procedure event.
- The WebSocket requires the `vitarish.live-transcription.v1` subprotocol and an exact configured origin.
- Do not add audio bytes, transcript text, credential paths, provider payloads, or note contents to logs, metrics, traces, or exception messages.
- Only final provider segments reach the note draft. Partial hypotheses are ignored.
- Closing the panel, changing procedure scope, navigating away, cancellation, failure, and terminal completion must stop microphone tracks, the recorder, timers, provider work, and the socket.
- A failed dictation retains text already confirmed in the note. It never triggers the note mutation endpoint automatically.

## Limits and failure behavior

The server validates language and MIME type before starting provider work. It bounds each browser frame, total session bytes, duration, idle time, provider time, and the in-memory audio channel. The Google adapter further splits audio into requests of at most 15,000 bytes. Provider calls are not retried because replaying a live stream would duplicate or reorder speech.

Each accepted session emits one `started` message, zero or more ordered final `segment` messages, and exactly one terminal `completed`, `failed`, or `canceled` message while the connection remains writable. Stable failure codes distinguish protocol, limit, audio, cancellation, timeout, and provider availability failures; user-facing copy remains localized in the clinic package.

## Diagnosis

| Symptom | Checks | Action |
| --- | --- | --- |
| Microphone control is unavailable | Call the capability endpoint while authenticated; check `Enabled`, non-placeholder project/location, and whether a configured credential path exists. | Correct configuration or credentials, then restart the host. Do not bypass capability gating. |
| Browser reports an insecure or unsupported environment | Check HTTPS, microphone permission, `navigator.mediaDevices`, `MediaRecorder`, WebM/Opus support, and WebSocket support in current Chrome or Edge. | Restore HTTPS/permission/browser support; use manual note entry meanwhile. |
| Upgrade returns 401 or 403 | Confirm the browser is authenticated to the API and its exact scheme/host/port appears in `Cors__AllowedOrigins`. | Fix authentication or the exact origin allowlist. Never use a wildcard origin. |
| `ProviderUnavailable` | Confirm the API is enabled, project/location are correct, ADC is visible to the host, the credential file exists when configured, IAM permits Speech V2, and Google is reachable. | Repair the provider boundary. Keep manual note entry available. |
| `ProviderTimeout`, `IdleTimeout`, or a duration/payload failure | Compare the session with the configured timeout and byte limits; verify the browser is sending non-empty audio. | Retry as a new manual session after correcting connectivity or use shorter dictation. Do not add transport retries. |
| `InvalidAudio` or `UnsupportedMediaType` | Confirm the negotiated MIME type is one returned by capabilities and the browser still supports it. | Use a supported current Chrome/Edge build or manual note entry. |
| `ProtocolViolation` | Check that the web client and API were deployed from compatible versions and use the required subprotocol. | Roll deployments to a compatible pair; do not relax sequence or session validation. |

Operational telemetry must remain content-free. Use stable failure code, lifecycle stage, duration, and byte counts only; redact all provider details that could contain sensitive content or credentials.

## HTTPS browser smoke test

Run this after configuration, deployment, or browser changes:

1. Sign in as a clinic user who can edit a patient procedure event and open the event-items panel in current Chrome or Edge over HTTPS.
2. Confirm the microphone control is enabled and Persian and English language choices are present.
3. Start dictation, grant microphone permission, speak a short phrase, and verify the UI moves through connecting and recording.
4. Continue typing in the note while recording, stop dictation, and verify final segments append to the latest typed draft without replacing it.
5. Verify Send is disabled while connecting, recording, and completing, then becomes available after completion.
6. Send once and verify the existing note-save behavior persists the combined draft; start another session to confirm resources were released.
7. Repeat once with permission denied and once by navigating away mid-session. Confirm localized failure/fallback behavior, retained confirmed text, no automatic save, and no continuing browser microphone indicator.
8. Review content-free telemetry and verify no audio or transcript text was recorded.

## Disable and rollback

Set `Vitarish__Intelligence__LiveTranscription__Enabled=false` and restart the host. The capability endpoint then reports unavailable and the clinic falls back to manual note entry. No database schema, persisted format, migration, backfill, or data rollback is involved. Reverting the code requires the API and clinic assets to move together because the WebSocket subprotocol is a compatibility boundary.
