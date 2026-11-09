# Central authentication checkpoint handoff

Current status: **deployed**. PR #1005 delivered central authentication; PR
#1006 fixed the production evidence collector's redirect race. Production
[run 42481734974](https://github.com/mobinseven/Vitarish/actions/runs/42481734974)
passed for `ebe2ed62beec67448faf4e9524b06181c7bb46b5`, including deployment and
public browser evidence. The full active-app two-host proof passed 72 tests
with 15 skips, including the owner-approved Knowledge exclusion. Authenticated
production sign-in was not exercised. The unrelated landing changes were
restored to the worktree; the original stash remains as a backup. Automated
approval rejected deletion of abandoned local build directories as blocked by
policy, so they remain.

The remainder is the historical checkpoint, retained for context and no longer
describes current deployment or branch state.

## Historical checkpoint

Status: **paused by the owner; not deployed**. Recorded 2026-11-08 at 10:32 UTC.
Do not resume implementation, tests, merge or deployment without an explicit
resume. Drafting this handoff and stopping owned processes were authorized.

## Authoritative state

- Branch: `feat/central-authentication`; pushed head
  `302ba4dce2e965f085a33836b0043423e7ec1be4`.
- [PR #1005](https://github.com/mobinseven/Vitarish/pull/1005) is open,
  unmerged and mergeable at this checkpoint.
- Live `main` is `99577ebaca9e9f7eef408ba044d15eb23abdcd48`, already integrated.
- [Latest CI](https://github.com/mobinseven/Vitarish/actions/runs/42471401381)
  completed successfully during this handoff for `302ba4dce`.
- No production workflow was dispatched; no production migration was applied.
- The task goal is paused. There is no complete passing final E2E result.

Read [the implementation plan](central-authentication.md),
[ADR-0021](../architecture/decisions/0021-central-browser-authentication.md),
and root/nearest `AGENTS.md` before edits. Re-derive the mandatory checklist;
inspect tracked, staged and untracked changes and every added quoted value.
Keep ownership/dependencies, read-only queries, explicit failures, compatibility,
generated-output rules and security protections intact. Preserve unrelated work.

## Implemented

Auth owns interactive login, recovery and personal security. Host-only app
sessions link to a durable central browser root; logout ends its family.
Single-use browser-correlated continuations preserve app intent and membership
checks. Pending verification survives focus changes and reload without another
delivery or persistence of entered passwords/codes. Central account screens
show browser sessions. Root and app cookie renewal retain host isolation.

Mainline platform home, explicit workspace choice, profile gates, second-factor
and recent-authentication behavior are integrated. The administrator guard now
allows handoff redemption before demanding an app session and preserves intent.
The obsolete workspace sidebar label is replaced with the existing Home label.

**Knowledge is obsolete and explicitly excluded by the owner.** Its scenario
is skipped, its checks are removed from the active cross-app scenario, and the
runner no longer builds/starts it. It already has no production deployment
service. Its legacy application/configuration/fixture are retained outside this
task; do not expand the release into application deprecation.

## Validation evidence and exact remaining uncertainty

- Local Authentication tests previously passed 555 with zero skips against
  disposable PostgreSQL. Parties 239, Journey 98, Users 307, Landing 42,
  Main 120 and PM Application 31 passed; affected Web checks and CI passed.
  Do not repeat unchanged suites merely to accumulate evidence.
- Claude initial and follow-up **high-effort code reviews completed** before
  E2E. Opus 5.5 medium consultation also completed. Reports are retained in
  `src/Hosts/Web/artifacts/central-auth-review/`.
- Last completed collective run, on `409a0927a`: **69 passed, 4 failed,
  14 existing skips**. Artifacts:
  `src/Hosts/Web/artifacts/two-host-e2e/runs/2026-11-08T09-32-41.434Z-9700-c1b86c81`.
  Its failed areas were passkey method evidence, two cross-app scenarios and
  personal access-history evidence. The Knowledge scenario is now excluded.
- Corrections are committed and typechecked, but still need a full runtime pass:
  passkey sign-in evidence runs on Auth; Patients enters via its existing login
  route with a profile return intent; browser-root IDs must stay unchanged
  across app visits with exactly one current root; app-local credential refusal
  is checked on PM; history is read on Auth. Session-limit evidence now uses
  its own seeded administrator so earlier browser roots cannot affect the limit.
- The prior run also proved the email tab switch, reload, single delivery and
  successful code redemption. Its resumed-email and Persian account screenshots
  were inspected. This is local evidence, not deployment proof.
- The Playwright-only DNS adapter was integration-checked for callback/promise
  lookup, original Host preservation and host-only cookie isolation. It does
  not alter Main/Web or production DNS. Keep it scoped to Playwright.
- A later tool-attached run stopped after 14 passes without a final result.
  Its handle, PID and listeners were absent; it is incomplete, not passed.
- The latest detached run was explicitly stopped at this checkpoint during
  owner Web compilation, **before any browser scenarios**. Artifact run ID:
  `2026-11-08T10-25-53.123Z-17476-9fa89c52`.

## Resume sequence

1. Confirm the paused goal has been resumed. Re-read instructions, inspect the
   complete diff and check the latest CI/PR/main state. Do not merge unrelated
   navigation or landing work. All product changes are already committed.
2. Finish the **full active-app** two-host gate. Use a fresh proof filename.
   Run from `src/Hosts/Web`; keep the separate E2E host and normal security paths:

   ```powershell
   $proofPath = Join-Path (Get-Location) ('artifacts/two-host-e2e/central-authentication-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.png')
   $env:VITARISH_E2E_PROOF_SCREENSHOT = $proofPath
   npm run test:e2e:two-host
   if ($LASTEXITCODE -ne 0) { throw 'The active-app E2E gate failed.' }
   if (-not (Test-Path -LiteralPath $proofPath)) { throw 'The proof screenshot was not produced.' }
   ```

   For a run that must survive conversation turns, the retained local wrapper
   `artifacts/central-auth-review/run-active-app-proof.ps1` can be started with
   `Start-Process -WindowStyle Hidden -PassThru`. It captures the native exit
   result and verifies the proof file. Choose a fresh screenshot/result path,
   retain its PID/start time and poll both the actual process and result file.
   The old PID record is terminal; never kill a reused PID without verifying
   its command line. Do not mistake an observation timeout for termination.
3. Investigate only fresh failures; do not restart broad architecture work or
   relax authorization, antiforgery, WebAuthn trust, rate limits or assertions.
   Fully exercise passkey sign-in, active-app SSO/family logout and the complete
   security-evidence scenario, including session limits and policy restoration.
   Distinguish the owner-approved Knowledge skip from existing skips.
4. Inspect the same passing run's authenticated proof and changed Auth screens:
   resumed email, browser security, Persian account and relevant audit details.
   Correct actual defects and rerun only as needed. A partial or failed run is
   insufficient; no narrow substitute for the final active-app suite.
5. Audit all tracked/untracked changes and quoted values; confirm required CI
   passes for the candidate. Update PR validation and merge with an expected
   head SHA. Verify the resulting tree equals the reviewed runtime tree.
6. Dispatch `.github/workflows/production.yml` **on main** and confirm its exact
   head SHA. The live workflow's manual dispatch publishes/deploys; PR/push
   runs are checks. Follow Required, immutable image publishing, deployment,
   migration/readiness and production browser-evidence jobs to completion.
   Inspect the deployed-release plan/digests and fresh public HTTPS/browser
   evidence. Do not claim authenticated production smoke without doing it.
7. Restore the preserved landing stash safely after the release, preserving
   all tracked and untracked owner changes. Record release evidence in its
   owner, clean owned temporary artifacts, and perform the completion audit
   before marking the goal complete.

## Compatibility and release constraints

The additive generated Authentication migrations preserve existing data and
mainline's applied migration history. No generated file should be hand-edited
or restored to manufacture a baseline. New Web and Main ship together. Legacy
null-linked sessions remain independent until reauthentication. Keep additive
schema on rollback; revoke linked roots before a backend rollback that cannot
validate browser families. Production's WebAuthn RP ID remains unchanged; the
Auth-host RP override is E2E-only. Do not create a Knowledge production service.

Deployment was already authorized; the owner reported no active production
sessions. Repeated permission requests are unnecessary after an explicit resume.

## Preserved work and checkpoint cleanup

- Landing WIP remains in stash
  `ad139a4620889a02bfb51f44b4d6c50bd67ded7a` (five tracked and four untracked
  files). Do not pop/drop it blindly or include it in the release commit.
- Wrapper PID `17672` and runner PID `17476` were stopped as verified owned
  processes. Ports `7272`, `3005`–`3009` have no listeners. The latest run did
  not change tracked Next build files. Earlier temporary Next changes were
  restored only after semantic/reference verification. No local servers remain.
- The checkpoint result is retained at
  `src/Hosts/Web/artifacts/central-auth-review/active-apps-checkpoint.json`;
  there is deliberately no fabricated successful exit result.
- Abandoned `.next-two-host-e2e-*` directories remain under app roots. Native
  recursive deletion of a verified workspace-contained directory was rejected
  by automatic approval review: **"blocked by policy"**. Do not bypass the
  rejection with another shell or deletion mechanism. Retained affected run IDs:
  `2026-11-07T15-22-03.947Z-21700-aa2c433f`,
  `2026-11-07T16-53-46.218Z-23120-181dcb4c`,
  `2026-11-08T09-59-48.120Z-11004-9f3fdda1`, and
  `2026-11-08T10-25-53.123Z-17476-9fa89c52`.
- This handoff and the implementation-plan status are local documentation
  drafts; no additional commit, merge or deployment is part of this pause.
