# Build-source agent routing

Load `platform-test-validation` for shared build profiles, analyzers, generators,
or build behavior. Load `platform-artifact-architecture` when changing project
topology, dependency enforcement, or repository-wide build ownership. Load
`platform-skill-governance` only when build logic validates agent/skill policy.

Do not load product boundary or Web skills merely because build configuration
applies to those projects. Preserve existing callers and do not weaken analyzers
or diagnostics to make a build pass.
