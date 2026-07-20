---
name: platform-react-feature
description: Owns React feature boundaries, hooks, generated-client wrappers, browser APIs, async lifecycle, localized accessible UI, and cleanup. Skip backend-only work.
---

# Platform React Feature

## Rules

1. Keep route/page files as composition; place workflow logic in the narrowest feature owner.
2. Wrap generated clients and browser/transport APIs when lifecycle, invalidation, state, cleanup, or error mapping is needed.
3. Use one explicit UI state model for applicable idle/connecting/active/completing/completed/failed/canceled/unsupported states.
4. Preserve latest user edits; prevent stale async results from overwriting them.
5. Release media tracks, recorders, sockets, subscriptions, timers, and pending work on every terminal path, navigation, owner change, and unmount.
6. Keep provider/protocol details out of visual components; localize stable errors and all user-facing text through central owners.
7. Preserve keyboard/focus behavior, labels, announcements, disabled reasons, contrast, permissions, and read-only access.
8. Prefer direct HeroUI/HeroUI Pro composition; promote only proven generic reuse to shared Web UI.
9. Test state transitions, edit preservation, failures, cleanup, permissions, localization, and access gating.

## Stop

- Visual components implement provider protocol or depend on server types.
- Async work can overwrite edits or leak resources.
- Copy bypasses localization or actions lack accessible state/labels.
