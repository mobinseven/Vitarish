# Focused module builds

Focused module builds reduce inner-loop build time by composing `Modules.Aggregator` with the explicit module graph needed for a host build.

The central source of truth is `src/Build/ModuleBuildProfiles.props`. It defines `ModuleBuildProfile`, defaults it to `All`, and maps each focused profile to the required `Enable*Module` MSBuild properties.

## Main host baseline

`src/Hosts/Main/Main.csproj` is not module-neutral. Its shared layout, setup, identity, admin, file input, feed infrastructure, and dashboard composition code currently have compile-time dependencies on these baseline modules:

- `Requests`
- `Users`
- `Parties`
- `Platform`
- `Feed`
- `Files`
- `Sea`
- `Communications`
- `Appointments`
- `Commerce`

Those modules are always enabled by `Main.csproj`, including `ModuleBuildProfile=Custom`, because they are part of the current host composition boundary. Disabled optional modules must not be referenced by active host/UI code.

## Profiles

`ModuleBuildProfile=All` is the default and preserves the full module build graph. Use it, or build the full solution, before merging.

Single-module profiles enable the named module plus the Main host baseline above. For example, `ModuleBuildProfile=Medical` enables the baseline graph and `Medical`; `ModuleBuildProfile=Appointments` enables the baseline graph because `Appointments` is already a host baseline module.

Examples:

```bash
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Users
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Appointments
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Custom -p:EnableUsersModule=true -p:EnableAppointmentsModule=true
```

Available single-module profile names match the module names referenced by `src/Modules/Aggregator/Modules.Aggregator/Modules.Aggregator.csproj`, including `Users`, `Appointments`, `Communications`, `Files`, `Knowledge`, `Financial`, `Requests`, `Parties`, `Platform`, `Medical`, `Commerce`, `Sea`, `Feed`, `Intelligence`, `Search`, `Contents`, `Social`, and `Links`.

## Custom focused builds

Use `ModuleBuildProfile=Custom` when a change needs more than the Main host baseline but not the full graph. Set each additional required `Enable*Module` property explicitly:

```bash
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Custom -p:EnableUsersModule=true -p:EnableAppointmentsModule=true
```

For `Main.csproj`, the baseline modules listed above are still enabled by the host project. Additional modules remain explicit opt-ins.

## Safety expectations

Focused builds are for local inner-loop speed only. They do not replace the default full build graph. Before opening or merging a change, run either:

```bash
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=All
```

or the repository's full solution build.

Module selection stays at the MSBuild project-composition boundary. Do not add C# preprocessor symbols or `#if` blocks to hide disabled module references. If host/UI code needs an optional module, either add that module to the required profile graph or move the code behind a project-composition boundary so the disabled module assembly is not referenced at compile time.

## Benchmarking focused build speed

Use `scripts/benchmark-focused-builds.sh` to compare the Main host `All`, `Appointments`, `Medical`, and `Custom` profiles with a consistent cache shape:

```bash
scripts/benchmark-focused-builds.sh
```

The benchmark script:

1. Generates an MSBuild restore graph for each profile and records the project count.
2. Runs one restore/build warm-up per profile.
3. Runs at least three timed no-op builds per profile with `--no-restore`.
4. Writes raw measurements to `results.csv` and a comparison table to `summary.md` under `artifacts/focused-build-benchmarks/<timestamp>/`.
5. Compares each focused profile against `ModuleBuildProfile=All` and marks material improvements using the configured threshold.

Optional arguments:

```bash
scripts/benchmark-focused-builds.sh --runs 5 --threshold-percent 15
scripts/benchmark-focused-builds.sh --output-dir artifacts/focused-build-benchmarks/manual-run
```
