Implement ADR-0008 as a multi-app Web workspace with one shared presentation package and two app boundaries.



Context:



* ADR source of truth: `docs/architecture/decisions/0008-standardize-app-experience-boundaries.md`

* Implementation roadmap: `docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md`

* Current Web app root: `src/Hosts/Web`

* Current app documentation root: `docs/apps/clinic-owner`

* New private-practice app name: `private-practice-general`

* New Knowledge QA demo app name: `knowledge-qa-demo`

* Shared Web UI package name: `@vitarish/web-ui`

* Backend remains the single source of truth for data, commands, authorization, validation, and workflows.



1. Record implementation decisions in the ADR-0008 roadmap:



   * Update `docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md`.

   * Set the current app child folder to `src/Hosts/Web/apps/private-practice-general`.

   * Set the second app child folder to `src/Hosts/Web/apps/knowledge-qa-demo`.

   * Set the private-practice app documentation root to `docs/apps/private-practice-general`.

   * Set the Knowledge QA demo documentation root to `docs/apps/knowledge-qa-demo`.

   * Close the roadmap open questions about app child folder names.

   * Note that `clinic-owner` is the former app identity and should remain only as temporary compatibility or historical reference when explicitly needed.



2. Inventory the current Web app before moving files:



   * Inspect `src/Hosts/Web/src/app` and `src/Hosts/Web/src/features`.

   * Classify each route/feature as:



     * `private-practice-general`

     * `knowledge-qa-demo`

     * shared app infrastructure copied or extracted per app

     * candidate for `@vitarish/web-ui`

     * platform/backend-owned and not moved

   * At minimum classify:



     * `src/Hosts/Web/src/app/owner/clinic/**`

     * `src/Hosts/Web/src/app/user/clinic/**`

     * `src/Hosts/Web/src/features/clinic/**`

     * `src/Hosts/Web/src/app/knowledge/qa/**`

     * `src/Hosts/Web/src/features/knowledge/qa/**`

     * `src/Hosts/Web/src/app/account/**`

     * `src/Hosts/Web/src/features/account/**`

     * `src/Hosts/Web/src/app/login/**`

     * `src/Hosts/Web/src/features/auth/**`

     * `src/Hosts/Web/src/app/home/**`

     * `src/Hosts/Web/src/features/home/**`

     * `src/Hosts/Web/src/features/landing/**`

     * `src/Hosts/Web/src/app/owner/sea/**`

     * `src/Hosts/Web/src/features/sea/**`

     * `src/Hosts/Web/src/features/journey/**`

   * Record whether each item is moved, copied, extracted, deferred, or deleted.



3. Define the `clinic-owner` to `private-practice-general` rename map:



   * Create a task/rename map document under `docs/apps/private-practice-general/tasks/` after the docs root exists.

   * Record:



     * old doc root: `docs/apps/clinic-owner`

     * new doc root: `docs/apps/private-practice-general`

     * old app label: `clinic-owner`

     * new app label: `private-practice-general`

     * user-facing name: “Private Practice General” unless product owner chooses otherwise

     * npm package name: `@vitarish/private-practice-general`

     * route compatibility policy

     * docs-link compatibility policy

     * search strings: `clinic-owner`, `Clinic Owner`, `apps/clinic-owner`, `@vitarish/web`

   * Do not rename backend modules such as `Clinic`, `Appointments`, `Medical`, or `Knowledge` unless a separate backend/module decision exists.



4. Migrate private-practice app docs:



   * Move `docs/apps/clinic-owner` to `docs/apps/private-practice-general`.

   * Update app metadata lines from `App: clinic-owner` to `App: private-practice-general`.

   * Update links from `docs/apps/clinic-owner/...` to `docs/apps/private-practice-general/...`.

   * Update headings/prose that refer to the app identity as “Clinic Owner”; preserve domain terms such as clinic, owner, patient, appointment, provider, reservation, prescription, and service when they describe workflows.

   * Keep app doc collection folders flat:



     * `decision-trees/`

     * `journeys/`

     * `tests/`

     * `decisions/`

     * `roadmaps/`

     * `tasks/`

     * `runbooks/`

   * Do not create or preserve nested app module mirrors under `docs/apps/private-practice-general/modules/**`.

   * Update global indexes such as `docs/indexes/journeys.md`.



5. Flatten legacy app module docs:



   * For every file under `docs/apps/clinic-owner/modules/**`, decide whether it is:



     * an app journey,

     * an app test/coverage document,

     * an app task,

     * platform module documentation incorrectly located under app docs,

     * obsolete duplicate content.

   * Move app-owned content into flat collections under `docs/apps/private-practice-general/{journeys,tests,tasks,...}/`.

   * Move platform-owned content to the appropriate `docs/platform/modules/<Module>/...` only if it defines module behavior rather than app experience.

   * Repair links in:



     * `docs/indexes/journeys.md`

     * `docs/tools/journey-graph-visualizer.md`

     * `docs/platform/modules/knowledge/journeys/README.md`

     * moved README files.

   * Do not create nested replacement folders.



6. Create Knowledge QA demo app docs:



   * Create `docs/apps/knowledge-qa-demo/README.md`.

   * Create only non-empty collection folders required by initial content, likely:



     * `decision-trees/`

     * `journeys/`

     * `tests/`

     * `tasks/`

   * Add an initial Knowledge QA demo decision tree covering:



     * user enters demo,

     * user submits a question,

     * loading state,

     * successful answer,

     * empty/no-answer state,

     * backend/API failure,

     * permission/auth state if applicable.

   * Add at least one journey derived from that decision tree.

   * Add a test plan that cites the journey and states covered/deferred success, empty, failure, permission, cancellation, idempotency, concurrency, and recovery scenarios.



7. Promote `src/Hosts/Web` to the Web npm workspace root:



   * Refactor `src/Hosts/Web/package.json` into a workspace-root package.

   * Use a root package name such as `@vitarish/web-workspace`.

   * Keep `"private": true`.

   * Add workspaces:



     * `apps/*`

     * `shared/*`

   * Keep `src/Hosts/Web/package-lock.json` as the single Web workspace lockfile.

   * Remove or retire `src/Hosts/Web/pnpm-lock.yaml`, because ADR-0008 keeps npm as the Web package manager.

   * Move app-specific scripts to app packages.

   * Add root aggregation scripts such as:



     * `dev:private-practice-general`

     * `dev:knowledge-qa-demo`

     * `build`

     * `typecheck`

     * `test`

     * `validate:api-boundary`

     * `validate:localization`

     * `validate:date-inputs`

     * `validate:web-ui-boundary`

     * `validate:app-boundaries`

     * `validate:docs:app-boundaries`



8. Classify current Web root files as workspace-owned or app-owned:



   * Workspace-owned candidates:



     * `src/Hosts/Web/package.json`

     * `src/Hosts/Web/package-lock.json`

     * workspace validation scripts under `src/Hosts/Web/scripts/`

     * shared base lint/TypeScript configs only if consumed by multiple apps/packages immediately.

   * App-owned candidates to move under `src/Hosts/Web/apps/private-practice-general`:



     * `next.config.ts`

     * `next-env.d.ts`

     * `tailwind.config.ts`

     * `postcss.config.mjs`

     * `vitest.config.ts`

     * `vitest.d.ts`

     * `.env.example`

     * `Dockerfile`

     * `.dockerignore`

     * `public/`

     * `src/`

     * `mimocode.jsonc`

     * `src/Web.esproj` if tied to the app rather than workspace.

   * Create shared base configs only when at least two apps/packages consume them immediately.



9. Move the current app into `apps/private-practice-general`:



   * Create `src/Hosts/Web/apps/private-practice-general`.

   * Move current app-owned files/folders there.

   * Create `src/Hosts/Web/apps/private-practice-general/package.json`.

   * Use:



     * `name: "@vitarish/private-practice-general"`

     * `private: true`

   * Preserve existing app dependencies and scripts needed by that app:



     * `dev`

     * `build`

     * `start`

     * `typecheck`

     * `test`

     * `generate:api`

     * `validate:api-boundary`

     * `validate:localization`

     * `validate:date-inputs`

   * Repair all relative paths in scripts/configs after the move.



10. Repair TypeScript, Next, Vitest, Orval, and import aliases after the move:



    * Update `src/Hosts/Web/apps/private-practice-general/tsconfig.json`.

    * Keep app-local alias such as `@/* -> ./src/*`.

    * Ensure the app can resolve `@vitarish/web-ui` once that package exists.

    * Update:



      * Next config paths,

      * Vitest setup paths,

      * MSW/test paths,

      * ESLint paths,

      * Orval config/output paths,

      * validation script paths.

    * Search for brittle assumptions:



      * `src/Hosts/Web/src`

      * `../src`

      * `src/api/client/orval.config.ts`

      * `scripts/validate-`



11. Update Docker/deployment paths:



    * Inspect:



      * `src/Hosts/Web/Dockerfile`

      * `src/Hosts/Web/.dockerignore`

      * `.github/workflows/**`

      * `src/Hosts/Web/src/Web.esproj`

    * Update builds so they run from the Web workspace root but target `apps/private-practice-general` explicitly.

    * If both apps need container images, use app-specific Dockerfiles or explicit build arguments.

    * Do not allow a Dockerfile to implicitly build whichever app happens to be current.



12. Create `apps/knowledge-qa-demo`:



    * Create `src/Hosts/Web/apps/knowledge-qa-demo`.

    * Create `src/Hosts/Web/apps/knowledge-qa-demo/package.json`.

    * Use:



      * `name: "@vitarish/knowledge-qa-demo"`

      * `private: true`

    * Add only the minimum Next/React app shell needed:



      * app layout/global styles,

      * providers required for auth/session/query/theme/localization if the demo needs them,

      * Knowledge QA route(s),

      * app-owned generated API client config or API adapter,

      * app-owned test setup.

    * Define scripts:



      * `dev`

      * `build`

      * `start`

      * `typecheck`

      * `test`

      * `generate:api` if the app owns generated clients

      * app-specific validators.

    * Do not import from `../private-practice-general/src/**`.



13. Extract Knowledge QA app code:



    * Move or copy the minimum Knowledge QA app-owned code from:



      * `src/Hosts/Web/src/app/knowledge/qa/**`

      * `src/Hosts/Web/src/features/knowledge/qa/**`

    * Target:



      * `src/Hosts/Web/apps/knowledge-qa-demo/src/app/knowledge/qa/**`

      * `src/Hosts/Web/apps/knowledge-qa-demo/src/features/knowledge/qa/**`

    * Keep generated clients/hooks app-owned.

    * If private-practice should no longer expose Knowledge QA, remove or redirect its Knowledge QA route.

    * If a Knowledge QA presentation component has no API, route, workflow, localization-provider, or business-type dependency and is needed by both apps, move only that generic presentation artifact into `@vitarish/web-ui`.



14. Decide and implement Knowledge QA demo authentication shell:



    * In `docs/apps/knowledge-qa-demo/decisions/`, record whether the demo is:



      * public/anonymous,

      * authenticated with existing platform auth,

      * authenticated with restricted demo-only roles,

      * embedded behind another app.

    * If authenticated:



      * keep API calls/session hooks app-owned,

      * copy/adapt only minimal auth providers,

      * pass generic user/session display state to shared UI.

    * If public:



      * do not include account/private-practice navigation,

      * show explicit failure states for API calls requiring unavailable credentials.



15. Create `@vitarish/web-ui`:



    * Create `src/Hosts/Web/shared/web-ui`.

    * Add:



      * `package.json`

      * `src/index.ts`

      * package-local `tsconfig.json`

      * package-local tests if any component/helper is exported

      * README describing ADR-0008 promotion rules.

    * Use:



      * `name: "@vitarish/web-ui"`

      * `version: "0.1.0"`

      * `private: true`

      * explicit `exports` for public entry points.

    * Allow only presentation dependencies:



      * React and React types,

      * HeroUI/HeroUI Pro presentation primitives already approved in the Web host,

      * React Aria presentation primitives,

      * Tailwind merge/variant helpers if immediately used,

      * Font Awesome React/icon packages if immediately used.

    * Do not add backend, generated client, React Query, route, localization provider, or feature dependencies.



16. Add first safe shared Web UI artifacts:



    * Add only artifacts satisfying ADR-0008 promotion rules.

    * Recommended first artifacts:



      * generic empty/loading/error state presentation contracts,

      * generic icon presentation helpers if already duplicated,

      * theme token exports or theme bridge helpers if presentation-only,

      * documented HeroUI/HeroUI Pro composition examples,

      * generic form field shell only if it does not hide HeroUI behavior.

    * For every export:



      * use generic UI language in props,

      * avoid patient/clinic/knowledge/account nouns,

      * avoid generated DTO types,

      * avoid app route/link dependencies,

      * add package-local tests or usage examples.



17. Wire both apps to consume `@vitarish/web-ui` through public exports:



    * In `apps/private-practice-general`, replace one or two safe duplicated presentation patterns with `@vitarish/web-ui` imports.

    * In `apps/knowledge-qa-demo`, consume the same generic presentation export.

    * Good first consumers:



      * generic loading state,

      * generic empty state,

      * generic error state,

      * generic icon helper.

    * Do not migrate workflow components.

    * Do not import `@vitarish/web-ui/src/**`.



18. Keep generated API clients app-owned:



    * For `private-practice-general`:



      * move existing Orval config and generated client output into the app root,

      * update generation script paths,

      * preserve existing feature API wrapper boundaries.

    * For `knowledge-qa-demo`:



      * create a minimal Orval config for Knowledge QA endpoints if feasible,

      * otherwise document why it uses a broader generated client inside the app root.

    * Do not import generated clients from another app.

    * Do not place generated clients in `@vitarish/web-ui`.



19. Define app-owned API adapter policy:



    * Document/implement policy for:



      * `apiFetch`,

      * generated clients,

      * React Query hooks,

      * error normalization,

      * authentication/session headers,

      * retry/cancellation behavior.

    * Preferred policy:



      * each app owns generated clients and feature hooks,

      * each app may duplicate minimal API adapter glue until a separate API-client package ADR exists,

      * shared UI receives normalized generic display state only.

    * If duplication becomes risky, propose a separate ADR for a Web API client package.

    * Do not put API adapter code in `@vitarish/web-ui`.



20. Split localization by app:



    * Move current localization into `apps/private-practice-general/src/localization` unless a file is truly workspace-shared.

    * Create `apps/knowledge-qa-demo/src/localization` with only demo copy.

    * `@vitarish/web-ui` must accept display strings, labels, aria labels, and messages through props/children.

    * `@vitarish/web-ui` must not import localization catalogs or providers.

    * Update localization validation to run per app and fail when:



      * app UI lacks required messages,

      * shared UI imports app localization files,

      * stale “Clinic Owner” appears where app identity should be “Private Practice General”.



21. Update route, navigation, and app identity:



    * In `apps/private-practice-general/src`, update:



      * app metadata/title,

      * navigation labels,

      * app shell branding,

      * test names,

      * coverage docs,

      * package references.

    * Preserve domain nouns when they describe workflows.

    * Search for:



      * `clinic-owner`

      * `Clinic Owner`

      * `@vitarish/web`

      * `apps/clinic-owner`

    * For `knowledge-qa-demo`, define a minimal route map:



      * `/` redirects to or renders Knowledge QA demo landing,

      * `/knowledge/qa` hosts the QA surface,

      * auth/error routes only if required.

    * Do not include private-practice owner/user clinic routes in the demo app.



22. Implement compatibility redirects or document breaking changes:



    * Decide whether old app URLs and docs links must remain valid.

    * For docs:



      * update global indexes,

      * add redirect notes only if the docs system supports them.

    * For Web routes:



      * add explicit redirects in the owning app or deployment layer if old entry URLs must work.

    * If compatibility is not required, document the breaking change in the relevant app roadmap/task.

    * Do not leave stale `clinic-owner` paths as undocumented active sources of truth.



23. Add ADR-0008 documentation structure validation:



    * Create `tools/architecture/validate_app_experience_boundaries.py`.

    * Enforce:



      * block new files under `docs/apps/*/modules/**`,

      * block nested folders under `docs/apps/<App>/{decision-trees,journeys,tests,decisions,roadmaps,tasks,runbooks}/`,

      * require app test docs to cite app journeys they verify,

      * require journey docs with branching/gates/recovery/persona-specific behavior to cite at least one app decision tree,

      * keep global indexes as navigation only.

    * Add configuration for exceptions if needed.

    * Wire the validator into CI and root validation scripts.



24. Add `@vitarish/web-ui` forbidden import validation:



    * Create `src/Hosts/Web/scripts/validate-web-ui-boundary.mjs`.

    * From `src/Hosts/Web/shared/web-ui/src`, block imports matching:



      * `../../apps/`,

      * app route/page folders,

      * app feature folders,

      * generated OpenAPI clients/DTOs,

      * React Query hooks,

      * backend `src/Modules/**`, `src/Apps/**`, `src/Identity/**`, `src/Libraries/**`,

      * app localization catalogs/providers,

      * non-Web host implementation files,

      * feature-specific business modules/types.

    * Allow only package-local files and approved presentation dependencies.

    * Run from the Web workspace root.



25. Add cross-app and public-import validation:



    * Create or extend `src/Hosts/Web/scripts/validate-web-app-boundaries.mjs`.

    * Enforce:



      * apps may import `@vitarish/web-ui`,

      * apps must not import `@vitarish/web-ui/src/**`,

      * `apps/private-practice-general` must not import `apps/knowledge-qa-demo/src/**`,

      * `apps/knowledge-qa-demo` must not import `apps/private-practice-general/src/**`,

      * app-to-app relative imports are forbidden,

      * shared UI must flow through `shared/web-ui` only when presentation-only.

    * Wire into root `validate:app-boundaries`.



26. Add HeroUI wrapper promotion governance:



    * Add `src/Hosts/Web/shared/web-ui/README.md` or docs under `docs/hosts/web/`.

    * For each proposed shared component, require:



      * why direct HeroUI/HeroUI Pro usage is insufficient,

      * which two apps/modules reuse it or why it protects a major design artifact,

      * which backend/app/feature dependencies were removed,

      * why public props are generic UI terms,

      * how it preserves HeroUI’s component model.

    * Optionally flag vague wrapper names such as `SurfaceCard`, `CommonButton`, `Manager`, `Helper`, `Util`, or vague `Service`.



27. Move existing tests with `private-practice-general`:



    * Move `src/Hosts/Web/src/__tests__` to `src/Hosts/Web/apps/private-practice-general/src/__tests__`.

    * Update:



      * Vitest config,

      * setup files,

      * MSW handlers,

      * test imports.

    * Preserve coverage for:



      * auth/account,

      * clinic/private-practice workflows,

      * localization,

      * API boundary behavior,

      * date input behavior,

      * component behavior.

    * Do not keep Knowledge QA tests in private-practice if the feature moves to the demo app.



28. Add Knowledge QA demo tests:



    * Under `src/Hosts/Web/apps/knowledge-qa-demo/src/__tests__`, test:



      * initial demo route renders,

      * question input accepts text,

      * submit invokes app-owned Knowledge QA hook/API adapter,

      * loading state displays,

      * successful answer displays,

      * empty/no-answer state displays,

      * API failure displays retryable error state,

      * auth/permission state if applicable,

      * private-practice navigation/routes are not rendered.



29. Add package-level tests for `@vitarish/web-ui`:



    * In `src/Hosts/Web/shared/web-ui`, add:



      * `typecheck` script,

      * `test` script when exports exist,

      * tests for empty/loading/error/icon/theme helpers.

    * Tests should verify:



      * labels/content passed by props render,

      * accessibility labels work,

      * retry/callback handlers are invoked,

      * app providers are not required,

      * generated clients/backend types are not imported.

    * Do not mock feature data or backend contracts in `web-ui` tests.



30. Define workspace validation scripts:



    * In `src/Hosts/Web/package.json`, add root scripts for:



      * `npm run typecheck --workspaces --if-present`

      * `npm run test --workspaces --if-present`

      * `npm run build --workspace @vitarish/private-practice-general`

      * `npm run build --workspace @vitarish/knowledge-qa-demo`

      * `npm run validate:web-ui-boundary`

      * `npm run validate:app-boundaries`

      * `npm run validate:docs:app-boundaries`

      * `npm run validate:localization --workspaces --if-present`

      * `npm run validate:api-boundary --workspaces --if-present`

      * `npm run validate:date-inputs --workspaces --if-present`

    * Keep app-specific commands inside app packages and aggregate from the workspace root.



31. Update CI workflows:



    * Inspect `.github/workflows/**` for `src/Hosts/Web` assumptions.

    * Update CI so it:



      * installs from `src/Hosts/Web`,

      * uses `src/Hosts/Web/package-lock.json`,

      * builds each app explicitly,

      * typechecks all workspaces,

      * runs app tests and package tests,

      * runs ADR-0008 validators,

      * runs API/localization/date validators in the right app workspaces.

    * Avoid commands that depend on `src/Hosts/Web` being an app root.



32. Document local development workflow:



    * Update or create docs under `docs/hosts/web/`.

    * Include:



      * install from `src/Hosts/Web`,

      * run private-practice app,

      * run Knowledge QA demo app,

      * generate API clients per app,

      * typecheck/test/build all workspaces,

      * run ADR-0008 validators,

      * add a new shared UI artifact to `@vitarish/web-ui`,

      * add a new Web app without copying another app’s private files.



33. Add private-practice decision tree traceability:



    * Under `docs/apps/private-practice-general/decision-trees/`, create decision trees for major branching workflows:



      * patient search/create/check-in,

      * appointment/reservation management,

      * service/provider/settings management,

      * reminders,

      * statistics/settings users.

    * Update existing journeys under `docs/apps/private-practice-general/journeys/` to cite the relevant decision tree.

    * Do not duplicate platform module behavior; link to platform module docs for backend/module rules.



34. Update private-practice React coverage audit:



    * Move/update `docs/apps/private-practice-general/tests/react-interface-coverage-audit.md`.

    * Update:



      * app path labels from `apps/clinic-owner` to `apps/private-practice-general`,

      * route/component evidence after source move,

      * journey links,

      * statuses for routes extracted out of private-practice, especially Knowledge QA if removed,

      * explicit covered/deferred success, empty, failure, permission, cancellation, idempotency, concurrency, and recovery scenarios where applicable.



35. Repair Knowledge module references:



    * Update `docs/platform/modules/knowledge/journeys/README.md`.

    * Replace references to `docs/apps/clinic-owner/tests/react-interface-coverage-audit.md` for normal-user QA React evidence with the relevant `docs/apps/knowledge-qa-demo/tests/...` document.

    * Keep platform Knowledge module behavior app-agnostic.

    * Clarify that Knowledge owner/admin journeys remain platform/module scope unless a specific app owns their UX.



36. Create shared Web UI promotion inventory:



    * Create a linked task document from `docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md`.

    * Inventory candidates from:



      * `apps/private-practice-general/src/components/**`

      * `apps/private-practice-general/src/features/**/components/**`

      * `apps/knowledge-qa-demo/src/**`

      * duplicated shell/empty/error/loading/icon/form patterns.

    * Classify each candidate:



      * `shared-now`

      * `feature-adapter-needed`

      * `app-local`

      * `defer`

      * `reject-raw-heroui-is-enough`

    * For each `shared-now` candidate, record:



      * current file path,

      * target `@vitarish/web-ui` export,

      * props after business nouns are removed,

      * first two consumers,

      * required tests,

      * forbidden dependencies removed.



37. Split feature-coupled UI candidates before moving:



    * For each `feature-adapter-needed` candidate:



      * keep backend DTOs, permissions, localized copy, route intents, and query/mutation state in the owning app adapter,

      * map values into generic props,

      * move only generic presentation into `@vitarish/web-ui`.

    * Apply only when there is immediate reuse across both apps or multiple modules.

    * Otherwise leave the component app-local.



38. Clean up stale artifacts:



    * Search and remove/update stale references:



      * `clinic-owner`

      * `Clinic Owner`

      * `docs/apps/clinic-owner`

      * `src/Hosts/Web/src`

      * `@vitarish/web`

      * app-to-app relative imports

      * deep `@vitarish/web-ui/src` imports

    * Keep only explicitly approved compatibility redirects or historical notes.

    * Delete empty folders rather than preserving them for symmetry.

    * Remove or formally retire `src/Hosts/Web/pnpm-lock.yaml`.



39. Recommended PR slicing:



    * PR 1: roadmap decisions, inventory, rename map, Knowledge QA docs skeleton.

    * PR 2: docs rename/flattening, indexes, Knowledge references, docs validator.

    * PR 3: Web workspace foundation and move current app to `apps/private-practice-general`.

    * PR 4: `@vitarish/web-ui` skeleton and boundary validators.

    * PR 5: `apps/knowledge-qa-demo` and Knowledge QA extraction.

    * PR 6: first cross-app shared UI migration.

    * PR 7: CI/developer workflow hardening and stale artifact cleanup.



40. Required validation commands after implementation:



    * From `src/Hosts/Web`: `npm install`

    * From `src/Hosts/Web`: `npm run typecheck --workspaces --if-present`

    * From `src/Hosts/Web`: `npm run test --workspaces --if-present`

    * From `src/Hosts/Web`: `npm run build --workspace @vitarish/private-practice-general`

    * From `src/Hosts/Web`: `npm run build --workspace @vitarish/knowledge-qa-demo`

    * From `src/Hosts/Web`: `npm run validate:web-ui-boundary`

    * From `src/Hosts/Web`: `npm run validate:app-boundaries`

    * From repo root: `python3 tools/architecture/validate_app_experience_boundaries.py`

    * From repo root: `python3 tools/architecture/validate_interface_boundaries.py`

    * Run app-specific API, localization, and date-input validators where applicable.



41. Hot scenarios requiring runtime/browser validation:



    * Private-practice login/account/clinic navigation after route move.

    * Knowledge QA demo question submission success/failure.

    * Container build/deploy path for each app.

    * Auth/session behavior if both apps share backend auth.

    * Generated client regeneration after moving Orval configs.

    * Runtime environment variable loading per app.

    * Redirect behavior for old app URLs if compatibility is required.



42. Hard stops:



    * Do not create a second backend.

    * Do not put generated clients, backend contracts, React Query hooks, app routes, app localization providers, or workflow decisions in `@vitarish/web-ui`.

    * Do not let either app import the other app’s private files.

    * Do not deep-import `@vitarish/web-ui/src/**`.

    * Do not create nested app docs under `docs/apps/<App>/modules/**`.

    * Do not migrate feature-specific clinic/private-practice or Knowledge workflow components into `@vitarish/web-ui`.

    * Do not create custom HeroUI wrappers when direct HeroUI/HeroUI Pro usage is sufficient.

    * Do not keep both npm and pnpm lockfiles as active Web package-manager sources of truth.

    * Do not rename backend/domain modules as part of this app-host split unless a separate decision explicitly approves it.
