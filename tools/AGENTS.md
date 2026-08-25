# Tooling agent routing

Tools that validate skill metadata, routing, bridges, or agent instructions
also follow `.agents/AGENTS.md`. The repository and scoped `AGENTS.md`
instructions own trust guidance when a tool handles secrets, credentials,
sensitive data, authorization, or untrusted external input.

Do not load product boundary skills merely because a validator scans that
boundary. Tool changes must preserve diagnostic strength and stable CI callers.

## Security and coverage validators

- **[MUST]** Derive endpoint, authentication, anonymous-justification, audit,
  transfer, generated-contract, TLS-client, or composition coverage from the
  authoritative executable mapping/configuration source. Do not infer a pass
  from filenames, namespaces, naming conventions, comments, or a partial
  project list.
- **[MUST]** A validator fails closed for a changed/discovered surface it cannot
  classify. Diagnostics identify the owning artifact and missing/invalid
  disposition with stable machine-readable output; do not silently skip parse,
  load, reflection, or project-discovery failures.
- **[MUST]** Add positive and negative fixtures for every new validator rule,
  including a regression that would have passed before the rule. Run the exact
  public command used by agents/CI and preserve its caller-compatible exit code
  and output contract.
- **[MUST]** Static validation proves only the property it checks. Do not label
  metadata presence as resource authorization, configuration presence as TLS
  behavior, or source inspection as runtime/E2E proof.
- **[MUST]** Validator execution must not mutate generated clients/specs,
  migrations, source, configuration, or retained proof artifacts. If an
  unavoidable generator is part of validation, isolate and review its output
  explicitly under the generated-output rules.
