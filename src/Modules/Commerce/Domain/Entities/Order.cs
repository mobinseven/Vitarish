namespace Vitarish.Commerce.Domain.Entities;

public class Order : Entity
{
    public int OrderNumber
    {
        get;
        set => SetField(ref field, value, nameof(OrderNumber));
    }

    public string MedusaId
    {
        get;
        set => SetField(ref field, value, nameof(MedusaId));
    } = string.Empty;

    public string CustomerId
    {
        get;
        set => SetField(ref field, value, nameof(CustomerId));
    } = string.Empty;

    public OrderStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    } = OrderStatus.Pending;

    public FulfillmentStatus FulfillmentStatus
    {
        get;
        set => SetField(ref field, value, nameof(FulfillmentStatus));
    } = FulfillmentStatus.NotFulfilled;

    public decimal TotalAmount
    {
        get;
        set => SetField(ref field, value, nameof(TotalAmount));
    }

    public string CurrencyCode
    {
        get;
        set => SetField(ref field, value, nameof(CurrencyCode));
    } = "usd";

    public RefundStatus RefundStatus
    {
        get;
        set => SetField(ref field, value, nameof(RefundStatus));
    } = RefundStatus.None;

    public string RefundNote
    {
        get;
        set => SetField(ref field, value, nameof(RefundNote));
    } = string.Empty;

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];

    public OrderStats Stats { get; set; } = new();
}
