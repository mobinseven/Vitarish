# Build-source agent routing

The repository and scoped `AGENTS.md` instructions own project topology,
dependency enforcement, and repository-wide build ownership. Load
`.agents/AGENTS.md` when build logic validates agent/skill policy.

Do not load product boundary or Web skills merely because build configuration
applies to those projects. Preserve existing callers and do not weaken analyzers
or diagnostics to make a build pass.
