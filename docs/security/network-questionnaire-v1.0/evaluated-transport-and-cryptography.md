# Evaluated transport and cryptography inventory

**Profile identity:** `vitarish-product-evaluated-v1.0`

This is the repository-owned applicability register for the selected mandatory
and triggered transport/cryptography requirements. It records implementation
enforcement; certificate issuance, exact release images, provider settings,
live placement and protocol probes remain deployment evidence. Every registered
provider link is explicitly classified as same-machine or separate-machine;
separate-machine TLS uses the native trusted-chain validation path.

## Transport selection

| Surface | Enforcement owner | Selected behavior | Rows |
| --- | --- | --- | --- |
| Caddy TLS server | `src/Hosts/caddy/Caddyfile.product-evaluated-v1.0` | TLS 1.2 only; `h1 h2`; `TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256`; P-256; no QUIC/HTTP/3 | TC-1–TC-3, HTTPS-1–2, TLSS-1–3 |
| Factory-created .NET HTTP clients | `src/Libraries/Hosting/DependencyInjection/SecureHttpClientDefaultsExtensions.cs` | TLS 1.2 only; online certificate-revocation check; native X.509 chain and RFC 6125 hostname validation; the framework's ordinary timeout remains unless a named client configures another; no custom bypass callback | HTTPS-3, TLSC-1–4, CERT-1–3 |
| Tamin Kiota clients | `src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/TaminProviderSession.cs` | TLS 1.2; revocation checking; native chain and hostname validation; 100-second timeout | HTTPS-3, TLSC-1–4, CERT-1–3 |
| SMTP client | `src/Modules/Communications/Infrastructure/Services/MessagesService.cs` | TLS 1.2; revocation checking; server identity validation; TLS is required by evaluated configuration | UD-7, HTTPS-3, TLSC-1–4, CERT-1–3 |
| Object storage SDK | `src/Modules/Files/Module/FilesModule.cs` and `MinioOptions` | HTTPS (`UseSsl=true`) for the evaluated deployment; SDK native certificate and hostname validation; no plaintext remote storage | UD-7, HTTPS-3, TLSC-1–4, CERT-1–3 |
| Elasticsearch SDK | `src/Modules/Search` and the Elasticsearch client | The native SDK transport is retained; evaluated configuration requires an HTTPS endpoint, but the repository does not replace or claim SDK-internal protocol enforcement | HTTPS-3, TLSC-1–4, CERT-1–3 |
| Provider SDKs | provider adapters and their SDK/runtime transport | Existing required OpenAI, Google Speech, DeepSeek, and provider SDK connections retain their native HTTPS/gRPC TLS and certificate-validation transport; repository configuration requires HTTPS for configurable evaluated endpoints but does not claim SDK-internal protocol enforcement | HTTPS-3, TLSC-1–4, CERT-1–3 |

Every factory client is covered by the shared registration applied by
`HostingComposition`; the explicitly-created Tamin clients and SMTP client are
configured at their owning adapters. Same-machine HTTP links are listed in
`topology.md` and are not converted to TLS solely for questionnaire coverage.
The named `AgentRuntimeHttpRunner` client retains its existing three-minute
timeout; explicit named-client timeouts are not overwritten by the shared
registration.

## Certificate validation

Certificate-based TLS connections use the platform/SDK validation path. The
connection succeeds only when the peer certificate builds an X.509 path to a
trusted CA, the chain's CA certificates satisfy `basicConstraints CA=TRUE`,
and the leaf has the applicable Server Authentication purpose. RFC 6125
reference-identity/hostname validation remains enabled. Invalid, expired,
wrong-identity, untrusted, or revoked certificates fail closed. No client
certificate, Client Authentication EKU, OCSP Signing EKU, or Code Signing EKU
is selected because the evaluated product does not use those purposes.

## Cryptographic inventory

| Algorithm | Mode/length | Owner/purpose | Questionnaire selection |
| --- | --- | --- | --- |
| AES | 256-bit CBC with authenticated HMAC protection | ASP.NET Core Data Protection payloads | CR-1 |
| HMAC-SHA-256 | 256-bit digest | agent callback signing, social webhook verification, temporary-token verification | CR-2 |
| SHA-256 | 256-bit digest | file/data integrity projections and provider/webhook digests | CR-2 |
| PBKDF2-HMAC-SHA-512 | 512-bit digest | ASP.NET Core Identity password hashing | CR-2 |
| RSA | 2048-bit minimum | existing platform certificate generation and certificate-backed protection; not a new product signing feature | CR-3 |
| TLS | `TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256`; RSA >=2048; P-256 | Caddy public TLS server | TLSS-1–3, TLSC-4 |

No product digital-signature, signed-update, key-management, SSH, or mutual
TLS capability is selected. Existing platform-managed certificate generation
is inventoried only; this stage does not add product-owned key generation or
destruction APIs.

## Time and update applicability

Security timestamps use the operating-system UTC clock through `DateTime.UtcNow`,
`DateTimeOffset.UtcNow`, and `TimeProvider.System` where the existing owner
already provides it. Elapsed-duration measurements use the monotonic
`Stopwatch`/runtime clock where required. Clock synchronization and change
protection are deployment evidence, not product-owned time services (TSF-4).

The only selected update mechanism is authorized manual deployment. The
operator runbook is [manual-update-runbook.md](manual-update-runbook.md).
Automatic checking, installation, update-file authenticity checks, code
signing, and published-hash verification are deliberately absent (TSF-5;
CR-4/TSF-6 not triggered).
