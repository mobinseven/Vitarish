# Tooling agent routing

Load `platform-test-validation` for validators, test runners, generators, and
build checks under this directory. Load `platform-skill-governance` when a tool
validates skill metadata, routing, bridges, or agent instructions. Load
`platform-trust-boundary` only when a tool itself handles secrets, credentials,
sensitive data, authorization, or untrusted external input.

Do not load product boundary skills merely because a validator scans that
boundary. Tool changes must preserve diagnostic strength and stable CI callers.
