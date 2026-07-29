# Shared library agent routing

Shared libraries have a high reuse radius. Load
`platform-artifact-architecture` when adding or moving shared behavior, changing
dependency direction, or deciding whether code is truly cross-cutting.

Then load only the skill matching the touched boundary: backend use cases for
Domain/Application behavior; transport for Contracts/Client wire contracts;
data evolution for Server persistence or persisted formats; external
integration for remote-provider adapters; and trust boundary for authentication,
authorization, privacy, tenant isolation, secrets, or untrusted input.

Do not load React, Web-route, or Web-layout skills for library-only work. Do not
place app-specific behavior here, and change generated artifacts only through
their owning source and generator.
