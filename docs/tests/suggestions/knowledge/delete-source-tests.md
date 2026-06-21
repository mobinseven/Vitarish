# Suggested Tests: Knowledge Source Deletion

## Application command tests

### DeleteKnowledgeSourceCommand_OwnerDeletesExistingSource

Verify that an owner with a valid tenant, owner identity, corpus ID, and source ID deletes the source from the corpus and returns a successful command result.

### DeleteKnowledgeSourceCommand_RejectsUnauthorizedOwner

Verify that a missing owner identity is rejected before mutation, and add the authorization-denied case when role/owner membership rules become available as a distinct application policy.

### DeleteKnowledgeSourceCommand_RejectsMissingCorpusOrSource

Verify that a missing corpus returns a corpus-not-found failure and an existing corpus with a missing source returns a source-not-found failure without mutating corpus source state.

## Infrastructure cleanup tests

### KnowledgeSourceRepository_DeleteSource_RemovesDependentCorpusRecords

Verify that deleting a source removes the source, cascaded documents/chunks, and questions whose expected chunk IDs reference deleted chunks in the same unit of work.

### KnowledgeService_DeleteSource_RemovesSearchIndexEntries

Verify that source deletion also calls the indexer delete operation using tenant, corpus, and source filters so stale indexed chunks are not retrievable.

## Owner UI tests

### KnowledgeSourceDeleter_ConfirmsBeforeDelete

Verify that the delete action is visible only when the owner surface has permission and sources exist, and that canceling the confirmation dialog does not invoke deletion.

### KnowledgeSourceDeleter_RendersBusySuccessAndFailureStates

Verify that the owner surface disables delete while busy, shows the success message after deletion, refreshes the source list, and renders the failure message when the command fails.
