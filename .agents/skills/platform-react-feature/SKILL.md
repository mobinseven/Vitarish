---
name: platform-react-feature
description: Designs and implements React feature boundaries, generated-client wrappers, hooks, browser APIs, async lifecycle, localized accessible UI, and cleanup behavior. Use for React pages, components, hooks, MediaRecorder/WebSocket/browser integration, HeroUI, or user-facing copy. Do not use for backend-only work.
---

# Platform React Feature

## Purpose

Keep React UI workflow-shaped, localized, accessible, and insulated from transport/provider details. This skill owns browser and presentation behavior, not backend policy or wire contracts.

## Use when

- React routes, feature folders, components, hooks, forms, actions, browser permissions/media APIs, realtime clients, HeroUI composition, localization, or accessibility change.
- Generated API output needs a feature-owned wrapper or hook.

## Do not use

- The task changes only backend, contracts, generated output, or server configuration.

## Procedure

1. Keep route/page files as composition surfaces; place workflow logic in the narrowest feature owner.
2. Wrap generated clients and native transport/browser APIs in feature-owned hooks/adapters when lifecycle, invalidation, state, cleanup, or error mapping is needed.
3. Define one explicit UI state model for idle, connecting, active, completing, completed, failed, canceled, and unsupported states that apply.
4. Preserve the latest editable user state; avoid stale-closure overwrites when async segments/results arrive.
5. Release tracks, recorders, sockets, subscriptions, timers, and pending work on stop, failure, navigation, owner change, and unmount.
6. Keep provider/protocol details out of visual components. Map stable errors to localized presentation copy.
7. Put all user-facing text in the central localization owner; cover supported locales and use central formatting helpers.
8. Preserve keyboard access, labels, focus, status announcements, disabled reasons, contrast, and read-only/permission behavior.
9. Prefer raw HeroUI/HeroUI Pro composition before a local wrapper; promote to shared Web UI only for proven generic reuse.
10. Add hook/component tests for state transitions, user edits, failure preservation, cleanup, permissions, localization, and access gating.

## Hard stops

- UI components directly implement provider protocol or depend on backend/server types.
- Async results can overwrite user edits or leak resources.
- New user-facing text bypasses localization or an action lacks accessible state/labeling.

## Handoff

Route contract changes to `platform-transport-contract`, trust behavior to `platform-trust-boundary`, and verification to `platform-test-validation`.
