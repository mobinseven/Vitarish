# Platform Test Curator

Curates required unit tests and environment-dependent hot test scenarios for scoped platform work.

This skill is designed for Vitarish-style documentation where artifacts are grouped by owning App and Module:

```txt
docs/apps/{app}/modules/{module}/tests/
docs/platform/modules/{module}/tests/
```

## Important Terminology

A **hot test** is not a normal unit/integration test that can be fully validated with a single `dotnet test` command.

A hot test is a scenario that needs runtime conditions to be meaningful, such as:

- real/containerized database
- UI/browser flow
- auth/session behavior
- file or media handling
- background worker or queue
- search/indexing delay
- network latency, timeout, outage, or partial failure
- concurrent requests
- deployment-like configuration

The skill’s main job is to analyze code and propose which scenario that code must survive.

## What It Produces

- scoped test plans
- required unit test lists
- required hot scenario lists
- suggested/deferred/rejected tests
- test implementation tasks
- execution guidance for CI, local lab, nightly, pre-release, or manual runs

## Core Rule

```txt
Test the risk. Document the scenario. Do not fake infrastructure-dependent validation as a unit test.
```

## Example Use

```txt
Use the Platform Test Curator skill to analyze this upload component and suggest required unit tests and hot test scenarios.
```

Expected result:

```txt
Required unit tests:
- validates allowed file type
- rejects oversized chunks
- maps upload state transitions

Required hot scenarios:
- browser uploads a large file while SignalR latency is injected
- upload is interrupted mid-stream and resumed/rejected safely
- storage write fails after metadata is created and cleanup/audit behavior is verified
```
