# Evaluated manual update runbook

**Profile identity:** `vitarish-product-evaluated-v1.0`

An authorized administrator or developer manually deploys a new product
version. The version is identified by the approved source commit and versioned container image references in the deployment environment. This is a release
procedure, not an in-product update service.

1. Record the authorized change, target product version, source revision, image references, operator and
   UTC start time. Confirm the target profile identity and required secret
   inputs without copying secrets into the record.
2. Run preflight checks: profile integrity, image-reference presence, required
   configuration, database backup/restore readiness, migration compatibility,
   listener allowlist, and the affected unit/integration/build checks.
3. Apply database migrations forward-only and in the owner-defined order. Do
   not remove applied migrations or run a destructive rollback.
4. Deploy the versioned images through the operator's deployment tooling. Keep
   the previous known-good deployment available for operational rollback.
5. Verify health, dependency readiness, ingress routing, TLS policy, and the
   mandatory smoke checks before declaring the deployment active.
6. Record the outcome, migration state, image identities, configuration
   revision, health result, failures and operator approval in the release
   record. If the checks fail, stop traffic and return to the previous known
   good compatible deployment under the operator's recovery procedure.

The product does not check for updates, download or install them, verify an
update file's signature or published hash, or perform code signing. Those
capabilities are outside the evaluated product boundary.
