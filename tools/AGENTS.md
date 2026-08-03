# Tooling agent routing

Tools that validate skill metadata, routing, bridges, or agent instructions
also follow `.agents/AGENTS.md`. The repository and scoped `AGENTS.md`
instructions own trust guidance when a tool handles secrets, credentials,
sensitive data, authorization, or untrusted external input.

Do not load product boundary skills merely because a validator scans that
boundary. Tool changes must preserve diagnostic strength and stable CI callers.
