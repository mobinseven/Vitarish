# App agent routing

Applies to .NET app aggregators and app-owned backend slices. This directory is
not the React Web workspace. Keep app composition free of module business
rules. The repository and scoped `AGENTS.md` instructions own backend boundary
guidance; load a Web skill only when the change also touches `src/Hosts/Web`.

## Native AOT readiness

- **[MUST]** Apply the root Native AOT readiness rule. Keep app aggregation
  explicit or generated; isolate incompatible infrastructure and dynamic
  behavior behind its owned boundary.
