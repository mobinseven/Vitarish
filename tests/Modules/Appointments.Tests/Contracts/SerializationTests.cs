using System.Text.Json;
using FluentAssertions;
using Vitarish.Appointments.Contracts.Results;
using Vitarish.Appointments.Contracts.Routes;

namespace Appointments.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    [Fact]
    public void AppointmentsApiRoutes_constants_are_correct()
    {
        AppointmentsApiRoutes.Base.Should().Be("/api/modules/appointments");
        AppointmentsApiRoutes.Reservations.Should().Be("/reservations");
        AppointmentsApiRoutes.Shifts.Should().Be("/shifts");
        AppointmentsApiRoutes.Services.Should().Be("/services");
    }

    [Fact]
    public void AppointmentsErrorCode_has_expected_values()
    {
        var values = Enum.GetValues<AppointmentsErrorCode>();
        values.Should().Contain(AppointmentsErrorCode.ValidationFailure);
        values.Should().Contain(AppointmentsErrorCode.Unauthorized);
        values.Should().Contain(AppointmentsErrorCode.Forbidden);
        values.Should().Contain(AppointmentsErrorCode.NotFound);
        values.Should().Contain(AppointmentsErrorCode.BookingConflict);
        values.Should().Contain(AppointmentsErrorCode.Timeout);
        values.Should().Contain(AppointmentsErrorCode.UnexpectedFailure);
    }

    [Fact]
    public void AppointmentsCommandResult_round_trip()
    {
        var id = Guid.NewGuid();
        var original = new AppointmentsCommandResult(true, id);
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AppointmentsCommandResult>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.Succeeded.Should().BeTrue();
        deserialized.EntityId.Should().Be(id);
    }
}
