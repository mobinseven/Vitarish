# Knowledge owner management collection suggested tests

## Collection hierarchy query
- Seed a tenant with two corpora, multiple sources per corpus, and multiple documents plus chunks per source.
- Send `KnowledgeOwnerCollectionQuery` for that tenant and owner.
- Assert the read model contains corpora with nested sources and source files, and each file has the expected chunk count.

## Invalid tenant and owner input
- Send `KnowledgeOwnerCollectionQuery` with a null, empty, and whitespace tenant id.
- Send the query with a missing owner id.
- Assert the handler returns `KnowledgeOwnerCollectionReadModel.Empty` and does not throw accidental EF/runtime exceptions.

## Empty collection safety
- Seed a corpus with no sources and a source with no documents.
- Assert empty source and file lists are represented as empty collections rather than null values.

## Explicit projection fields
- Seed corpus description, active state, source type, imported timestamp, original location, document title, language, text length, and chunks.
- Assert `KnowledgeOwnerCollectionProjector` preserves each value in explicit read-model fields instead of requiring parsing from display labels.
