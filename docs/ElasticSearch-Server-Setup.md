# Elasticsearch server setup for Vitarish Search

This guide covers the Search-module-related Elasticsearch configuration that is required or recommended.

## 1) Required server access

Vitarish Search expects a reachable Elasticsearch node and valid credentials (if security is enabled).

Application settings are under `Vitarish:ElasticSearch`:

```json
{
  "Vitarish": {
    "ElasticSearch": {
      "Url": "http://localhost:9200",
      "Username": "app_user",
      "Password": "app_password",
      "DefaultIndexPrefix": "vitarish-"
    }
  }
}
```

- `Url`: Elasticsearch endpoint used by the Search module.
- `Username` / `Password`: optional basic auth credentials.
- `DefaultIndexPrefix`: prefix applied to all logical search index names.

## 2) Native inference policy (Elasticsearch-managed only)

Vitarish does **not** manage external embedding/inference providers for Search.
If semantic retrieval is enabled, inference must be configured natively in Elasticsearch and referenced by endpoint ID.

```json
{
  "Vitarish": {
    "ElasticSearch": {
      "EnableSemanticRetrieval": true,
      "UseNativeInferenceOnly": true,
      "InferenceEndpointId": "vitarish-semantic-inference"
    }
  }
}
```

### Warning behavior

Search logs warnings when:

- `EnableSemanticRetrieval = true` and `UseNativeInferenceOnly = false`.
- `EnableSemanticRetrieval = true` and `InferenceEndpointId` is missing/empty.

When semantic retrieval is disabled (`EnableSemanticRetrieval = false`), these warnings are intentionally not emitted.

## 3) Sea module exact-match-first declaration

`SeaProjectsSearchProvider` declares exact-match-first policy for `Project.Name` via indexed field `title.keyword`.

- Provider: `sea.projects`
- Field: `title.keyword`
- Priority tier: `High`

This declaration is exposed through Search policy reports (`ISearchService.GetIndexPolicyReports()`), so other modules can decide where/how to apply the policy.
