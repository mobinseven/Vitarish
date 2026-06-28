# Commerce Storefront Journey

## 1. Context

- App: Platform-wide (commerce-enabled tenants)
- Module: Commerce
- Capability: Product browsing, cart management, order placement, merchant operations
- Related apps: All commerce-enabled apps
- Related modules: Users (customer identity), Files (product images), Platform (admin provisioning)
- Product / service: Vitarish Commerce module
- Journey type: Current state with gaps
- Scenario: Customer browses a storefront, adds products to cart, and places an order; merchant manages products, orders, and store settings.
- User goal: Purchase products seamlessly; merchant manages store operations.
- Business goal: Enable tenant-scoped e-commerce with MedusaJS integration.
- Scope start: Customer opens the storefront or merchant opens the dashboard.
- Scope end: Order is placed (customer) or store operations are managed (merchant).
- Primary persona: Storefront customer (user) and store merchant (owner)
- Evidence sources: Static code review of Commerce Interface components, host pages, admin provisioning
- Confidence level: Medium

## 2. Persona Snapshot

**Name / segment:** Storefront customer

**Situation:** Browsing a tenant's product catalog to make a purchase.

**Motivation:** Find and buy products easily.

**Key needs:** Product browsing, cart management, order history.

**Constraints:** Cart functionality is placeholder; no checkout flow implemented.

**Success means:** Products are found, added to cart, and ordered without confusion.

---

**Name / segment:** Store merchant / owner

**Situation:** Managing a tenant's e-commerce store.

**Motivation:** Maintain product catalog, fulfill orders, recover abandoned carts.

**Key needs:** Product management, order management, store settings, abandoned cart recovery.

**Constraints:** MedusaJS integration is external; sync is manual.

**Success means:** Store runs smoothly; orders are fulfilled; abandoned carts are recovered.

## 3. Journey Summary

**Narrative:** A customer opens the storefront, browses published products, adds items to the cart, and views order history. A merchant manages the product catalog, processes orders, configures store settings, and recovers abandoned carts. A platform admin provisions new stores and triggers catalog/order syncs.

**Emotional arc (customer):** Start: curious (browsing) → Middle: decisive (adding to cart) → End: satisfied (order placed)

**Emotional arc (merchant):** Start: systematic (dashboard overview) → Middle: attentive (processing orders) → End: confident (store configured)

**Biggest friction point:** Cart and checkout are placeholder implementations; no actual purchase flow exists.

**Highest-value opportunity:** Complete the cart → checkout → payment flow and add product search/filter.

## 4. Journey Stages

### Customer Journey

#### Stage 1: Browse Storefront

**User goal:** Discover products available for purchase.

**User actions:** Open storefront page; browse product grid; view product details.

**Touchpoints / channels:** `StorefrontPage`, `ProductsGrid`.

**User thoughts / questions:** "What products are available? Which ones interest me?"

**Emotions:** Curious, 3, because browsing is exploratory.

**Pain points:** No product search or filter; no category navigation; no product images in grid.

**Needs:** Search, filter, category navigation, product images.

**Opportunities:** Add product search, category filter, and image thumbnails.

**Success metrics:** Products viewed per session; browse-to-cart conversion.

**Evidence:** Static code review assumption.

#### Stage 2: Cart Management

**User goal:** Add products to cart and review selections.

**User actions:** Add product to cart; open cart drawer; review items.

**Touchpoints / channels:** `CartDrawer`.

**User thoughts / questions:** "What's in my cart? Can I change quantities?"

**Emotions:** Neutral, 3, because cart is functional but limited.

**Pain points:** Cart is placeholder (shows empty/ready state); no quantity adjustment; no remove item; no price display.

**Needs:** Full cart CRUD; price calculation; quantity adjustment.

**Opportunities:** Implement full cart functionality with price calculation.

**Success metrics:** Cart add rate; cart completion rate.

**Evidence:** Static code review assumption.

#### Stage 3: Order Placement

**User goal:** Complete a purchase.

**User actions:** (Not yet implemented — checkout flow is missing).

**Touchpoints / channels:** (None — checkout does not exist).

**User thoughts / questions:** "How do I check out? When will this be ready?"

**Emotions:** Frustrated, 2, because checkout is not available.

**Needs:** Checkout flow, payment processing, order confirmation.

**Opportunities:** Implement checkout with payment integration.

**Success metrics:** Checkout completion rate (once implemented).

**Evidence:** Static code review assumption.

#### Stage 4: Order History

**User goal:** View past orders and their status.

**User actions:** Navigate to order history; view order list; check order status.

**Touchpoints / channels:** `OrderHistoryPage`.

**User thoughts / questions:** "What did I order? What's the status?"

**Emotions:** Satisfied, 3, if order history is clear.

**Pain points:** No order detail view; no reorder functionality; no shipment tracking.

**Needs:** Order detail, reorder, tracking.

**Opportunities:** Add order detail page with reorder and tracking.

**Success metrics:** Order history view frequency; support tickets for order status.

**Evidence:** Static code review assumption.

### Merchant Journey

#### Stage 5: Merchant Dashboard

**User goal:** Get an overview of store operations.

**User actions:** Open merchant dashboard; review order count, pending fulfillment, today's sales, abandoned carts, products.

**Touchpoints / channels:** `MerchantDashboard`.

**User thoughts / questions:** "How is the store performing? What needs attention?"

**Emotions:** Systematic, 3, because the dashboard provides entry points.

**Pain points:** No trend data; no comparison to previous periods; no alerts.

**Needs:** Trend data, period comparison, alerts.

**Opportunities:** Add trend charts and period-over-period comparison.

**Success metrics:** Dashboard view frequency; time-to-first-action.

**Evidence:** Static code review assumption.

#### Stage 6: Product Management

**User goal:** Manage the product catalog.

**User actions:** View product list; (no create/edit in current UI — list only).

**Touchpoints / channels:** `ProductsListPage`.

**User thoughts / questions:** "What products are listed? Are they published?"

**Emotions:** Neutral, 3, because list is functional.

**Pain points:** No product create/edit UI; no bulk operations; no product image management.

**Needs:** Full product CRUD, bulk operations, image management.

**Opportunities:** Add product create/edit forms, bulk status changes.

**Success metrics:** Product management task completion time.

**Evidence:** Static code review assumption.

#### Stage 7: Order Processing

**User goal:** Manage and fulfill customer orders.

**User actions:** View order list; filter by status/date; view order details; process refunds.

**Touchpoints / channels:** `OrdersListPage`.

**User thoughts / questions:** "Which orders need fulfillment? Which have refund requests?"

**Emotions:** Focused, 3, because order processing is operational.

**Pain points:** No bulk fulfillment; no shipment tracking integration; refund is manual.

**Needs:** Bulk fulfillment, tracking integration, automated refund processing.

**Opportunities:** Add bulk fulfillment actions and tracking integration.

**Success metrics:** Order fulfillment time; refund processing time.

**Evidence:** Static code review assumption.

#### Stage 8: Abandoned Cart Recovery

**User goal:** Recover abandoned carts from registered customers.

**User actions:** View recovery queue; review cart details; contact customer; mark as recovered or ignored.

**Touchpoints / channels:** `RecoveryQueuePage`.

**User thoughts / questions:** "Which carts can be recovered? What's the estimated value?"

**Emotions:** Hopeful, 3, because recovery has value.

**Pain points:** No automated recovery emails; no time-based escalation; no recovery rate tracking.

**Needs:** Automated recovery emails, escalation rules, rate tracking.

**Opportunities:** Add automated recovery campaigns and rate analytics.

**Success metrics:** Cart recovery rate; recovery revenue.

**Evidence:** Static code review assumption.

#### Stage 9: Store Settings

**User goal:** Configure store basics.

**User actions:** View store name, currency, MedusaJS base URL.

**Touchpoints / channels:** `StoreSettingsPage`.

**User thoughts / questions:** "Is my store configured correctly?"

**Emotions:** Neutral, 3, because settings are read-only.

**Pain points:** Settings are read-only in current UI; no edit capability.

**Needs:** Editable store settings.

**Opportunities:** Add store settings edit form.

**Success metrics:** Configuration accuracy.

**Evidence:** Static code review assumption.

### Platform Admin Journey

#### Stage 10: Store Provisioning

**User goal:** Provision a new commerce store for a tenant.

**User actions:** Enter store name, currency, MedusaJS URL, API keys; provision store.

**Touchpoints / channels:** `AdminStoreProvisionPage`.

**User thoughts / questions:** "Is the MedusaJS instance configured? Are the keys valid?"

**Emotions:** Careful, 4, because provisioning affects the tenant.

**Pain points:** No validation of MedusaJS URL or API keys before provisioning; no test connection.

**Needs:** Connection validation, key verification, test endpoint.

**Opportunities:** Add test connection before provisioning.

**Success metrics:** Provisioning success rate; post-provisioning errors.

**Evidence:** Static code review assumption.

#### Stage 11: Sync Management

**User goal:** Keep Vitarish and MedusaJS catalogs in sync.

**User actions:** Trigger catalog sync; trigger order sync; view sync status.

**Touchpoints / channels:** `AdminSyncStatusPage`, `AdminCommerceOverviewPage`.

**User thoughts / questions:** "Is the sync running? Did it complete? Were there errors?"

**Emotions:** Alert, 3, because sync failures affect store operations.

**Pain points:** No automated sync; no error details; no sync history.

**Needs:** Automated sync scheduling, error reporting, sync history.

**Opportunities:** Add automated sync scheduling and error alerting.

**Success metrics:** Sync success rate; sync frequency; error resolution time.

**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points

1. Cart and checkout are placeholder — occurs at the most critical conversion point.
2. No product search or filter — occurs at browse stage.
3. Sync is manual with no error visibility — occurs at operational level.

### Moments that matter

- **Moment of truth:** Customer adds first product to cart — confirms the storefront is functional.
- **Moment of friction:** Customer tries to check out but the flow doesn't exist.
- **Moment of delight:** Merchant sees recovered cart revenue in the dashboard.

### Design principles implied by the journey

- Complete the core purchase flow before adding advanced features.
- Provide operational visibility for all automated processes.
- Validate external service connectivity before provisioning.

## 6. Implementation Derivation

### Required artifacts

| Artifact | Why it is needed | Journey source | Priority |
|---|---|---|---|
| Cart functionality | Core purchase flow; currently placeholder | Stage 2 | Critical |
| Checkout flow | Enables actual purchases | Stage 3 | Critical |
| Product search & filter | Helps customers find products | Stage 1 | High |
| Automated sync scheduling | Removes manual sync burden | Stage 11 | High |
| Product create/edit UI | Enables merchant catalog management | Stage 6 | High |

### Feature slices

| Slice | User value | Includes | Excludes | Acceptance signal |
|---|---|---|---|---|
| Cart CRUD | Customers can manage cart contents | Add, remove, quantity adjust, price display | Payment processing | Cart completion rate > 50% |
| Product search | Customers can find products quickly | Text search, category filter, sort | AI recommendations | Browse-to-cart conversion increases |
| Automated sync | Store stays in sync without manual intervention | Scheduled catalog/order sync, error alerts | Real-time sync | Sync error resolution time decreases |

### Domain / data implications

- Cart entity must support add/remove/quantity operations.
- Checkout requires payment gateway integration (external dependency).
- Product search requires indexing or filtering on the MedusaJS side.

### UI implications

- Cart drawer needs full CRUD controls.
- Storefront needs search bar and filter controls.
- Merchant dashboard needs trend charts.

### API / service implications

- Cart operations must integrate with MedusaJS cart API.
- Checkout must integrate with payment gateway.
- Sync must use MedusaJS webhook or polling API.

### Validation, permissions, and safety

- Cart operations require authenticated user.
- Merchant operations require owner role.
- Store provisioning requires platform admin role.

### Tests and acceptance criteria

- Cart add/remove/quantity operations persist correctly.
- Product search returns relevant results.
- Sync completes without errors.
- Store provisioning validates API keys before creation.

### Metrics

- Cart-to-checkout conversion rate.
- Product search usage rate.
- Sync success rate.
- Abandoned cart recovery rate.

## 7. Technical Detail Addendum

### Current implementation touchpoints

- `src/Modules/Commerce/Interface/Components/StorefrontPage.razor` — renders `ProductsGrid` for customer browsing.
- `src/Modules/Commerce/Interface/Components/ProductsGrid.razor` — MudGrid of published products with title and description.
- `src/Modules/Commerce/Interface/Components/CartDrawer.razor` — placeholder cart view (empty/ready state).
- `src/Modules/Commerce/Interface/Components/OrderHistoryPage.razor` — MudTable of customer orders with status and total.
- `src/Modules/Commerce/Interface/Components/Merchant/MerchantDashboard.razor` — operations dashboard with order/product/recovery stats and quick-action buttons.
- `src/Modules/Commerce/Interface/Components/Merchant/OrdersListPage.razor` — filterable order list with expandable details and refund request.
- `src/Modules/Commerce/Interface/Components/Merchant/ProductsListPage.razor` — product list with title, handle, status chip.
- `src/Modules/Commerce/Interface/Components/Merchant/RecoveryQueuePage.razor` — abandoned cart queue with recovery actions.
- `src/Modules/Commerce/Interface/Components/Merchant/StoreSettingsPage.razor` — read-only store info display.
- `src/Modules/Commerce/Interface/Components/Admin/AdminCommerceOverviewPage.razor` — platform-wide order/product counts.
- `src/Modules/Commerce/Interface/Components/Admin/AdminStoreProvisionPage.razor` — store creation form with MedusaJS config.
- `src/Modules/Commerce/Interface/Components/Admin/AdminSyncStatusPage.razor` — manual catalog/order sync triggers.
- `src/Hosts/Main/Pages/Commerce/Owners/CommerceOwnerDashboardPage.razor` — host page for merchant dashboard.
- `src/Hosts/Main/Pages/Commerce/Owners/CommerceOwnerOrdersPage.razor` — host page for order list.
- `src/Hosts/Main/Pages/Commerce/Owners/CommerceOwnerProductsPage.razor` — host page for product list.
- `src/Hosts/Main/Pages/Commerce/Owners/CommerceOwnerRecoveryPage.razor` — host page for recovery queue.
- `src/Hosts/Main/Pages/Commerce/Owners/CommerceOwnerStoreSettingsPage.razor` — host page for store settings.
- `src/Hosts/Main/Pages/Commerce/Users/CommerceUserShopPage.razor` — host page for storefront.
- `src/Hosts/Main/Pages/Commerce/Users/CommerceUserOrdersPage.razor` — host page for order history.
- `src/Hosts/Main/Pages/Commerce/PlatformAdmins/PlatformAdminCommercePage.razor` — admin commerce overview.
- `src/Hosts/Main/Pages/Commerce/PlatformAdmins/PlatformAdminCommerceProvisionPage.razor` — admin store provisioning.
- `src/Hosts/Main/Pages/Commerce/PlatformAdmins/PlatformAdminCommerceSyncPage.razor` — admin sync management.

### Issues to address

- `CartDrawer` is a placeholder showing empty/ready state; no actual cart CRUD functionality exists.
- No checkout flow; customers cannot complete purchases.
- `ProductsGrid` has no search, filter, or category navigation; products are displayed as a flat grid.
- `ProductsListPage` (merchant) is list-only with no product create/edit UI.
- `StoreSettingsPage` is read-only; merchants cannot edit store configuration.
- `AdminStoreProvisionPage` does not validate MedusaJS URL or API keys before provisioning.
- `AdminSyncStatusPage` has manual sync triggers with no automated scheduling, error reporting, or sync history.
- `RecoveryQueuePage` has no automated recovery emails or time-based escalation.
- No product images are displayed in the storefront grid.
- No order detail view for customers; only status and total are shown.
