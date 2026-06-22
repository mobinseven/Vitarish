# Scope Location Rules

Classify each test plan by owning scope before placing it.

## App-owned

Use when the test protects an app user journey, UI, app module, app-specific API, or app-specific workflow.

```txt
docs/apps/{app-slug}/modules/{module-slug}/tests/
```

## Platform-owned

Use when the test protects a shared platform service, module, provider, architecture boundary, infrastructure component, or cross-app capability.

```txt
docs/platform/modules/{module-slug}/tests/
```

## Cross-scope

Place the document under the primary owner and list related apps/modules in metadata.

Do not duplicate the same test plan in multiple folders.
