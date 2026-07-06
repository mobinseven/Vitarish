using FluentAssertions;
using Vitarish.Sea.Api;
using ContractProcedureReadDto = Vitarish.Sea.Contracts.Responses.ProcedureReadDto;
using ContractProcedureWriteDto = Vitarish.Sea.Contracts.Responses.ProcedureWriteDto;
using DomainProcedureReadDto = Vitarish.Sea.Domain.Dto.ProcedureReadDto;
using DomainProcedureWriteDto = Vitarish.Sea.Domain.Dto.ProcedureWriteDto;

namespace Vitarish.Sea.Tests;

public sealed class SeaContractApplicationMapperTests
{
    [Fact]
    public void Convert_maps_same_shaped_read_dtos_without_manual_case()
    {
        DomainProcedureReadDto source = new()
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Procedure",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "user-1",
            UpdatedAt = DateTime.UtcNow.AddMinutes(1),
            UpdatedBy = "user-2"
        };
        source.SetField("Procedure description", nameof(source.Description));

        ContractProcedureReadDto result = SeaContractApplicationMapper.Convert<ContractProcedureReadDto>(source);

        result.Id.Should().Be(source.Id);
        result.ProjectId.Should().Be(source.ProjectId);
        result.Name.Should().Be(source.Name);
        result.Description.Should().Be(source.Description);
        result.CreatedAt.Should().Be(source.CreatedAt);
        result.CreatedBy.Should().Be(source.CreatedBy);
        result.UpdatedAt.Should().Be(source.UpdatedAt);
        result.UpdatedBy.Should().Be(source.UpdatedBy);
    }

    [Fact]
    public void Convert_maps_contract_write_dtos_to_domain_dtos_without_manual_case()
    {
        ContractProcedureWriteDto source = new()
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Procedure",
            Description = "Procedure description"
        };

        DomainProcedureWriteDto result = SeaContractApplicationMapper.Convert<DomainProcedureWriteDto>(source);

        result.Id.Should().Be(source.Id);
        result.ProjectId.Should().Be(source.ProjectId);
        result.Name.Should().Be(source.Name);
        result.Description.Should().Be(source.Description);
    }
}
