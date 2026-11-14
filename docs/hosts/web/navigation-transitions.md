# Browser navigation transitions

Owner: Web shared UI; Authentication and Journey own their navigation adapters.

The presentation protocol and its bounds live in
[`transitionContext.ts`](../../../src/Hosts/Web/shared/ui/src/navigation/transitionContext.ts).
Its hints cannot select a destination, represent a session or authorize access.
Authentication request bodies and return-path contracts remain independent of
these hints. Never put the envelope inside a return path or a ticket fragment.

Mount `NavigationTransitionDocument` before the host's application content and
give it the existing request CSP nonce and source-owned recovery routes. Its
critical styles and bootstrap are nonced; server markup already exposes safe
recovery links if JavaScript cannot run. The bootstrap selects static localized
content through attributes, rather than replacing text before hydration.
Covered application content starts inert and is released by the presentation
lifecycle, never by authentication policy in UI code.

`NavigationTransitionReadiness` connects initial session discovery to shell
font/layout readiness. Pending automatic Auth and Journey work participates
separately. Widget queries, images and assistant responses are not readiness
participants. Required forms and server-owned security notices must yield an
interaction outcome; background refetch must preserve mounted form state.

Use `carryNavigationTransition` only at existing approved first-party navigation
sites, after the destination owner has selected the target. It adds the envelope
only when crossing origins and leaves same-origin continuity to tab-local
presentation state. Do not wrap arbitrary links, OAuth destinations or the
general application router. Use the shared stripping operation wherever a
location becomes an API return-path argument; stripping preserves the remaining
raw path, query encoding and fragment, including unsafe inputs that the server
must still reject.

Completion, interaction, cancellation and failure clear presentation storage.
Late asynchronous completions must check their attempt before navigating. A
timeout or uncertain mutation result offers fresh entry; it must not repeat a
ticket redemption or continuation completion. Presentation abort signals do not
make mutations safe to retry.

The optional versioned envelope permits mixed Web versions: missing or rejected
hints start local feedback without changing navigation authority. Cross-hop
clock continuity requires every participating host to support the same version.
UI/Auth own this compatibility window; retire any navigation-only fallback when
all browser consumers adopt the document capability and the full boundary proof
passes. Ordinary data loading indicators remain local.

The two-host runner's test ingress routes the landing hostname to the actual
landing app and other clinic/authentication hostnames to the owner app, retaining
the public origins and host-only cookies. Internal listener addresses belong to
[`two-host-e2e-artifacts.mjs`](../../../src/Hosts/Web/scripts/two-host-e2e-artifacts.mjs).
They are not authentication origins. This makes platform-home UI handoffs part
of real-host proof rather than a clinic page rendered under the landing name.

Follow [Web validation and capture gates](../../../src/Hosts/Web/AGENTS.md) and
[browser spec ownership](../../../src/Hosts/Web/tests/AGENTS.md). The
`navigation-transition` capture flow includes the source departure and a frame
with client chunks held back before hydration. Inspect captures and recordings;
static checks and settled screenshots alone cannot establish continuity.

Rollback reverts the Web presentation and adapters together. This capability
adds no backend schema, API operation, cookie scope, application configuration
key or dependency. Keep the existing authentication deployment and rollback
requirements in [ADR-0021](../../architecture/decisions/0021-central-browser-authentication.md).
No production deployment or production verification is implied by local proof.
