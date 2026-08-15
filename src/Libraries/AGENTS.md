# Shared library agent routing

Shared libraries have a high reuse radius. The repository and scoped
`AGENTS.md` instructions own classification and boundary guidance. Web-host
routing rules do not apply to library-only work. Do not place app-specific
behavior here, and change generated artifacts only through their owning source
and generator.

## Shared-hosting proof feedback

- **[MUST]** When a reusable library change affects Hosting registration,
  middleware, authentication, authorization, tenant resolution, lifecycle, or
  browser-visible data, use the real Web-to-`src/Hosts/E2e` flow before reporting
  completion.
- Inspect the authenticated screenshot from the passing run, correct any issue
  it reveals, and rerun the proof. The screenshot is an implementation
  feedback loop, not merely post hoc evidence.
