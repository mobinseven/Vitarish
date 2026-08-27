# Network questionnaire v1.0 topology and data flow

**Profile identity:** `vitarish-product-evaluated-v1.0`
**Status:** Stage 2 repository topology selection; live placement and release
listeners remain deployment-evidence inputs.

The Compose services in this profile are deployed on one controlled machine.
Their application network is internal, service configuration is supplied only
at deployment, and no service other than Caddy publishes a host port. These
facts are the same-machine exception; a release that places any listed service
on another machine must use the separate-machine disposition below and reopen
the profile.

```text
Browser clients (separate machine)
      | HTTPS/TLS 1.2, server-authenticated
      v
[Caddy: public TCP 80/443, TOE]
      | same machine, internal non-remotely-configurable Compose link
      +------------------> [Main: private application host, TOE]
      +------------------> [Web applications]

[Main]
      | same machine, internal non-remotely-configurable Compose link
      +------------------> [PostgreSQL]
      +------------------> [Redis]
      +------------------> [agent-runtime]
      |
      +-- separate machine, provider TLS with server identity validation -->
          SMS, SMTP, search, object storage, social, medical, AI and speech providers
```

## Link register

| Link | Placement | Role/channel | Certificate and identity behavior | Questionnaire rows |
| --- | --- | --- | --- | --- |
| Browser → Caddy | separate-machine | TLS server; TCP 443, HTTP 80 redirect only | TLS 1.2; trusted public certificate; Server Authentication; hostname validation | TC-1–TC-3, HTTPS-1–2, TLSS-1–3, CERT-1–3 |
| Caddy → Main | same-machine | HTTP on internal Compose network | Same-machine exception; no public Main listener; placement/configuration is release evidence | TSF-2, VULN-4 |
| Caddy → Web applications | same-machine | HTTP on internal Compose network | Same-machine exception; services are not host-published | TSF-2, VULN-4 |
| Main → PostgreSQL | same-machine | PostgreSQL connection | Same-machine exception; no host-published database port | TSF-2, VULN-4 |
| Main → Redis | same-machine | Redis connection with password | Same-machine exception; no host-published cache port | TSF-2, VULN-4 |
| Main → agent-runtime | same-machine | HTTP callback/runner channel with HMAC and replay protection | Same-machine exception; server-only service is not publicly routed | TSF-2, VULN-4 |
| Main → object storage | separate-machine when configured | HTTPS object transfer with access-key authentication | Native SDK X.509 path, CA, EKU and hostname validation; plaintext rejected | UD-7, HTTPS-3, TLSC-1–4, CERT-1–3 |
| Main → SMS provider | separate-machine | HTTPS API client | TLS 1.2; native trusted-chain, revocation and hostname validation | HTTPS-3, TLSC-1–4, CERT-1–3 |
| Main → SMTP provider | separate-machine | TLS SMTP client | TLS 1.2; native trusted-chain, revocation and server identity validation | UD-7, HTTPS-3, TLSC-1–4, CERT-1–3 |
| Main → search provider | separate-machine when configured | HTTPS client | TLS 1.2; shared HTTP policy and native validation | HTTPS-3, TLSC-1–4, CERT-1–3 |
| Main → social providers | separate-machine when configured | HTTPS API clients | TLS 1.2; shared HTTP policy and native validation | HTTPS-3, TLSC-1–4, CERT-1–3 |
| Main → medical providers | separate-machine when configured | HTTPS/OAuth API clients | TLS 1.2; shared HTTP policy or provider SDK native validation; no bypass | HTTPS-3, TLSC-1–4, CERT-1–3 |
| Main → AI/speech providers | separate-machine when configured | HTTPS/gRPC API clients | TLS 1.2 minimum, obsolete versions rejected by runtime/SDK, native validation | HTTPS-3, TLSC-1–4, CERT-1–3 |

The provider rows are conditional on the corresponding configured capability.
No SSH, mutual TLS, signing, or automatic-update path is part of this profile.

Private Docker networks, unpublished ports, and firewall rules are placement
controls only; they do not secure a link that is deployed across machines.

## Listener allowlist

Public: Caddy TCP 80 and TCP 443 only. Main, web applications, PostgreSQL,
Redis, agent-runtime, diagnostics, and development listeners are private or
operator-only; state services are not host-published. UDP 443/QUIC and HTTP/3
are absent while the TLS 1.2-only profile applies.
