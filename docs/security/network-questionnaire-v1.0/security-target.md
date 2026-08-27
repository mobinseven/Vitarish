# Network questionnaire v1.0 security target boundary

**Profile:** `vitarish-product-evaluated-v1.0`
**Status:** Stage 0 draft; not a certification claim.

## TOE

The TOE is the release of Caddy ingress, Main, active repository modules/apps,
and the browser application image listed by the profile manifest. PostgreSQL,
Redis, object/file storage, and enabled external providers are supporting
dependencies; their product-side protocol, authentication, authorization,
integrity, failure, and audit controls remain in scope even where the service
operator owns the infrastructure evidence.

## Security objectives

The release must preserve authentication, tenant/resource authorization,
confidentiality and integrity of user transfers, durable audit integrity,
session restrictions, safe failure/recovery, and authorized manual update
provenance. Caddy is the only public ingress. Main is never a supported public
listener. All separate-machine links require authenticated confidentiality and
integrity; same-machine exceptions require frozen placement and a
non-routable, non-remotely-configurable endpoint proof.

## Explicit exclusions

There is no product SSH interface, automatic update subsystem, mobile client,
HA/uninterrupted-service claim, mTLS claim, or evidence of certification. These
are excluded only while the trigger register remains unchanged. Operator SSH,
CA/DNS administration, backup execution, alert delivery, and incident response
are environmental controls with release-evidence handoffs, not silently omitted
product controls.

## Required assurance

The [requirements matrix](requirements-matrix.md) is the complete catalog of
mandatory and triggered conditional rows. The implementation matrix proves
repository artifacts; the deployment matrix proves release-instance behavior.
Neither matrix treats a build, private network, mock, screenshot, or the
Main-only ISO profile as proof for this profile.
