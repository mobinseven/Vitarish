# Design Rules

These rules define how the React Web host should feel and behave visually. They are design principles, not implementation details.

This file intentionally contains no component APIs, class names, package names, CSS snippets, token values, or file paths. Concrete implementation belongs in `component-patterns.md` and reset/removal details belong in `heroui-v3.2.1-off-the-shelf-reset.md`.

The current platform visual policy is: **use the UI framework's off-the-shelf visual language by default.** Custom styling is allowed only when it is explicitly requested, documented, and justified.

---

## Journey Feeling First

UI work starts from the user's journey state.

A journey state describes what the user is trying to do and how the user is likely to feel at that moment: uncertain, safe, blocked, confident, warned, focused, comparing, recovering, waiting, exploring, or finished.

The design system's job is to translate that feeling into the right interface affordance. A feeling does not automatically imply a custom visual treatment. A feeling should first influence:

- which component is chosen,
- which state the component communicates,
- which action is primary,
- which message is shown,
- which semantic structure is used,
- which accessibility behavior is required,
- how much information is shown,
- and how layout supports the user's next step.

The mapping is:

```txt
journey state -> user feeling -> design principle -> framework component -> off-the-shelf styling
```

The feeling is preserved; the visual implementation remains framework-native.

---

## Framework-Native Fidelity

The UI should feel like it belongs to the framework being used. A developer familiar with the framework should recognize the interface as standard, predictable, and easy to maintain.

A framework-native component is not unfinished. Default styling is a valid design outcome when the policy says the framework's visual language is the product's visual language.

The product should avoid a second local visual identity unless that identity is explicitly approved. The interface may express product meaning through copy, information architecture, domain concepts, and workflow quality, not through an invented visual skin.

---

## Surface Philosophy

Surfaces should make information feel organized, calm, and trustworthy. A surface creates containment and hierarchy: it tells the user which content belongs together and which content requires attention.

Surfaces should not compete with content. They should feel stable and familiar, not theatrical. Their role is to support comprehension, not to create atmosphere.

Depth should be understandable through the framework's own surface model. Do not invent a separate depth language just to make a surface feel special. When a journey state needs more importance, choose the right component, placement, copy, and state before considering visual customization.

A surface should answer: "What is this group of information, and what can I do with it?"

---

## Color Philosophy

Color should communicate semantic meaning. Success, danger, warning, information, selection, disabled state, and primary action should be immediately understandable.

Color should be restrained. Most screens should be readable and calm, with color reserved for status, action, and navigation cues.

Color should not become decoration. When color appears, it should help the user understand state, priority, or action. If removing a color does not reduce understanding, the color probably should not be there.

Theme behavior should follow the framework's built-in semantics. Light mode and dark mode should feel like normal framework themes, not like two locally invented skins.

---

## Typography Philosophy

Text should feel clear, calm, and readable. Typography creates hierarchy through semantic structure, concise wording, and consistent emphasis.

Headings should orient the user. Body text should explain what matters. Labels should reduce uncertainty. Error text should help the user recover.

Typography should not become branding by itself. Avoid creating a local typography voice through excessive casing, tracking, custom weights, or decorative heading treatments.

A component should use as few typographic voices as possible. More voices create noise and make the user's next step harder to identify.

---

## Space Philosophy

Whitespace is how the UI explains relationships. Related items should sit together. Separate tasks should be visibly separated. A page should give the user enough room to understand what is happening without feeling sparse.

Space should support the journey state:

- focused tasks need a narrow, direct path,
- comparison tasks need clear alignment,
- review tasks need scannable grouping,
- recovery tasks need proximity between the problem and the fix,
- exploration tasks need clear navigation and progressive disclosure.

Spacing is structural, not decorative. Do not use spacing to create a branded look.

---

## Hierarchy and Depth Philosophy

Hierarchy should tell the user what matters first, what is supporting information, and what action is available next.

Depth is a hierarchy tool, not an ornament. The most important content should feel easier to find, not louder or more decorated.

Prefer hierarchy through component choice, ordering, headings, grouping, disabled/pending states, and clear actions. Avoid inventing local shadows, layers, effects, or elevation systems.

---

## Motion Philosophy

Motion should confirm change, preserve context, or show progress. It should not exist to decorate the interface.

Fast interactions should feel responsive. Waiting states should feel honest and calm. Reduced-motion preferences must be respected through framework and browser behavior.

Do not create a local animation language for ordinary UI components. The framework's built-in motion is the default.

---

## Interaction Philosophy

Interactive elements should feel predictable. The user should know what can be clicked, what is disabled, what is pending, what failed, and what succeeded.

Interaction feedback should come from the framework's built-in states first. Hover, focus, press, disabled, pending, invalid, and selected states should remain accessible and consistent.

A journey feeling such as confidence, caution, or recovery should change the component state, copy, and action structure before it changes the visual style.

---

## State Feedback Philosophy

Every meaningful state should be expressed clearly:

- loading,
- empty,
- invalid,
- disabled,
- selected,
- pending,
- warning,
- success,
- danger,
- not found,
- unauthenticated,
- unauthorized,
- unavailable.

State feedback should be close to the thing it explains. It should help the user decide what to do next.

Do not invent custom status visuals when the framework already provides status components or state props.

---

## Dark Mode Philosophy

Dark mode should be the framework's dark mode. It should preserve readability, hierarchy, and state semantics without becoming a separate local theme.

Dark mode is not a place to add extra atmosphere. It should feel standard, accessible, and consistent with the rest of the framework.

Theme choice may express a user's preference, but it must not create a custom product visual identity unless explicitly approved.

---

## Clean Minimal Philosophy

Every element should earn its place. Minimalism is not emptiness; it is the removal of unnecessary visual and structural noise so the user's next step is easier to see.

If a screen feels weak, first improve the journey mapping, copy, component choice, grouping, and accessibility. Do not immediately add decoration.

The best visual change is often deleting custom styling and letting the framework component communicate the state.

---

## Prohibited Aesthetics

The product must not feel:

- **Custom-skinned by accident.** No local visual language unless explicitly approved.
- **Decorated to compensate for weak structure.** Fix the journey mapping instead.
- **Atmospheric without purpose.** No mood effects that do not help task completion.
- **Patchworked.** Do not mix old custom visual classes with framework defaults.
- **Over-designed.** Do not add custom visual systems for ordinary components.
- **Inconsistent.** Do not let one feature invent a style that other features must learn.
- **Accessibility-hostile.** No visual cleanup may remove labels, focus, error handling, or semantics.

---

## Opt-Out Principles

These rules are the default.

A component may deviate only when there is a specific, articulable requirement that cannot be satisfied through framework components, framework props, semantic HTML, or structural layout.

A valid reason names the rule being overridden and explains why the component genuinely cannot follow it. "Looks better", "more premium", "matches the old design", or "the page feels empty" are not valid reasons.

Deviations are exceptions, not new defaults. They must not introduce a reusable custom visual language unless a future accepted decision explicitly changes the platform policy.
