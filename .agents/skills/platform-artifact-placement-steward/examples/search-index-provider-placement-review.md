# Example: Search Index Provider Placement Review

## Artifact Placement

Artifact: `ElasticsearchSearchIndexProvider.cs`

Classification: Infrastructure adapter

Owning scope: Search module

Preferred location: `src/Modules/Search/Infrastructure/...`

Reason: The provider implements a search application contract using Elasticsearch, a concrete technology. Application logic should depend on the contract, not on Elasticsearch directly.

Allowed dependencies:

- Search Application contracts
- Search Domain models/DTOs as appropriate
- Elasticsearch client package
- logging/options abstractions

Disallowed dependencies:

- Host pages
- App-specific UI
- unrelated module internals
- domain dependency on Elasticsearch

Related tests:

- Unit tests for query shaping if logic is isolated
- Integration/hot scenario against Elasticsearch for indexing/search behavior

Documentation impact:

- Update `docs/platform/modules/search/...` if behavior changes retrieval semantics.

ADR impact:

- Required if this establishes Elasticsearch as the accepted retrieval/index technology.
- Not required for ordinary implementation under an existing ADR.
