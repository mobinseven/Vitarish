# Skill routing notes

`AGENTS.md` owns the route table. Load only skills that decide the current task.

- **Planning:** `platform-change-planning` + touched boundary skills.
- **Implementation:** `platform-delivery-loop` + touched boundary skills + `platform-test-validation` for non-trivial changes.
- **Diagnosis/review:** owning boundary skill + validation; add delivery only for authorized edits or final delivery review.

Layout skill applies to route/page shells, `<main>`, viewport geometry, shared widths/gutters, and outer scroll ownership. Use `platform-react-feature` instead for feature-internal spacing, grids, dialogs, or pane scrolling when the outer layout contract is unchanged.
