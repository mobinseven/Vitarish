using C = Vitarish.Commerce.Contracts;
using D = Vitarish.Commerce.Domain.Dto;
using DE = Vitarish.Commerce.Domain.Enums;

namespace Vitarish.Commerce.Api;

internal static class CommerceContractMapper
{
    public static D.ProductDescriptorDto ToDomain(C.ProductDescriptorDto dto) => new() { Id = dto.Id, Skip = dto.Skip, Take = dto.Take, Search = dto.Search, CreatedFrom = dto.CreatedFrom, CreatedTo = dto.CreatedTo, CategoryId = dto.CategoryId, Status = MapProductStatus(dto.Status) };
    public static D.OrderDescriptorDto ToDomain(C.OrderDescriptorDto dto) => new() { Id = dto.Id, Skip = dto.Skip, Take = dto.Take, Search = dto.Search, CreatedFrom = dto.CreatedFrom, CreatedTo = dto.CreatedTo, CustomerId = dto.CustomerId, OrderNumber = dto.OrderNumber, Status = MapOrderStatus(dto.Status) };
    public static D.StoreWriteDto ToDomain(C.StoreWriteDto dto) => new() { Id = dto.Id, Name = dto.Name, CurrencyCode = dto.CurrencyCode, MedusaBaseUrl = dto.MedusaBaseUrl, AdminApiToken = dto.AdminApiToken, StorefrontPublishableKey = dto.StorefrontPublishableKey };

    public static C.ProductReadDto ToContract(D.ProductReadDto dto) => new() { Id = dto.Id, CreatedAt = dto.CreatedAt, CreatedBy = dto.CreatedBy, UpdatedAt = dto.UpdatedAt, UpdatedBy = dto.UpdatedBy, DeletedAt = dto.DeletedAt, DeletedBy = dto.DeletedBy, MedusaId = dto.MedusaId, Handle = dto.Handle, Title = dto.Title, Description = dto.Description, Status = MapProductStatus(dto.Status), Url = dto.Url };
    public static C.OrderReadDto ToContract(D.OrderReadDto dto) => new() { Id = dto.Id, CreatedAt = dto.CreatedAt, CreatedBy = dto.CreatedBy, UpdatedAt = dto.UpdatedAt, UpdatedBy = dto.UpdatedBy, DeletedAt = dto.DeletedAt, DeletedBy = dto.DeletedBy, OrderNumber = dto.OrderNumber, CustomerDisplay = dto.CustomerDisplay, Status = MapOrderStatus(dto.Status), FulfillmentStatus = MapFulfillmentStatus(dto.FulfillmentStatus), RefundStatus = MapRefundStatus(dto.RefundStatus), TotalAmount = dto.TotalAmount, CurrencyCode = dto.CurrencyCode };
    public static C.StoreReadDto ToContract(D.StoreReadDto dto) => new() { Id = dto.Id, CreatedAt = dto.CreatedAt, CreatedBy = dto.CreatedBy, UpdatedAt = dto.UpdatedAt, UpdatedBy = dto.UpdatedBy, DeletedAt = dto.DeletedAt, DeletedBy = dto.DeletedBy, Name = dto.Name, CurrencyCode = dto.CurrencyCode, MedusaBaseUrl = dto.MedusaBaseUrl };
    public static C.MerchantRecoveryCartReadDto ToContract(D.MerchantRecoveryCartReadDto dto) => new() { CartId = dto.CartId, CustomerDisplay = dto.CustomerDisplay, CustomerId = dto.CustomerId, HasContact = dto.HasContact, EstimatedValue = dto.EstimatedValue, CreatedAt = dto.CreatedAt, LastActivityAt = dto.LastActivityAt, RecoveryStatus = MapCartRecoveryStatus(dto.RecoveryStatus), RecoveryNote = dto.RecoveryNote };

    private static DE.ProductStatus? MapProductStatus(C.ProductStatus? status) => status is null ? null : Enum.Parse<DE.ProductStatus>(status.Value.ToString());
    private static C.ProductStatus MapProductStatus(DE.ProductStatus status) => Enum.Parse<C.ProductStatus>(status.ToString());
    private static DE.OrderStatus? MapOrderStatus(C.OrderStatus? status) => status is null ? null : Enum.Parse<DE.OrderStatus>(status.Value.ToString());
    private static C.OrderStatus MapOrderStatus(DE.OrderStatus status) => Enum.Parse<C.OrderStatus>(status.ToString());
    private static C.FulfillmentStatus MapFulfillmentStatus(DE.FulfillmentStatus status) => Enum.Parse<C.FulfillmentStatus>(status.ToString());
    private static C.RefundStatus MapRefundStatus(DE.RefundStatus status) => Enum.Parse<C.RefundStatus>(status.ToString());
    public static DE.CartRecoveryStatus MapCartRecoveryStatus(C.CartRecoveryStatus status) => Enum.Parse<DE.CartRecoveryStatus>(status.ToString());
    private static C.CartRecoveryStatus MapCartRecoveryStatus(DE.CartRecoveryStatus status) => Enum.Parse<C.CartRecoveryStatus>(status.ToString());
}
