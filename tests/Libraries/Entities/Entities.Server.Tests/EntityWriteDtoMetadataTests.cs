namespace Entities.Server.Tests;

public sealed class EntityWriteDtoMetadataTests
{
    [Fact]
    public void SetField_WhenMetadataWasAssignedOnGrandChild_PreservesExistingMetadata()
    {
        GrandChildWriteDto dto = new()
        {
            Metadata = new Dictionary<string, object?>
            {
                [nameof(GrandChildWriteDto.FirstName)] = "Ada",
                [nameof(GrandChildWriteDto.LastName)] = "Lovelace"
            }
        };

        dto.VisitReason = "Initial consultation";

        Assert.Equal("Ada", dto.Metadata[nameof(GrandChildWriteDto.FirstName)]);
        Assert.Equal("Lovelace", dto.Metadata[nameof(GrandChildWriteDto.LastName)]);
        Assert.Equal("Initial consultation", dto.Metadata[nameof(GrandChildWriteDto.VisitReason)]);
    }

    [Fact]
    public void SetField_WhenMetadataValuesWasInitializedOnGrandChild_PreservesExistingMetadata()
    {
        Metadata metadataValues = new(new Dictionary<string, object?>
        {
            [nameof(GrandChildWriteDto.FirstName)] = "Ada",
            [nameof(GrandChildWriteDto.LastName)] = "Lovelace"
        });
        GrandChildWriteDto dto = new()
        {
            MetadataValues = metadataValues
        };

        dto.VisitReason = "Initial consultation";

        Assert.Equal("Ada", dto.Metadata[nameof(GrandChildWriteDto.FirstName)]);
        Assert.Equal("Lovelace", dto.Metadata[nameof(GrandChildWriteDto.LastName)]);
        Assert.Equal("Initial consultation", dto.Metadata[nameof(GrandChildWriteDto.VisitReason)]);
    }

    [Fact]
    public void GetField_WhenMetadataWasAssignedOnGrandChild_ReturnsExistingMetadataValue()
    {
        GrandChildWriteDto dto = new()
        {
            Metadata = new Dictionary<string, object?>
            {
                [nameof(GrandChildWriteDto.FirstName)] = "Ada"
            }
        };

        Assert.Equal("Ada", dto.FirstName);
    }

    private record ParentWriteDto : EntityWriteDto
    {
        public Guid? AvatarFileId { get => GetField<Guid?>(); set => SetField(value); }
    }

    private record ChildWriteDto : ParentWriteDto
    {
        public string? FirstName { get => GetField<string>(); set => SetField(value); }

        public string? LastName { get => GetField<string>(); set => SetField(value); }
    }

    private sealed record GrandChildWriteDto : ChildWriteDto
    {
        public string? VisitReason { get => GetField<string>(); set => SetField(value); }
    }
}
