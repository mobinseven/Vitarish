# Shared library agent routing

Shared libraries have a high reuse radius. The repository and scoped
`AGENTS.md` instructions own classification and boundary guidance. Web-host
routing rules do not apply to library-only work. Do not place app-specific
behavior here, and change generated artifacts only through their owning source
and generator.

## Shared security mechanics

- **[MUST]** A shared library may centralize transport, audit, time, integrity,
  or session mechanics only when the behavior is genuinely cross-cutting. It
  must not own module/app resource authorization, data classification,
  business invariants, provider purpose, or security-administration policy.
- **[MUST]** Expose the narrow capability required by callers. Do not leak
  persistence entities, cryptographic key material, raw forwarding headers,
  vendor clients, or host-framework state through Application-facing ports.
- **[MUST]** Security-sensitive library failure must have an explicit caller
  contract. Do not swallow, downgrade, or convert an unavailable audit,
  integrity, session, certificate-validation, or trusted-configuration result
  into success.

## Shared-hosting proof feedback

- **[MUST]** When a reusable library change affects Hosting registration,
  middleware, authentication, authorization, tenant resolution, lifecycle, or
  browser-visible data, use the real Web-to-`src/Hosts/E2e` flow before reporting
  completion.
- Inspect the authenticated screenshot from the passing run, correct any issue
  it reveals, and rerun the proof. The screenshot is an implementation
  feedback loop, not merely post hoc evidence.
