using Vitarish.Interfaces.Base.Common;

namespace Vitarish.Interfaces.Base.Tests.Common;

public class AppRoutesTests
{
    [Fact]
    public void AdminShiftCreateForDay_WithoutDayOfWeek_UsesBaseRoute()
    {
        AppRoutes.Appointments.AdminShiftCreateForDay().Should().Be(AppRoutes.Appointments.AdminShiftCreate);
    }

    [Fact]
    public void AdminShiftCreateForDay_WithDayOfWeek_AppendsSegment()
    {
        AppRoutes.Appointments.AdminShiftCreateForDay((int)DayOfWeek.Wednesday)
            .Should()
            .Be("/Admin/Shifts/New/3");
    }

    [Fact]
    public void PatientEvent_ComposesNestedRoute()
    {
        AppRoutes.Medical.PatientEvent("patient-1", "procedure-2", "event-3")
            .Should()
            .Be("/Medical/Patients/patient-1/procedure-2/event-3");
    }

    [Fact]
    public void TemplateEvent_ComposesNestedRoute()
    {
        AppRoutes.Sea.TemplateEvent("project-1", "procedure-2", "event-3")
            .Should()
            .Be("/sea/Admin/Settings/Projects/Templates/project-1/procedure-2/event-3");
    }

    [Fact]
    public void UnauthorizedWithRequestedPath_AddsEncodedQuery()
    {
        AppRoutes.Account.UnauthorizedWithRequestedPath("/Medical/Patients?id=42")
            .Should()
            .Be("/Account/Unauthorized?RequestedPath=%2FMedical%2FPatients%3Fid%3D42");
    }

    [Fact]
    public void LegacyAppointmentsRoutes_RemainAvailable()
    {
        AppRoutes.Appointments.LegacyReservations.Should().Be("/Appointments");
        AppRoutes.Appointments.LegacyAdminReservations.Should().Be("/Admin/Appointments");
    }
}
