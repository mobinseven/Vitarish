using FluentAssertions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Models;

namespace Vitarish.Sea.Tests;

public sealed class ProjectDescriptorTests
{
    [Fact]
    public void Template_is_unfiltered_when_descriptor_is_created_without_a_filter()
    {
        new ProjectDescriptorDto().Template.Should().BeNull();
        new ProjectDescriptor().Template.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Template_filter_remains_explicit_when_provided(bool template)
    {
        new ProjectDescriptorDto { Template = template }.Template.Should().Be(template);
        new ProjectDescriptor { Template = template }.Template.Should().Be(template);
    }
}
