-- Commerce module SQL views
-- Uses sp_executesql pattern to create or replace views safely

DECLARE @sql NVARCHAR(MAX);

-- vw_StoreStats
SET @sql = N'
CREATE OR ALTER VIEW vw_StoreStats AS
SELECT
    s.Id AS StoreId,
    COUNT(DISTINCT p.Id) AS TotalProducts,
    COUNT(DISTINCT CASE WHEN p.Status = ''Published'' AND p.DeletedAt IS NULL THEN p.Id END) AS PublishedProducts,
    COUNT(DISTINCT o.Id) AS TotalOrders,
    COUNT(DISTINCT CASE WHEN o.Status = ''Pending'' AND o.DeletedAt IS NULL THEN o.Id END) AS PendingOrders,
    ISNULL(SUM(CASE WHEN o.DeletedAt IS NULL THEN o.TotalAmount ELSE 0 END), 0) AS TotalRevenue,
    MAX(CASE WHEN o.DeletedAt IS NULL THEN o.CreatedAt END) AS LastOrderAt
FROM Stores s
LEFT JOIN Products p ON p.TenantId = s.TenantId AND p.DeletedAt IS NULL
LEFT JOIN Orders o ON o.TenantId = s.TenantId AND o.DeletedAt IS NULL
WHERE s.DeletedAt IS NULL
GROUP BY s.Id, s.TenantId;
';
EXEC sp_executesql @sql;

-- vw_OrderStats
SET @sql = N'
CREATE OR ALTER VIEW vw_OrderStats AS
SELECT
    o.Id AS OrderId,
    COUNT(oi.Id) AS ItemCount,
    o.TotalAmount
FROM Orders o
LEFT JOIN OrderItems oi ON oi.OrderId = o.Id AND oi.DeletedAt IS NULL
WHERE o.DeletedAt IS NULL
GROUP BY o.Id, o.TotalAmount;
';
EXEC sp_executesql @sql;
