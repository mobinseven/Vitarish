# MudBlazor UI Guideline Framework (Agent-Friendly)

Purpose: Build clean, modern, minimal, professional UIs using MudBlazor (Material Design), optimized for admin dashboards, chat interfaces, and SaaS apps. Responsive by **one consistent rule set** (no separate mobile design), prioritizing **single-column clarity**.

---

## 0) Non-Negotiables
- Prefer **clarity over decoration**: fewer colors, fewer sizes, fewer component variants.
- Default to **one primary action per view**.
- Use **semantic structure**: page title → sections → content → actions.
- Prefer **predictable patterns** (consistent placement, consistent copy, consistent states).
- Accessibility is baseline: keyboard-first, visible focus, adequate contrast, readable sizing.

---

## 1) Design Principles
### Visual
- **Neutral-first** surfaces; accent color only for emphasis.
- **High information density without clutter**: whitespace + hierarchy > borders everywhere.
- **Consistent shape**: one border radius scale, one shadow/elevation scale.

### Interaction
- **Immediate feedback** for every action (loading, success, error, disabled reasons).
- **Progressive disclosure**: show advanced controls only when needed.
- **Forgiving UX**: prevent errors via constraints; when errors happen, guide recovery.

### MudBlazor alignment
- Use MudBlazor theming (`MudTheme`) as the single source of truth.
- Prefer MudBlazor components over custom HTML controls.
- Extend via wrappers + tokens (not ad-hoc overrides).

---

## 2) Design Tokens (defaults; override allowed)

### Color Tokens
Use a small set; keep most UI neutral.

- `--color-primary`: brand / primary CTA / selected states
- `--color-secondary`: secondary emphasis (rare)
- `--color-bg`: app background
- `--color-surface`: cards, panels, sheets
- `--color-surface-2`: elevated surface (dialogs, popovers)
- `--color-text`: main text
- `--color-text-muted`: secondary text
- `--color-border`: subtle dividers
- `--color-success`, `--color-warning`, `--color-error`, `--color-info`: status colors

Rules:
- Don’t use status colors as decoration; only meaning.
- Keep background/surface differences subtle (avoid high-contrast panels everywhere).
- Use primary color mainly for: primary button, active nav item, selected chip/tab, key links.

### Typography Tokens
- Base font size: `16px` (minimum). Avoid smaller than `14px` for dense admin tables only.
- Scale (example):
  - Page title: `H4/H5`
  - Section header: `H6`
  - Body: `Body1`
  - Supporting: `Body2`
  - Caption: `Caption` (avoid for critical info)

Rules:
- Max 2 weights in a view (Regular + Medium/Bold).
- Use sentence case for most UI text.
- Keep line length readable: ideal 60–90 chars on desktop content areas.

### Spacing & Layout Tokens
Base spacing unit: `8px`
- `xs: 4`, `sm: 8`, `md: 16`, `lg: 24`, `xl: 32`, `2xl: 48`

Rules:
- Internal padding on surfaces: `16px` default.
- Vertical spacing between sections: `24px` default.
- Use consistent gutters across the app.

### Shape & Elevation
- Border radius: choose one default (e.g., `8px`) and one smaller (e.g., `4px`).
- Elevation: minimal.
  - `0`: flat content
  - `1–2`: cards
  - `3–4`: app bar / sticky headers
  - `6+`: dialogs only

### Motion Tokens
- Small transitions: `120–180ms`
- Medium: `200–280ms`
- Easing: standard ease-out for enter, ease-in for exit
- Prefer fade + slight translate (no bouncy animations)

---

## 3) Responsive Layout Rules (Single System)
### Core layout
- Use a **single primary column** for most pages.
- Content max width:
  - Chat: `720–840px`
  - Admin/SaaS forms: `960–1200px` (depending on density)
- Center content column; keep consistent horizontal padding:
  - Mobile: `16px`
  - Tablet+: `24px`

### Grid usage
- Use `MudGrid` for controlled splits (only when it improves scanning).
- Rule: on small screens, everything becomes `xs="12"` (single column).

### Page structure (recommended)
1. Top app bar (optional actions on right)
2. Page header: title + description + primary action
3. Filter/search row (if needed)
4. Content (cards/tables/forms)
5. Footer actions (if long forms)

---

## 4) MudBlazor Theme Setup
Define theme once. Avoid per-component styling drift.

Theme rules:

* Never hardcode colors in components except in rare, documented exceptions.
* Use `Color="Color.Primary"` etc., not hex strings in markup.
* Use spacing utilities/classes consistently (`pa-4`, `ma-2`, etc.) or centralized CSS tokens.

---

## 5) Component Standards (MudBlazor)

### 5.1 Buttons

**Variants**

* Primary CTA: `Variant="Filled"` + `Color="Primary"`
* Secondary: `Variant="Outlined"` + `Color="Primary"` (or default)
* Tertiary: `Variant="Text"` + neutral (for low-emphasis actions)
* Destructive: `Color="Error"`; confirm via dialog or undo pattern

**Rules**

* One primary per view (page or dialog).
* Buttons should be **verb-first**: “Save changes”, “Invite user”, “Send”.
* Loading: disable + show spinner (prefer button-local loading).
* Icon-only buttons must have tooltip + aria-label.

**Do**

* Use `StartIcon` for context (Save, Add, Send).
* Keep button groups aligned: primary on the right, cancel on the left.

**Don’t**

* Two primary filled buttons side-by-side.
* Use `Error` color for non-destructive emphasis.

---

### 5.2 Cards / Surfaces (`MudCard`, `MudPaper`)

**Rules**

* Cards are for grouping; not for complex nested navigation.
* Default padding `16px`, consistent header spacing.
* Use subtle elevation (`1–2`), avoid heavy shadows.

**Do**

* Use card header for title + optional action (kebab menu).
* Make entire card clickable only when it truly represents a single navigation target.

**Don’t**

* Put full forms with many fields in a card unless it’s a small “settings panel”.

---

### 5.3 Dialogs / Modals (`MudDialog`)

**Structure**

* Title (H6) → content → actions
* Actions: `Cancel` (text/outlined) + primary (filled)

**Rules**

* Trap focus; `Esc` closes when safe.
* Don’t stack dialogs.
* Prefer inline panels or drawers for non-critical secondary flows.

**Destructive confirm**

* Use clear title: “Delete project?”
* Describe consequence briefly.
* Primary action label: “Delete” (not “OK”).

---

### 5.4 Forms (`MudForm`, `MudTextField`, `MudSelect`, `MudDatePicker`)

**Layout**

* Default: label + field in one vertical flow.
* Group related fields with section headers; avoid multi-column unless it improves speed.

**Validation**

* Validate on blur and submit.
* Error text must explain how to fix.
* Focus first invalid field on submit.

**Field rules**

* Use `Immediate="true"` only for search or non-expensive validation.
* Use correct control types (selects for finite options, autocomplete for large lists).
* Provide helper text for non-obvious inputs.

**Do**

* Use `MudForm` + data annotations or explicit validators.
* Disable submit until valid for high-stakes forms; otherwise allow submit and show errors.

**Don’t**

* Placeholder-only labels.
* Long inline error paragraphs—keep it actionable.

---

### 5.5 Navigation (`MudAppBar`, `MudDrawer`, `MudNavMenu`, `MudNavLink`)

**Default patterns**

* Admin: left drawer + top app bar
* SaaS: top bar + optional drawer
* Chat: minimal top bar + conversation list (optional) + main column

**Rules**

* Highlight active route.
* Keep nav labels short and consistent.
* Collapse drawer on small screens into temporary overlay.

**Do**

* Group nav items by domain (e.g., “Manage”, “Billing”, “Settings”).
* Keep hierarchy shallow (max 1 level deep).

**Don’t**

* Hide critical pages behind a kebab-only menu.
* Use hover-only interactions for essential nav.

---

### 5.6 Tables / DataGrids (`MudTable`, `MudDataGrid`)

**Rules**

* Prioritize scanability: good padding, stable columns, clear headers.
* Provide search and filters above table, not crammed inside cells.
* Row actions: either 1–2 icon buttons or a single overflow menu.

**Responsive**

* On mobile: reduce columns; consider “card rows” representation.
* Avoid horizontal scroll unless unavoidable.

**States**

* Loading: skeleton rows
* Empty: “No results” + CTA if appropriate
* Error: inline retry

---

### 5.7 Alerts & Notifications (`MudAlert`, `MudSnackbar`)

**Rules**

* Snackbars: transient confirmations (“Saved”, “Copied”).
* Alerts: persistent issues (“Billing issue”, “Sync failed”).
* Keep messages short, action-oriented.

**Do**

* Provide “Undo” on destructive actions when feasible.
* Use severity consistently (Success/Info/Warning/Error).

**Don’t**

* Use multiple snackbars for one action.
* Use error alerts for minor validation issues (keep those inline).

---

### 5.8 Loading (`MudProgressCircular`, `MudProgressLinear`, `MudSkeleton`)

**Rules**

* Prefer skeletons for content areas.
* Use linear progress for page-level loading.
* Avoid spinner-only empty screens when structure is known.

**Pattern**

* Show layout frame immediately (header/nav).
* Skeleton content while fetching.
* Replace with actual content when ready.

---

## 6) Chat Interface Patterns (MudBlazor-Friendly)

### Layout

* Main content in centered column.
* Message list scroll area + composer fixed at bottom (safe-area aware).

### Messages

* Use consistent spacing between messages.
* Differentiate roles subtly (alignment, background tint, or avatar).
* Support rich text: headings, lists, code blocks.

### Composer

* Single input row with:

  * input field
  * send button
  * attachments/options (optional)
* Enter to send; Shift+Enter newline (configurable).

### Streaming / Typing

* Show subtle indicator (ellipsis or small progress) near latest assistant message.
* Avoid large animated loaders.

### Safety/Errors

* If message fails: show inline error + “Retry” action on that message.

---

## 7) Accessibility Checklist (Required)

* Contrast: text and icons must be readable on all surfaces.
* Keyboard:

  * Tab order matches visual order.
  * All interactive components reachable without mouse.
  * `Esc` closes menus/dialogs.
* Focus:

  * Visible focus ring (never remove without replacement).
  * Focus trapped in dialogs; returned to opener on close.
* Screen readers:

  * Icon-only buttons require `aria-label`.
  * Inputs require labels; errors announced (aria-live or built-in).
* Touch targets:

  * Minimum ~40px height for primary controls.
* Reduced motion:

  * Respect reduced motion preference where possible; keep motion subtle.

---

## 8) Interaction States (Standard)

For all interactive components:

* **Default**: neutral
* **Hover**: slight surface tint or elevation change
* **Focus**: clear outline/ring (high contrast)
* **Active**: slightly pressed (reduced elevation or darker tint)
* **Disabled**: reduced opacity + no hover effects + accessible disabled semantics

---

## 9) Content & Tone (Microcopy Rules)

* Short, clear, neutral.
* Prefer verbs and outcomes:

  * “Save changes”, “Invite member”, “Download report”
* Error messages:

  * State problem + next step
  * “Couldn’t save changes. Try again.”
  * “Password must be at least 8 characters.”
* Avoid blame, sarcasm, or filler:

  * Avoid: “Oops!”, “You did something wrong”
* Confirmations:

  * Title as question, action as verb:
  * “Delete project?” → buttons “Cancel” / “Delete”

---

## 10) Documentation & Governance (For Teams)

### Structure (recommended repo layout)

* `/design-system/`

  * `tokens.md` (colors, spacing, typography, motion)
  * `components/` (buttons.md, forms.md, tables.md, dialogs.md, nav.md, chat.md)
  * `patterns/` (loading.md, empty-states.md, errors.md)
  * `a11y.md` (checklist + testing steps)
  * `copy.md` (tone + examples)

### Process

* Changes require:

  * rationale
  * before/after screenshot or description
  * accessibility impact notes
  * migration notes (if breaking)
* Versioning:

  * Patch: docs fixes
  * Minor: new component/pattern
  * Major: breaking token or component behavior changes

### PR checklist (minimum)

* Uses tokens/theme (no hardcoded colors)
* Responsive behavior verified
* Loading/empty/error states implemented
* Keyboard navigation works
* Focus visible
* Copy matches tone rules

---

## 11) Quick Defaults (Drop-in Conventions)

* Inputs: `Variant="Outlined"` everywhere unless specified
* Primary button: `Filled + Primary`
* Secondary button: `Outlined`
* Tertiary: `Text`
* Cards: elevation 1–2, padding 16
* Sections: spacing 24 between blocks
* Page max width: 960–1200; chat max 840
* Skeletons for loading; inline empty/error states with clear CTA


# Tips

## 1) Information architecture: don’t ship a “scroll wall”

* **Split long configuration screens into clear sections** (tabs/side-nav/accordions): e.g. *Account*, *User details*, *Theme*, *Typography*, *Surfaces*, *Advanced*.
* **Use progressive disclosure**: show common settings first; put tokens/advanced sliders behind “Advanced”.
* **Provide a “Search settings” field** when settings exceed one screen.
* **Keep one primary task per screen**. If “Add user” and “Theme configuration” are both present, separate them.

## 2) Layout & grid: consistent alignment beats decoration

* Use a **single layout grid** (e.g. 12-col or simple max-width + gutters) and stick to it.
* Pick one label strategy and apply everywhere:

  * **Top labels** for narrow layouts/mobile, or
  * **Left/right aligned labels** for desktop (but consistent).
* **Align control edges**: inputs, toggles, sliders, helper text, and buttons should snap to the same vertical lines.
* Use a **spacing system** (e.g. 8px steps): 8/16/24/32. Avoid “random” gaps.

## 3) Typography: legibility first, then style

* Minimum body text: **14–16px**, and avoid ultra-light gray for important text.
* Define a **type scale**: H1/H2/Body/Caption, with consistent weights and line heights.
* Don’t mix typographic styles across sections (e.g., tiny labels in one area, big labels elsewhere) unless intentional.

## 4) Localization & RTL/LTR: choose one experience and make it correct

* **Don’t mix languages** (Persian + English labels) in the same UI unless there’s a clear reason. Localize everything, or separate language-only areas.
* For RTL UIs:

  * Set the container `dir="rtl"` and ensure components are RTL-aware.
  * Keep **numbers/hex codes** LTR but visually integrated (e.g. use `dir="ltr"` only on the code field).
  * Icons and affordances (chevrons, edit pencils) should mirror appropriately.

## 5) Forms: reduce ambiguity and make actions obvious

* Every field needs:

  * **Label**, **helper text** (optional), **validation message** (when needed).
* Avoid repeated unlabeled icon rows:

  * **Icon-only actions must have tooltips** and ideally a text label if frequent (“Edit email”, “Edit phone”).
* Use clear call-to-actions:

  * One **primary** button (“Save changes” / “Create user”)
  * Secondary (“Cancel”, “Reset”)
* Show **unsaved changes** state (“You have unsaved changes”) and disable Save until something changes.

## 6) Controls: use the right input for the job

* **Sliders are bad for precise values** unless you also show:

  * Current numeric value
  * Units
  * Optional number input for exact entry
  * Clear min/max and step
* Toggles must have:

  * A **label describing the state** (“Use high contrast mode”)
  * If needed, a short explanation of what changes.

## 7) Color & theme configuration: tokens must be scannable and safe

What’s going wrong in many theme editors is “giant color bars + tiny labels”. Instead:

* Represent each color token as a row:

  * **Swatch** + **Token name** + **Editable value** (HEX/RGB) + **Copy** + **Reset**
* Group tokens semantically:

  * **Brand** (Primary/Secondary/Tertiary)
  * **Text** (Primary text/Secondary text/Disabled)
  * **Surfaces** (Background/Surface/Elevation)
  * **Status** (Success/Warning/Error/Info)
* Add **contrast checks** automatically (WCAG):

  * Warn when text on surface fails contrast.
  * Provide suggested accessible alternatives.
* Avoid putting hex text inside the colored bar unless contrast is guaranteed.

## 8) Preview: make feedback immediate and meaningful

* Provide a **live preview panel** that stays visible (sticky side panel on desktop, collapsible drawer on mobile).
* Preview should show real components and states:

  * Normal / Hover / Disabled / Focus / Error states
* Make it easy to test:

  * “Preview on Light/Dark”
  * “Simulate color-blindness” (optional but valuable)

## 9) Visual consistency: fewer borders, fewer shadows, clearer hierarchy

* Use **one elevation style** (cards/shadows) consistently—avoid heavy shadows everywhere.
* Don’t stack “card inside card inside card” unless there’s a clear hierarchy.
* Use separators sparingly; prefer **spacing and headings** for grouping.

## 10) Accessibility & usability: bake it in

* Minimum touch target: **44×44 px**.
* Strong **focus states** for keyboard navigation.
* Ensure labels are programmatically tied to inputs (screen readers).
* Avoid low-contrast placeholder-as-label patterns.
* Ensure error messages are specific and placed near the field.


