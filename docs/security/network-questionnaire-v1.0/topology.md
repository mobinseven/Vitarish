# Network questionnaire v1.0 topology and data flow

**Profile identity:** `vitarish-product-evaluated-v1.0`
**Status:** Stage 0 draft; exact host placement and release listeners remain
release-bound inputs.

```text
Browser clients
      | HTTPS/TLS 1.2, authenticated session, CSRF, tenant context
      v
[Caddy: public TCP 80/443, TOE]
      | authenticated encrypted channel when separate machine;
      | otherwise loopback/non-routable and non-remotely-configurable proof
      v
[Main: private application host, TOE]
      | authenticated confidentiality + integrity when separate machine
      +------------------> [PostgreSQL: supporting dependency]
      +------------------> [Redis: supporting dependency]
      +------------------> [Object/file storage: supporting dependency]
      +------------------> [Enabled external provider: environment]
      +------------------> [Independent audit/data witness: environment]
```

## Link register

| Link | Data | Stage 0 disposition | Required proof |
| --- | --- | --- | --- |
| Browser → Caddy | credentials, cookies, user data | TLS 1.2; plaintext auth rejected | Caddy config and release probes |
| Caddy → Main | authenticated requests and responses | OPEN placement; encrypted if separate machine | topology plus certificate/identity probe |
| Main → PostgreSQL | security, tenant, audit and product data | encrypted/authenticated if separate machine | connection config and negative identity probe |
| Main → Redis | session, cache, data-protection material | encrypted/authenticated if separate machine | connection config and negative identity probe |
| Main → storage | file bytes and metadata | encrypted/authenticated if separate machine | endpoint identity and transfer probe |
| Main → providers | selected user/security data | provider-specific authenticated TLS | client inventory, hostname/certificate tests |
| Main → witness | checkpoint payloads only | authenticated channel and append-only identity | witness protocol and reconciliation probe |

Private Docker networks, unpublished ports, and firewall rules are placement
controls only; they do not close a separate-machine secure-channel row.

## Listener allowlist

Public: Caddy TCP 80 and TCP 443 only. Main, PostgreSQL, Redis, storage,
witness, diagnostics, and development listeners are private or operator-only;
state services are not host-published. UDP 443/QUIC is absent while TLS 1.2-only
applies. Exact private and health listeners are recorded by the committed
compose manifest and release port report.
