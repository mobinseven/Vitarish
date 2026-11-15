# Agent domain models

An assistant understands a user's own words only when it knows what the user's things are, how they relate and what their states mean. [ADR-0023](../../../architecture/decisions/0023-agent-domain-models.md) makes each app declare that once, as a domain model, and has Intelligence turn it into instruction text per composed assistant.

## Where things live

| Piece | Location |
|---|---|
| Domain-model type, validation and rendering | `src/Modules/Intelligence/Application/Agents/AgentDomainModel.cs` (`AgentDomainModel`, `AgentDomainModelInstructions`) |
| Profile hook | `AgentProfileBuilder.SetDomainModel(model, order)` in `src/Modules/Intelligence/Agents/AgentProfiles.cs`; `AgentProfileComposer` renders it from the exposed tools |
| Clinic domain model | `src/Apps/Clinic/Agents/ClinicAssistantDomainModel.cs`, set by both Clinic profiles in `AssistantProfiles.cs` |
| Clinic procedure reads | `src/Apps/Clinic/Infrastructure/Assistant/Capabilities/Procedures` (`PatientProcedureRecords` is the one reader; tools exist for the patient page and the practice workspace) |

An assistant built without the profile composer calls `AgentDomainModelInstructions.Render(model, toolNames)` with its own tools and adds the text to its instructions.

## Declaring a domain model

- One concept per thing users talk about. `Meaning` is one or two short sentences in the app's words.
- `Synonyms` are the other words users say, in every supported language, including the words on the app's screens. A word may belong to one concept only; validation rejects a word claimed twice, because one word must lead to one concept.
- `Relations` are short clauses ("Belongs to one procedure of one patient."). `States` give each state's meaning to the user and its words. `NotToConfuseWith` names the nearby concepts the model mixes up.
- `Operations` bind a verb to tools in preference order. List the tool of every assistant that can perform the verb; the rendered text names the one the composed assistant exposes. A verb with no exposed tool is rendered as unavailable, with `whenUnavailable` telling the user where to go.
- Model-level `Rules` cover how to read common questions (for example what "latest state" means).

The operations are the app's coverage matrix: a verb with an empty tool list is a deliberate gap, not an omission. Add the tool to the operation when the gap is filled.

## Keeping it true

- An app agent test asserts that every tool the model names is a registered tool of the app (`tests/Apps/Clinic.Agents.Tests/DomainModelTests.cs`), and that each composed assistant renders the right tools.
- Tool names, descriptions and result fields use the domain model's words; module names never appear in what the model reads.
- After changing a domain model, a tool, its schema or its result shape, run the [agent behaviour spec](agent-behaviour-spec.md). Add a scenario with varied phrasings in each language when you add a concept or an operation.
