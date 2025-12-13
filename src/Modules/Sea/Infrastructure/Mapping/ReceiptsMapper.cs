namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class ReceiptsMapper
{
    public partial ReceiptDto ToDto(Receipt src);
    public partial Receipt ToDomain(ReceiptDto src);
    public partial ReceiptDescriptor ToDomain(ReceiptDescriptorDto src);
}
