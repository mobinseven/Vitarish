using Riok.Mapperly.Abstractions;

namespace Vitarish.Sea.Api;

[Mapper]
public partial class SeaContractDtoMapper
{
    [MapperIgnoreSource(nameof(Domain.Dto.ProjectReadDto.Url))]
    public partial Contracts.Responses.ProjectReadDto ToContract(Domain.Dto.ProjectReadDto source);

    public partial Contracts.Responses.ProjectWriteDto ToContract(Domain.Dto.ProjectWriteDto source);

    public partial Contracts.Responses.ProjectSnapshot ToContract(Domain.Dto.ProjectSnapshot source);

    [MapperIgnoreSource(nameof(Domain.Dto.ProcedureReadDto.Url))]
    public partial Contracts.Responses.ProcedureReadDto ToContract(Domain.Dto.ProcedureReadDto source);

    public partial Contracts.Responses.ProcedureWriteDto ToContract(Domain.Dto.ProcedureWriteDto source);

    [MapperIgnoreSource(nameof(Domain.Dto.EventReadDto.Url))]
    public partial Contracts.Responses.EventReadDto ToContract(Domain.Dto.EventReadDto source);

    [MapperIgnoreSource(nameof(Domain.Dto.AttachmentReadDto.Url))]
    public partial Contracts.Responses.AttachmentReadDto ToContract(Domain.Dto.AttachmentReadDto source);

    public partial Contracts.Responses.RoleReadDto ToContract(Domain.Dto.RoleReadDto source);

    public partial Contracts.Responses.UserRoleReadDto ToContract(Domain.Dto.UserRoleReadDto source);

    public partial Contracts.Responses.DatabaseStatisticsReadDto ToContract(Domain.Dto.DatabaseStatisticsReadDto source);

    public partial Domain.Dto.ProjectWriteDto ToDomain(Contracts.Responses.ProjectWriteDto source);

    public Domain.Dto.ProcedureWriteDto ToDomain(Contracts.Responses.ProcedureWriteDto source) => new()
    {
        Id = source.Id,
        ProjectId = source.ProjectId,
        Name = source.Name,
        Description = source.Description
    };
}
