-- Commerce module SQL views

-- vw_StoreStats
CREATE OR REPLACE VIEW "Commerce"."vw_StoreStats" AS
WITH ProductStats AS (
    SELECT
        p."TenantId",
        COUNT(p."Id") AS "TotalProducts",
        COUNT(CASE WHEN p."Status" = 'Published' THEN p."Id" END) AS "PublishedProducts"
    FROM "Commerce"."Products" p
    WHERE p."DeletedAt" IS NULL
    GROUP BY p."TenantId"
),
OrderStats AS (
    SELECT
        o."TenantId",
        COUNT(o."Id") AS "TotalOrders",
        COUNT(CASE WHEN o."Status" = 'Pending' THEN o."Id" END) AS "PendingOrders",
        COALESCE(SUM(o."TotalAmount"), 0) AS "TotalRevenue",
        MAX(o."CreatedAt") AS "LastOrderAt"
    FROM "Commerce"."Orders" o
    WHERE o."DeletedAt" IS NULL
    GROUP BY o."TenantId"
)
SELECT
    s."Id" AS "StoreId",
    COALESCE(ps."TotalProducts", 0) AS "TotalProducts",
    COALESCE(ps."PublishedProducts", 0) AS "PublishedProducts",
    COALESCE(os."TotalOrders", 0) AS "TotalOrders",
    COALESCE(os."PendingOrders", 0) AS "PendingOrders",
    COALESCE(os."TotalRevenue", 0) AS "TotalRevenue",
    os."LastOrderAt"
FROM "Commerce"."Stores" s
LEFT JOIN ProductStats ps ON ps."TenantId" = s."TenantId"
LEFT JOIN OrderStats os ON os."TenantId" = s."TenantId"
WHERE s."DeletedAt" IS NULL;

-- vw_OrderStats
CREATE OR REPLACE VIEW "Commerce"."vw_OrderStats" AS
SELECT
    o."Id" AS "OrderId",
    COUNT(oi."Id") AS "ItemCount",
    o."TotalAmount"
FROM "Commerce"."Orders" o
LEFT JOIN "Commerce"."OrderItems" oi ON oi."OrderId" = o."Id" AND oi."DeletedAt" IS NULL
WHERE o."DeletedAt" IS NULL
GROUP BY o."Id", o."TotalAmount";
