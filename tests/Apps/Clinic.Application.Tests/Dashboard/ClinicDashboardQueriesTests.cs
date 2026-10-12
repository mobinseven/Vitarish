using FluentAssertions;
using Moq;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Enums;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Queries.Dashboard;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Entities.Contracts.Models;
using Xunit;

namespace Clinic.Application.Tests.Dashboard;

public sealed class ClinicDashboardQueriesTests
{
    [Fact]
    public void Date_range_uses_the_default_limit()
    {
        DashboardDateRange.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), null)
            .Take.Should().Be(4);
    }

    [Theory]
    [InlineData(2026, 8, 2, 2026, 8, 1, null)]
    [InlineData(2026, 8, 1, 2026, 11, 1, null)]
    [InlineData(2026, 8, 1, 2026, 8, 1, 0)]
    [InlineData(2026, 8, 1, 2026, 8, 1, 21)]
    public void Date_range_rejects_invalid_bounds_and_limits(
        int fromYear, int fromMonth, int fromDay,
        int toYear, int toMonth, int toDay,
        int? take)
    {
        Action action = () => DashboardDateRange.Create(
            new DateOnly(fromYear, fromMonth, fromDay),
            new DateOnly(toYear, toMonth, toDay),
            take);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Appointment_query_enriches_valid_patients_preserves_missing_references_and_orders_stably()
    {
        Guid earlyReservationId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid lateReservationId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Guid missingShiftId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        Guid patientId = Guid.Parse("00000000-0000-0000-0000-000000000004");

        Mock<IAccessControlledReservationManager> reservations = new(MockBehavior.Strict);
        Mock<IAccessControlledShiftManager> shifts = new(MockBehavior.Strict);
        Mock<IServiceManager> services = new(MockBehavior.Strict);
        Mock<IPatientDirectory> patients = new(MockBehavior.Strict);
        Mock<ISurgeryToolchain> surgeries = new(MockBehavior.Strict);

        reservations.Setup(value => value.GetListAsync(It.IsAny<ReservationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ReservationReadDto>>.Success(
            [
                new() { Id = lateReservationId, ShiftId = missingShiftId, Date = new DateOnly(2026, 8, 2), ReservingPartyId = "not-a-guid", Status = ReservationStatus.Confirmed },
                new() { Id = earlyReservationId, ShiftId = missingShiftId, Date = new DateOnly(2026, 8, 1), ReservingPartyId = patientId.ToString(), Status = ReservationStatus.Pending }
            ]));
        shifts.Setup(value => value.GetAsync(missingShiftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ShiftReadDto>.Failure(EntityError.NotFound("shift missing")));
        patients.Setup(value => value.FindByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientView { Id = patientId, Name = "Patient One", Tags = ["vip", "follow-up"] });

        ClinicDashboardQueries query = new(reservations.Object, shifts.Object, services.Object, surgeries.Object, patients.Object);

        IReadOnlyList<DashboardAppointmentReadModel> result = await query.ListAppointmentsAsync(
            DashboardDateRange.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), null), CancellationToken.None);

        result.Select(value => value.ReservationId).Should().Equal(earlyReservationId, lateReservationId);
        result[0].PatientDisplayName.Should().Be("Patient One");
        result[0].PatientTags.Should().Equal("vip", "follow-up");
        result[1].PatientTags.Should().BeEmpty();
        result[1].PatientId.Should().BeNull();
        result.All(value => value.StartTime is null && value.ServiceDisplayName is null).Should().BeTrue();
        shifts.Verify(value => value.GetAsync(missingShiftId, It.IsAny<CancellationToken>()), Times.Once);
        services.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Appointment_query_fails_when_schedule_dependency_is_unavailable()
    {
        Guid reservationId = Guid.NewGuid();
        Guid shiftId = Guid.NewGuid();
        Mock<IAccessControlledReservationManager> reservations = new(MockBehavior.Strict);
        Mock<IAccessControlledShiftManager> shifts = new(MockBehavior.Strict);

        reservations.Setup(value => value.GetListAsync(It.IsAny<ReservationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ReservationReadDto>>.Success(
                [new() { Id = reservationId, ShiftId = shiftId, Date = new DateOnly(2026, 8, 1), Status = ReservationStatus.Confirmed }]));
        shifts.Setup(value => value.GetAsync(shiftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ShiftReadDto>.Failure(EntityError.Unknown("dependency unavailable")));

        ClinicDashboardQueries query = new(
            reservations.Object,
            shifts.Object,
            Mock.Of<IServiceManager>(),
            Mock.Of<ISurgeryToolchain>(),
            Mock.Of<IPatientDirectory>());

        Func<Task> action = () => query.ListAppointmentsAsync(
            DashboardDateRange.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 1), 1), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
