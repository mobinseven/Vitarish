# Example ADR Impact Review

## ADR Impact

Classification: ADR Recommended

Reason:

The proposed change introduces semantic reranking into the Search module. This affects the retrieval strategy and may alter relevance behavior, cost, and operational dependencies.

Related ADRs:

- ADR-0004: Use Elasticsearch for Search Retrieval

Potential conflicts:

- Possible conflict if semantic reranking bypasses the accepted Elasticsearch-based retrieval flow instead of extending it.

Recommended action:

Create a proposed ADR describing semantic retrieval/reranking as an extension to the accepted search architecture.
