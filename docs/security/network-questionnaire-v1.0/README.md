# Retired evaluated deployment profile

This directory records the historical questionnaire-specific profile. It is no longer the server deployment reference and is not a release gate. The profile Compose/Caddy artifacts and integrity script were replaced by the current deployment references under `src/Hosts/deployment` and `src/Hosts/scripts/Test-DeploymentBundles.ps1`.

The current repository gate is `dotnet run --project tools/architecture -- security-baseline`; it validates source security surfaces and the test, production, and shared-ingress bundle topology. Questionnaire evidence and any future certification work must define a new, explicitly versioned profile before making claims about an evaluated release.
