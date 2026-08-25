# Vitarish Network Application Security Questionnaire: Guide Screenshot Collection Plan

**Status:** Separate screenshot-capture plan; execute only after the corresponding implementation checks and non-screenshot build/run/test reports are complete, and before the unsigned evidence candidate is validated and signed.
**Source:** `nsmail-1.docx` self-assessment guide and the completed Persian questionnaire.
**Companion:** [implementation plan](implementation-plan.md).
**Outcome:** Every applicable guide test has a sanitized, reproducible, release-bound screenshot and access path for the guide's image fields.

## Scope and non-goals

This plan collects only guide-facing screenshots, their access paths, and screenshot redaction. It does not collect or approve configuration, transport, database, automated-test, release-manifest, or other non-screenshot evidence; those remain in Stage 9 of the implementation plan and its authoritative evidence index.

Authenticated E2E screenshots required by the implementation plan remain engineering feedback, not evidence captured by this plan.

## Preconditions

The related implementation acceptance tests and non-screenshot build/run/test reports must already pass for the exact evaluated release/topology. The screenshot references are then added to the unsigned evidence-index candidate; `Verify-NetworkQuestionnaireEvidence.ps1` validates the complete candidate before signing. Capture only with disposable accounts, tenants, data, certificates, and fault-injection state.

Never capture credentials, secrets, private keys, raw cookies, bearer tokens, production data, patient data, or unredacted sensitive audit payloads.

## Screenshot index contribution

For each applicable `nsmail-1` test, add only these values to the main plan's evidence-index row:

| Field | Required content |
| --- | --- |
| Guide test ID | Exact guide identifier. |
| Screenshot purpose | The one visible product behavior or configured screen the image proves. |
| Access path | Browser route and required disposable role/tenant, or the evaluator-visible configuration screen. |
| Preconditions | Minimum setup needed to show the state safely. |
| Action and expected visible result | Reproducible action and the result that must be visible in the image. |
| Screenshot reference | Stable filename and SHA-256. |
| Release binding | Commit, evaluated hostname/topology, and UTC capture time. |
| Redaction review | Confirmation that prohibited material is absent. |

## Capture rules

- Capture the smallest visible state that proves the guide test. Do not use a database-browser screenshot as product-behavior evidence.
- For audit review, capture the authorized filter/sort result and a separate unauthorized-denial result where applicable.
- For capacity, failure, session, and integrity tests, capture the product’s visible notice, restriction, or safe response—not only the persisted record.
- For TLS, certificate, header, cookie, port, method, and CSRF tests, capture a browser-visible or evaluator-visible result only when useful; the authoritative scan/report remains in the main plan’s evidence index.
- Use a clean browser profile and a stable viewport. Include enough context to identify the product route/state without exposing unrelated data.
- Redact before publication. Do not crop or annotate in a way that changes the meaning of the result.

## Naming and storage

Store screenshots in an access-controlled, ignored staging location until the release operator copies the selected files into the immutable unsigned candidate bundle. The candidate and its later signed attestation are separate immutable records; never replace a captured file after it has been indexed. Use the guide ID, purpose, commit, and UTC timestamp in each filename:

```text
screenshots/
  FAU-SAR-3.1_authorized-filter-sort_<commit>_<utc>.png
  FAU-SAR-3.1_unauthorized-denial_<commit>_<utc>.png
  FAU-STG-3.1_capacity-notice_<commit>_<utc>.png
  FCS-TLSS-EXT-1.1_ingress-settings_<commit>_<utc>.png
```

## Completion gate

Screenshot collection is complete only when every applicable guide test that needs a visible image has a linked, release-bound, redaction-reviewed screenshot and reproducible access path in the main evidence index. Missing or stale screenshots block completion of the corresponding guide field; they do not replace missing non-screenshot evidence.
