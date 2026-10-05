using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vitarish.Medical.Api;
using Vitarish.Medical.Application.Abstractions;
using Vitarish.Medical.Contracts.Errors;
using Vitarish.Medical.Contracts.Requests;

namespace Vitarish.Medical.Tests.Api;

public sealed class MedicalApiEndpointMappingTests
{
    [Fact]
    public async Task Cancellation_maps_to_499_instead_of_a_server_error()
    {
        DefaultHttpContext context = Context();

        IResult result = MedicalApiEndpointMapping.ToHttpFailure(new MedicalError(MedicalErrorCode.Cancellation, "The request was cancelled."));
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status499ClientClosedRequest);
    }

    [Fact]
    public async Task Provider_unavailable_stays_503()
    {
        DefaultHttpContext context = Context();

        IResult result = MedicalApiEndpointMapping.ToHttpFailure(new MedicalError(MedicalErrorCode.ProviderUnavailable, "Provider is unavailable."));
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task Unexpected_failure_stays_a_server_error()
    {
        DefaultHttpContext context = Context();

        IResult result = MedicalApiEndpointMapping.ToHttpFailure(new MedicalError(MedicalErrorCode.UnexpectedFailure, "Unexpected."));
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Theory]
    [InlineData("not-a-facility-indicator")]
    [InlineData("999")]
    public async Task Invalid_facility_indicator_maps_to_400_without_querying_the_store(string facilityIndicator)
    {
        Mock<IMedicalQueryService> service = new();
        DefaultHttpContext context = Context();

        IResult result = await MedicalApiEndpointMapping.GetServiceValuationAsync(
            new GetServiceValuationRequest(Guid.NewGuid(), 2024, null, facilityIndicator),
            service.Object,
            CancellationToken.None);
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        service.Verify(
            value => value.GetServiceValuationAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<Vitarish.Medical.Domain.Enums.FacilityIndicator?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("not-a-mapping-type")]
    [InlineData("999")]
    public async Task Invalid_mapping_type_maps_to_400_without_querying_the_store(string mappingType)
    {
        Mock<IMedicalQueryService> service = new();
        DefaultHttpContext context = Context();

        IResult result = await MedicalApiEndpointMapping.GetCodeMapsAsync(
            new GetCodeMapsRequest(Guid.NewGuid(), null, mappingType),
            service.Object,
            CancellationToken.None);
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        service.Verify(
            value => value.GetCodeMapsAsync(
                It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<Vitarish.Medical.Domain.Enums.MappingType?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static DefaultHttpContext Context()
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddOptions();
        DefaultHttpContext context = new() { RequestServices = services.BuildServiceProvider() };
        context.Response.Body = new MemoryStream();
        return context;
    }
}
