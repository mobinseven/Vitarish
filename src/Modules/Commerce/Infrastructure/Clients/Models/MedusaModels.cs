namespace Vitarish.Commerce.Infrastructure.Clients.Models;

internal record MedusaListResponse<T>
{
    [JsonPropertyName("products")]
    public IReadOnlyList<T>? Products { get; init; }

    [JsonPropertyName("orders")]
    public IReadOnlyList<T>? Orders { get; init; }

    [JsonPropertyName("product_categories")]
    public IReadOnlyList<T>? ProductCategories { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; }
}

internal record MedusaProduct
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("handle")]
    public string Handle { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "draft";

    [JsonPropertyName("variants")]
    public IReadOnlyList<MedusaProductVariant>? Variants { get; init; }
}

internal record MedusaProductVariant
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("sku")]
    public string? Sku { get; init; }

    [JsonPropertyName("inventory_quantity")]
    public int InventoryQuantity { get; init; }

    [JsonPropertyName("prices")]
    public IReadOnlyList<MedusaPrice>? Prices { get; init; }
}

internal record MedusaPrice
{
    [JsonPropertyName("amount")]
    public long Amount { get; init; }

    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; init; } = string.Empty;
}

internal record MedusaOrder
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "pending";

    [JsonPropertyName("fulfillment_status")]
    public string FulfillmentStatus { get; init; } = "not_fulfilled";

    [JsonPropertyName("total")]
    public long Total { get; init; }

    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; init; } = string.Empty;

    [JsonPropertyName("items")]
    public IReadOnlyList<MedusaLineItem>? Items { get; init; }
}

internal record MedusaLineItem
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("variant_id")]
    public string? VariantId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }

    [JsonPropertyName("unit_price")]
    public long UnitPrice { get; init; }
}

internal record MedusaCategory
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("handle")]
    public string Handle { get; init; } = string.Empty;

    [JsonPropertyName("parent_category_id")]
    public string? ParentCategoryId { get; init; }
}

internal record MedusaCart
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; init; }

    [JsonPropertyName("items")]
    public IReadOnlyList<MedusaLineItem>? Items { get; init; }
}

internal record MedusaCartResponse
{
    [JsonPropertyName("cart")]
    public MedusaCart? Cart { get; init; }
}
