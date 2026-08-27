# Network questionnaire v1.0 implementation matrix

This matrix records repository-owned proof and committed deployment artifacts;
row definitions live in [requirements-matrix.md](requirements-matrix.md).
Statuses are `implemented`, `implementation-gap`,
`deployment-evidence-pending`, or `blocked`.

**Stable-ID coverage:** AU-1, AU-2, AU-3, AU-4, AU-5, AU-6, AU-7, AU-8,
CR-1, CR-2, CR-3, CR-4, IA-1, IA-2, IA-3, IA-4, IA-5, IA-6, IA-7, IA-8,
IA-9, UD-1, UD-2, UD-3, UD-4, UD-5, UD-6, UD-7, UD-8, UD-9, UD-10, UD-11,
SM-1, SM-2, SM-3, SM-4, SM-5, SM-6, TSF-1, TSF-2, TSF-3, TSF-4, TSF-5,
TSF-6, RA-1, AC-1, AC-2, AC-3, AC-4, AC-5, AC-6, AC-7, TC-1, TC-2, TC-3,
VULN-1, VULN-2, VULN-3, VULN-4, VULN-5, VULN-6, HTTPS-1, HTTPS-2, HTTPS-3,
TLSS-1, TLSS-2, TLSS-3, TLSC-1, TLSC-2, TLSC-3, TLSC-4, CERT-1, CERT-2,
CERT-3.

| ID range | Repository artifact/proof owner | Deterministic command or check | Status at Stage 0 |
| --- | --- | --- | --- |
| AU-1–AU-8 | `src/Libraries/Audit`, owning applications, emergency journal contract | Audit tests plus static owner/integrity lint | implementation-gap |
| CR-1–CR-3 | evaluated transport and cryptography inventory, existing platform certificate path | inventory and source-policy checks | implemented |
| CR-4 / TSF-6 | not triggered; no product signing or signed-update capability | applicability check | deployment-evidence-pending |
| IA-1–IA-9, AC-1–AC-7 | Authentication/Users policy, session contracts and tests | focused Authentication/Users tests | implementation-gap |
| UD-1–UD-11, SM-1–SM-6 | owning Application policies, Files integrity, authorization matrix | endpoint/matrix validators and owning tests | implementation-gap |
| TSF-1–TSF-4 | Hosting/Main failure, time and topology contracts | static profile checks and fault tests | implementation-gap |
| TSF-5 | manual-update-runbook.md | runbook/provenance contract check | implemented |
| TC-1–TC-3, HTTPS-1–3, TLSS-1–3, TLSC-1–4, CERT-1–3 | Caddyfile, Compose, outbound client inventory | Caddy/Compose parse, source-policy and protocol checks | implemented |
| VULN-1–VULN-6 | release inventory, ingress policy, cookie/CSRF contracts | plan lint and static route/config checks | implementation-gap |
| TSF-3 | data-sharing register and compatibility tests | profile lint; exact enabled consumers | blocked |

The profile manifest, Caddyfile, topology, and configuration documents are
implemented Stage 0 artifacts. Behavior rows remain gaps until later stages;
this table must not be changed to `implemented` based on documentation alone.
