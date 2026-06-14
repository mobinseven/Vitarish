# Focused module builds

Focused module builds reduce inner-loop build time by composing `Modules.Aggregator` with only the module projects needed for the change being made.

The central source of truth is `src/Build/ModuleBuildProfiles.props`. It defines `ModuleBuildProfile`, defaults it to `All`, and maps each focused profile to the matching `Enable*Module` MSBuild property.

## Profiles

`ModuleBuildProfile=All` is the default and preserves the full module build graph. Use it, or build the full solution, before merging.

Examples:

```bash
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Users
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Appointments
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Custom -p:EnableUsersModule=true -p:EnableAppointmentsModule=true
```

Available single-module profile names match the module names referenced by `src/Modules/Aggregator/Modules.Aggregator/Modules.Aggregator.csproj`, including `Users`, `Appointments`, `Communications`, `Files`, `Knowledge`, `Financial`, `Requests`, `Parties`, `Platform`, `Medical`, `Commerce`, `Sea`, `Feed`, `Intelligence`, `Search`, `Contents`, `Social`, and `Links`.

## Custom focused builds

Use `ModuleBuildProfile=Custom` when a change needs more than one module but not the full graph. Set each required `Enable*Module` property explicitly:

```bash
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Custom -p:EnableUsersModule=true -p:EnableAppointmentsModule=true
```

## Safety expectations

Focused builds are for local inner-loop speed only. They do not replace the default full build graph. Before opening or merging a change, run either:

```bash
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=All
```

or the repository's full solution build.

Module selection stays at the MSBuild project-composition boundary. Do not add C# preprocessor symbols or `#if` blocks to hide disabled module references. If code accidentally references a disabled module at compile time, the focused build should fail clearly because that module assembly is not referenced.
