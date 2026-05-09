/// <summary>
/// Mapping extension methods for VitarishCommerceDomain entities.
/// These methods provide convenient mapping between Entity types and their DTOs.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly global::Vitarish.Commerce.Domain.Mappers.EntityMapper Mapper = new();

    public static global::Vitarish.Commerce.Domain.Dto.StoreReadDto ToDto(this global::Vitarish.Commerce.Domain.Entities.Store entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Commerce.Domain.Entities.Store ToDomain(this global::Vitarish.Commerce.Domain.Dto.StoreWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.StoreWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.StoreReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.StoreDescriptor ToDomain(this global::Vitarish.Commerce.Domain.Dto.StoreDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.ProductReadDto ToDto(this global::Vitarish.Commerce.Domain.Entities.Product entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Commerce.Domain.Entities.Product ToDomain(this global::Vitarish.Commerce.Domain.Dto.ProductWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.ProductWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.ProductReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.ProductDescriptor ToDomain(this global::Vitarish.Commerce.Domain.Dto.ProductDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.ProductVariantReadDto ToDto(this global::Vitarish.Commerce.Domain.Entities.ProductVariant entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Commerce.Domain.Entities.ProductVariant ToDomain(this global::Vitarish.Commerce.Domain.Dto.ProductVariantWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.ProductVariantWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.ProductVariantReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.ProductVariantDescriptor ToDomain(this global::Vitarish.Commerce.Domain.Dto.ProductVariantDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CategoryReadDto ToDto(this global::Vitarish.Commerce.Domain.Entities.Category entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Commerce.Domain.Entities.Category ToDomain(this global::Vitarish.Commerce.Domain.Dto.CategoryWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CategoryWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.CategoryReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.CategoryDescriptor ToDomain(this global::Vitarish.Commerce.Domain.Dto.CategoryDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.OrderReadDto ToDto(this global::Vitarish.Commerce.Domain.Entities.Order entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Commerce.Domain.Entities.Order ToDomain(this global::Vitarish.Commerce.Domain.Dto.OrderWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.OrderWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.OrderReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.OrderDescriptor ToDomain(this global::Vitarish.Commerce.Domain.Dto.OrderDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.OrderItemReadDto ToDto(this global::Vitarish.Commerce.Domain.Entities.OrderItem entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Commerce.Domain.Entities.OrderItem ToDomain(this global::Vitarish.Commerce.Domain.Dto.OrderItemWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.OrderItemWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.OrderItemReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.OrderItemDescriptor ToDomain(this global::Vitarish.Commerce.Domain.Dto.OrderItemDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CartReadDto ToDto(this global::Vitarish.Commerce.Domain.Entities.Cart entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Commerce.Domain.Entities.Cart ToDomain(this global::Vitarish.Commerce.Domain.Dto.CartWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CartWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.CartReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.CartDescriptor ToDomain(this global::Vitarish.Commerce.Domain.Dto.CartDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CartItemReadDto ToDto(this global::Vitarish.Commerce.Domain.Entities.CartItem entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Commerce.Domain.Entities.CartItem ToDomain(this global::Vitarish.Commerce.Domain.Dto.CartItemWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CartItemWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.CartItemReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.CartItemDescriptor ToDomain(this global::Vitarish.Commerce.Domain.Dto.CartItemDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CustomerProfileReadDto ToDto(this global::Vitarish.Commerce.Domain.Entities.CustomerProfile entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Commerce.Domain.Entities.CustomerProfile ToDomain(this global::Vitarish.Commerce.Domain.Dto.CustomerProfileWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Commerce.Domain.Dto.CustomerProfileWriteDto ToWrite(this global::Vitarish.Commerce.Domain.Dto.CustomerProfileReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Commerce.Domain.Models.CustomerProfileDescriptor ToDomain(this global::Vitarish.Commerce.Domain.Dto.CustomerProfileDescriptorDto dto) => Mapper.ToDomain(dto);
}
