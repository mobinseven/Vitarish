# Intelligence agent conversation transport

The authoritative public execution contract is:

`POST /api/modules/intelligence/agent-conversations`

The request identifies an agent (`agentKey`), an opaque subject scope (`subjectKind` and `subjectId`), the user message, and an optional opaque conversation ID. Continuation uses only that conversation ID and the encrypted server-side history. The response contains the run ID, conversation ID, and an agent-owned opaque structured result. Feature providers own the result schema and patient authorization; the Intelligence transport does not inspect or persist that payload.

The Clinic patient-agent provider uses `agentKey=clinic.patient` and `subjectKind=patient`. It preserves Clinic authorization, tool execution, structured parsing, action proposal persistence, and action confirmation APIs. Scope mismatches are mapped to the same not-found response as the compatibility Clinic route.

The former `POST /api/modules/clinic/patients/{patientId}/agent` operation is compatibility-only. It delegates to the same provider and is scheduled for removal in the next major API release after all supported clients have migrated. OpenAPI specifications and generated clients are derived from the backend contracts; generated files must not be edited by hand.
