# Design Rules

These rules define how the Vitarish product should feel and look. They are ideas, not implementation details. Update this file to change platform-wide design policy.

The skill reads these rules and finds the most compatible options provided by the UI framework to fulfill them. Do not put framework-specific code, class names, token values, or numbers here.

---

## Surface Philosophy

Surfaces should feel like frosted glass — translucent, catching ambient light, creating gentle depth without heaviness. Content should float above the background, never sitting flat on it. Each layer of depth should feel intentional, as if the interface is a stack of thin, luminous panes.

The background itself should feel alive but calm — subtle color gradients that breathe, not static flat fills. When a surface overlaps another, the relationship should be visible through translucency, not through hard edges or thick borders.

Surfaces should never feel heavy, opaque, or industrial. They should feel weightless, like morning mist over water.

---

## Color Philosophy

Color should feel organic, pulled from nature, not synthetic or digital. The palette should be muted and restrained — color as punctuation, not decoration. A single accent should draw the eye with purpose, never competing with other colors for attention.

Semantic meaning should drive color choice. Success, danger, warning, and information each have a distinct emotional tone. These tones should be felt instantly, without reading a label.

Color should adapt to its environment. The same concept — success, danger, primary action — should feel equally natural in bright daylight and deep night. Dark mode is not inverted light mode; it is a different lighting condition with its own character.

Raw, unpolluted color should be rare. Most surfaces and text should live in desaturated, neutral territory. When color appears, it should feel like a deliberate brushstroke on a calm canvas.

---

## Typography Philosophy

Text should breathe. Generous line height and letter spacing create calm, readable passages that invite the eye rather than demand it. Dense, compressed text blocks feel hostile.

Hierarchy should be felt through weight and size contrast, not through color or decoration. A heading announces itself through presence, not through bright colors or underlines.

Typography should feel like a quiet, confident voice — never shouting, never whispering. The largest text should feel monumental but not aggressive. The smallest text should feel delicate but never illegible.

A component should use at most two typographic voices: one for emphasis, one for body. More than that creates visual noise.

---

## Space Philosophy

Whitespace is not emptiness — it is a design element. Space should feel generous, intentional, and restful. It separates concerns, creates hierarchy, and gives content room to exist without crowding.

Spacing should feel proportional and consistent. When one section uses generous padding, adjacent sections should match that generosity. Inconsistent spacing feels like a disorganized room.

The page should feel like it has natural margins — content should never touch the edges of the viewport. There should always be a sense of boundary between the interface and the world outside it.

Elements within a group should feel related through proximity. Elements that are unrelated should feel separated through space, not through lines or borders.

---

## Depth Philosophy

Depth creates hierarchy. The most important content should feel closest to the user, while supporting content recedes gently into the background. This feeling of depth comes from translucency, blur, shadow, and layering — not from z-index tricks or hard edges.

Shadows should feel soft, ambient, and natural — like light diffused through frosted glass, not like harsh spotlights. A shadow should suggest that an element is gently elevated, not that it is hovering aggressively.

When elements lift on interaction — a card rising on hover, a button pressing down — the motion should feel physical but delicate, like touching a soap bubble that yields but does not pop.

---

## Motion Philosophy

Motion should feel fluid, natural, and subtle. Transitions should be barely perceptible — the eye should register that something changed without consciously watching it happen.

Fast interactions (hover, focus, press) should feel instant and responsive. Slower transitions (elevation changes, page transitions) should feel smooth and unhurried. Neither should ever feel sluggish or jarring.

Motion should never be decorative. Every animation must serve a purpose: guiding attention, confirming an action, or maintaining spatial context. Animated elements that do not serve a purpose are visual noise.

The interface should respect the user's motion preferences. When a user has requested reduced motion, the interface should comply gracefully — functioning perfectly without any animation.

---

## Interaction Philosophy

Feedback should feel alive. When a user hovers, clicks, or focuses, the interface should respond immediately — not with dramatic effects, but with subtle acknowledgment that the input was received.

Interactive elements should feel tappable and responsive, like physical buttons with just enough give to feel real. They should never feel like static images that happen to respond to clicks.

Disabled states should feel genuinely inactive — visually quieter, perceptibly unreachable, but not invisible. A disabled element should communicate that it exists but cannot be used right now.

Focus should be visible and obvious. Every interactive element should have a clear focus indicator that makes keyboard navigation feel safe and predictable, never guessing where focus currently lives.

---

## Dark Mode Philosophy

Dark mode is not light mode with inverted colors. It is a different lighting condition — like moving from a sunlit room to a candlelit one. The mood changes, the relationships between elements shift, and the character of the interface transforms.

In dark mode, surfaces should become more transparent, allowing the deep background to show through. This creates depth without heaviness. Light mode feels solid and bright; dark mode feels deep and luminous.

Borders should soften and recede in dark mode. Shadows should become subtler. The interface should feel like it emerges from darkness rather than sitting on top of it.

Text should remain perfectly readable but should feel warmer and softer than its light mode counterpart. Harsh white-on-black feels clinical; the goal is comfortable, ambient readability.

---

## Clean Minimal Philosophy

Every element on the screen should earn its place. If an element does not serve a clear purpose — guiding the user, providing information, enabling action — it should be removed.

Minimalism is not emptiness. It is the removal of everything that is not essential, so that what remains can breathe and be understood immediately. The interface should feel uncluttered without feeling sparse.

Decorative elements — gradients, shadows, borders, ornaments — should be used with extreme restraint. When they appear, they should feel inevitable, not added. If removing a decorative element does not change the user's understanding, the element should not exist.

The product should feel calm, focused, and purposeful. It should never feel busy, noisy, or overwhelming.

---

## Prohibited Aesthetics

The product must never feel:

- **Industrial or utilitarian.** No harsh borders, no dense data tables without breathing room, no cramped layouts.
- **Playful or whimsical.** No rounded-everything, no bright multi-color palettes, no bouncy animations.
- **Flat or two-dimensional.** No surfaces without depth, no elements without hierarchy, no uniform visual weight.
- **Heavy or oppressive.** No dark backgrounds without translucency, no thick borders, no solid opaque panels.
- **Digital or synthetic.** No neon colors, no hard gradients, no pixel-perfect grid rigidity.
- **Cluttered or busy.** No crowded layouts, no competing visual elements, no information overload on a single screen.
- **Generic or template-like.** No stock UI patterns without adaptation, no default-styled components shipped to production.

---

## Opt-Out Principles

These rules are the default. A component may deviate when there is a specific, articulable reason that overrides the rule for that particular case.

A valid reason names the rule being overridden and explains why the component genuinely cannot follow it. "It looks better this way" is not a valid reason.

The rules remain the source of truth. Deviations are exceptions, not new defaults.
