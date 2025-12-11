using Vitarish.Files.Application.Abstractions;
using Vorn.Entities;

namespace Vitarish.Files.Tests;

/// <summary>
/// Unit tests for the IFileInformationService interface behavior.
/// </summary>
/// <remarks>
/// The IFileInformationService interface is a marker interface that extends IEntityService.
/// These tests verify the contract expectations for implementations.
/// </remarks>
public sealed class FileInformationServiceInterfaceTests
{
    [Fact]
    public void IFileInformationService_InheritsFromIEntityService()
    {
        // Assert
        typeof(IFileInformationService)
            .Should().Implement<IEntityService<FileInformationDto, FileInformationDescriptorDto>>();
    }

    [Fact]
    public void IFileInformationService_IsInterface()
    {
        // Assert
        typeof(IFileInformationService).IsInterface.Should().BeTrue();
    }
}
