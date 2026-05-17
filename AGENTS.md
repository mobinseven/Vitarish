Follow these architecture rules:

- One responsibility per unit.
- One active context/state model.
- Minimize knowledge between components.
- Use abstractions to hide implementation details.
- Separate state-changing actions from read-only queries.
- Validate inputs and handle failures safely.
- Make features extensible without rewriting stable code.
- Protect core logic from unnecessary change.
- The most CRITICAL rule: Keep the design simple. Keep it simple stupid.  
- Use one source of truth for shared data and rules.

## Required orientation step

Before adding or moving code, read `SOLUTION_MAP.md`. It is the compact source map for this repository and includes placement rules such as: routable main-app pages belong in `src/Hosts/Main/Pages/`, while module `Interface/Components` folders are for reusable UI components, dialogs, grids, and plugin UI, not pages.
