/// <summary>
/// Mapping extension methods for VitarishCommerceDomain entities.
/// These methods provide convenient mapping between Entity types and their DTOs.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly global::Vitarish.Commerce.Domain.Mappers.EntityMapper Mapper = new();

    public static global::Vitarish.Commerce.Domain.Dto.StoreReadDto ToRead(this global::Vitarish.Commerce.Domain.Entities.Store entity) => Mapper.ToRead(entity);

    public static global::Vitarish.Commerce.Domain.Entities.Store ToEntity(this global::Vitarish.Commerce.Domain.Dto.StoreWriteDto dto) => Mapper.ToEntity(dto);

    public static global::Vitarish.Commerce.Domain.Dto.StoreWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.StoreReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.StoreDescriptor ToDescriptor(this global::Vitarish.Commerce.Domain.Dto.StoreDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static global::Vitarish.Commerce.Domain.Dto.ProductReadDto ToRead(this global::Vitarish.Commerce.Domain.Entities.Product entity) => Mapper.ToRead(entity);

    public static global::Vitarish.Commerce.Domain.Entities.Product ToEntity(this global::Vitarish.Commerce.Domain.Dto.ProductWriteDto dto) => Mapper.ToEntity(dto);

    public static global::Vitarish.Commerce.Domain.Dto.ProductWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.ProductReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.ProductDescriptor ToDescriptor(this global::Vitarish.Commerce.Domain.Dto.ProductDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static global::Vitarish.Commerce.Domain.Dto.ProductVariantReadDto ToRead(this global::Vitarish.Commerce.Domain.Entities.ProductVariant entity) => Mapper.ToRead(entity);

    public static global::Vitarish.Commerce.Domain.Entities.ProductVariant ToEntity(this global::Vitarish.Commerce.Domain.Dto.ProductVariantWriteDto dto) => Mapper.ToEntity(dto);

    public static global::Vitarish.Commerce.Domain.Dto.ProductVariantWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.ProductVariantReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.ProductVariantDescriptor ToDescriptor(this global::Vitarish.Commerce.Domain.Dto.ProductVariantDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CategoryReadDto ToRead(this global::Vitarish.Commerce.Domain.Entities.Category entity) => Mapper.ToRead(entity);

    public static global::Vitarish.Commerce.Domain.Entities.Category ToEntity(this global::Vitarish.Commerce.Domain.Dto.CategoryWriteDto dto) => Mapper.ToEntity(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CategoryWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.CategoryReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.CategoryDescriptor ToDescriptor(this global::Vitarish.Commerce.Domain.Dto.CategoryDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static global::Vitarish.Commerce.Domain.Dto.OrderReadDto ToRead(this global::Vitarish.Commerce.Domain.Entities.Order entity) => Mapper.ToRead(entity);

    public static global::Vitarish.Commerce.Domain.Entities.Order ToEntity(this global::Vitarish.Commerce.Domain.Dto.OrderWriteDto dto) => Mapper.ToEntity(dto);

    public static global::Vitarish.Commerce.Domain.Dto.OrderWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.OrderReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.OrderDescriptor ToDescriptor(this global::Vitarish.Commerce.Domain.Dto.OrderDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static global::Vitarish.Commerce.Domain.Dto.OrderItemReadDto ToRead(this global::Vitarish.Commerce.Domain.Entities.OrderItem entity) => Mapper.ToRead(entity);

    public static global::Vitarish.Commerce.Domain.Entities.OrderItem ToEntity(this global::Vitarish.Commerce.Domain.Dto.OrderItemWriteDto dto) => Mapper.ToEntity(dto);

    public static global::Vitarish.Commerce.Domain.Dto.OrderItemWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.OrderItemReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.OrderItemDescriptor ToDescriptor(this global::Vitarish.Commerce.Domain.Dto.OrderItemDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CartReadDto ToRead(this global::Vitarish.Commerce.Domain.Entities.Cart entity) => Mapper.ToRead(entity);

    public static global::Vitarish.Commerce.Domain.Entities.Cart ToEntity(this global::Vitarish.Commerce.Domain.Dto.CartWriteDto dto) => Mapper.ToEntity(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CartWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.CartReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.CartDescriptor ToDescriptor(this global::Vitarish.Commerce.Domain.Dto.CartDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CartItemReadDto ToRead(this global::Vitarish.Commerce.Domain.Entities.CartItem entity) => Mapper.ToRead(entity);

    public static global::Vitarish.Commerce.Domain.Entities.CartItem ToEntity(this global::Vitarish.Commerce.Domain.Dto.CartItemWriteDto dto) => Mapper.ToDescriptor(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CartItemWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.CartItemReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.CartItemDescriptor ToDescriptor(this global::Vitarish.Commerce.Domain.Dto.CartItemDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CustomerProfileReadDto ToRead(this global::Vitarish.Commerce.Domain.Entities.CustomerProfile entity) => Mapper.ToRead(entity);

    public static global::Vitarish.Commerce.Domain.Entities.CustomerProfile ToDescriptor(this global::Vitarish.Commerce.Domain.Dto.CustomerProfileWriteDto dto) => Mapper.ToDescriptor(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CustomerProfileWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.CustomerProfileReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.CustomerProfileDescriptor ToDescriptor(this global::Vitarish.Commerce.Domain.Dto.CustomerProfileDescriptorDto dto) => Mapper.ToDescriptor(dto);
}
