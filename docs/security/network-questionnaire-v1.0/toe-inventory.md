# Network questionnaire v1.0 TOE inventory

**Profile identity:** `vitarish-product-evaluated-v1.0`
**Status:** Stage 0 draft; any unlisted enabled component reopens the gate.

| Component | Placement/role | Boundary | Owner and control |
| --- | --- | --- | --- |
| Caddy | Public TCP 80/443 ingress; HTTP redirects to HTTPS | TOE | Hosts; TLS, headers, methods, proxy trust |
| Main | Private application/API host | TOE | Hosts/Main; composition and startup validation |
| Web image | Browser applications used by release | TOE | Web release owner; immutable image and route inventory |
| PostgreSQL | Main persistence and Hangfire backing store | Supporting dependency | Main/owning modules; authenticated channel, migration and recovery |
| Redis | cache, data protection, session/provider state where configured | Supporting dependency | Main/Hosting; authenticated channel and failure policy |
| Object/file storage | Files bytes and verified references, only if release enables it | Supporting dependency | Files; type/size/auth/integrity/quarantine |
| External providers | Only provider clients selected in the release | Environmental dependency | Owning integration; endpoint identity, TLS and failure policy |
| Release operator | Manual deployment, backup, alert receiver, evidence custody | Environment | Release evidence; commit/digest and immutable evidence controls |

The committed Compose health check for Main remains a process-liveness check,
and Main now also owns a private readiness contract for PostgreSQL and Redis.
Caddy uses that contract for upstream health evaluation without publicly
routing the readiness path. Release evidence must still exercise dependency
readiness and recovery; a product health response is not delivered operator
evidence.

## Repository-delivered capability inventory

The release image is built from the `All` profile. When the product-evaluated
identity is enabled, the executable composes every compiled module and the
Clinic, Landing, and ProjectManagement app roots. The distinct Main-only ISO
profile remains restricted to its separately documented six-module/no-app
boundary; its evidence and claims do not apply here.

For traceability, the complete build-time module roots are Appointments,
Authentication, Commerce, Communications, Contents, Feed, Files, Financial,
Intelligence, Journey, Knowledge, Links, Medical, Parties, Platform, Requests,
Sea, Search, Social, and Users. The product-evaluated profile enables this
compiled inventory; a release that changes it reopens Stage 0.

The browser image must declare the included applications and routes. Mock
service workers, test authentication, `src/Hosts/E2e`, questionnaire PDFs/DOCX,
screenshots, scans, manifests, and evidence bundles are not TOE inputs and are
not served, embedded, logged, or used as fixtures/configuration.

## Conditional trigger register

| Trigger | Current disposition | Reopen condition |
| --- | --- | --- |
| HTTPS server | Included; HTTPS-1/2, TLSS-1/2/3, CERT-1/2/3 apply | ingress removed or changed |
| Outbound HTTPS/TLS | Triggered for every enabled provider/client | any new client/provider |
| Key generation/signing | Triggered by TLS/platform keys and any selected signing | new primitive or update signer |
| Shared security data | Not triggered: the current profile does not exchange authentication data, keys, signatures, or audit data with another secure IT product | new shared data or consumer |
| Mobile client | Not applicable | Android/iOS included |
| SSH, automatic update, mTLS | Not applicable | capability or flow enabled |
