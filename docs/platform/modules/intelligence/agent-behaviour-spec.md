# Agent behaviour spec

The live agent behaviour suite checks what an assistant does with the real model: which tools it calls, with which arguments, and whether a conversation keeps working after its configuration changes. It lives in `tests/Apps/Clinic.Agents.Tests/Live`.

## What runs

- The real agent runtime image, in local Docker, against the configured OpenAI model (`gpt-6-luna` by default).
- The operations assistant and the patient-page assistant composed as production composes them: the real Clinic capabilities, tool definitions, instructions and domain model ([agent domain models](agent-domain-models.md)).
- A local tool-callback endpoint in the test process. It records every tool call and answers with scripted results; the calendar conversion tool runs for real. Presentation runs for real as Main runs it: the Clinic presentations attach handles to scripted reads, `present_surface` checks the model's surface, and the collector fills it. No database, Main host or browser is involved.

## Run it

Build the runtime image, then run the suite with a key. Without the key every live test is skipped, so ordinary test runs never call a model.

```powershell
docker build -f src/Hosts/Web/apps/agent-runtime/Dockerfile -t vitarish-agent-runtime:local src/Hosts/Web
$env:VITARISH_LIVE_AGENT_OPENAI_API_KEY = '<key>'
dotnet test tests/Apps/Clinic.Agents.Tests --filter "FullyQualifiedName~Live"
```

Optional settings: `VITARISH_LIVE_AGENT_RUNTIME_IMAGE` (another image tag) and `VITARISH_LIVE_AGENT_MODEL` (another model).

## Adding a scenario

Add a test to the suite for the assistant: reset the runtime, script the results of the tools the scenario depends on, run one or more turns, and assert on the recorded tool calls and their arguments. Assert on choices and arguments that are stable for the product (the tool, a date in `yyyy-MM-dd`, a name passed to search), not on the model's wording. A scenario that needs a second turn passes the first turn's history, carried over with `AgentConversationHistory.CarryOver` when it simulates a configuration change.

A presentation scenario asserts that the model called `present_surface` successfully and that the collected surface's data model holds the scripted records while its components hold none of their values; script reads in the shape the real tool serializes (the Operations tools write PascalCase).

A domain-understanding scenario (`ProcedureUnderstandingBehaviourTests`) runs one theory over several phrasings of the same request, in English and Persian, and asserts the tool the request needs. Add phrasings users actually use rather than paraphrases of the domain model's own words.

Each run calls the model, so keep scenarios few and specific. Run the suite after changing an assistant's instructions, tools or tool schemas, and after changing the agent runtime or the history carry-over.
