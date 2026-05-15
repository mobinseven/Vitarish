using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;

namespace Vitarish.Main.Common;

public static class AppRoutes
{
    public const string Home = "/";

    public static class Common
    {
        public const string Error = "/Error";
        public const string Privacy = "/Privacy";
    }

    public static class Account
    {
        public const string Settings = "/Settings";
        public const string SettingsName = "/Settings/Name";
        public const string SettingsEmail = "/Settings/Email";
        public const string SettingsPhone = "/Settings/Phone";
        public const string SettingsPassword = "/Settings/Password";
        public const string SettingsPasskeys = "/Settings/Passkeys";
        public const string SettingsAppearance = "/Settings/Appearance";

        public const string Login = "/Account/Login";
        public const string LoginPhone = "/Account/Login/Phone";
        public const string LoginEmail = "/Account/Login/Email";
        public const string LoginPassword = "/Account/Login/Password";
        public const string LoginPasskey = "/Account/Login/Passkey";
        public const string Logout = "/Account/Signout";
        public const string Unauthorized = "/Account/Unauthorized";

        public static string UnauthorizedWithRequestedPath(string? requestedPath) =>
            string.IsNullOrWhiteSpace(requestedPath)
                ? Unauthorized
                : QueryHelpers.AddQueryString(Unauthorized, "RequestedPath", requestedPath);
    }

    public static class Admin
    {
        public const string Home = "/Admin";
        public const string Search = "/Admin/Search";
        public const string Communications = "/Admin/Communications";
        public const string Users = "/Admin/Users";
        public const string UserTemplate = "/Admin/Users/{UserId}";
        public const string Owners = "/Admin/Owners";
        public const string OwnerTemplate = "/Admin/Owners/{OwnerId}";
        public const string OwnerTenantTemplate = "/Admin/Owners/{OwnerId}/{TenantId}";

        public static string User(object? userId) => $"{Users}/{Value(userId)}";
        public static string Owner(object? ownerId) => $"{Owners}/{Value(ownerId)}";
        public static string OwnerTenant(object? ownerId, object? tenantId) => $"{Owners}/{Value(ownerId)}/{Value(tenantId)}";
    }

    public static class Appointments
    {
        public const string Reservations = "/Reservations";
        public const string ReservationTemplate = "/Reservations/{reservationId}";
        public const string LegacyReservations = "/Appointments";
        public const string Shifts = "/Shifts";
        public const string ShiftTemplate = "/Shifts/{shiftId}";

        public const string AdminReservations = "/Admin/Reservations";
        public const string AdminReservationTemplate = "/Admin/Reservations/{reservationId}";
        public const string LegacyAdminReservations = "/Admin/Appointments";
        public const string AdminShifts = "/Admin/Shifts";
        public const string AdminShiftTemplate = "/Admin/Shifts/{shiftId}";
        public const string AdminShiftOccurrenceTemplate = "/Admin/Shifts/{shiftId}/ShiftOccurrence";
        public const string AdminShiftEditTemplate = "/Admin/Shifts/{shiftId}/Edit";
        public const string AdminShiftCreate = "/Admin/Shifts/New";
        public const string AdminShiftCreateTemplate = "/Admin/Shifts/New/{dayOfWeek:int}";

        public static string Reservation(object? reservationId) => $"{Reservations}/{Value(reservationId)}";
        public static string Shift(object? shiftId) => $"{Shifts}/{Value(shiftId)}";
        public static string AdminReservation(object? reservationId) => $"{AdminReservations}/{Value(reservationId)}";
        public static string AdminShift(object? shiftId) => $"{AdminShifts}/{Value(shiftId)}";
        public static string AdminShiftOccurrence(object? shiftId) => $"{AdminShift(shiftId)}/ShiftOccurrence";
        public static string AdminShiftEdit(object? shiftId) => $"{AdminShift(shiftId)}/Edit";
        public static string AdminShiftCreateForDay(int? dayOfWeek = null) =>
            dayOfWeek is null
                ? AdminShiftCreate
                : $"{AdminShiftCreate}/{dayOfWeek.Value}";
    }

    public static class Commerce
    {
        public const string Shop = "/Shop";
        public const string ShopOrders = "/Shop/Orders";

        public const string OwnerDashboard = "/Owners/Commerce";
        public const string OwnerOrders = "/Owners/Commerce/Orders";
        public const string OwnerProducts = "/Owners/Commerce/Products";
        public const string OwnerRecovery = "/Owners/Commerce/Recovery";
        public const string OwnerSettings = "/Owners/Commerce/Settings";

        public const string AdminDashboard = "/Admin/Commerce";
        public const string AdminProvision = "/Admin/Commerce/Provision";
        public const string AdminSync = "/Admin/Commerce/Sync";
    }

    public static class Medical
    {
        public const string Dashboard = "/Medical/Admin";
        public const string DashboardLegacy = "/Medical/";
        public const string CheckIn = "/Medical/CheckIn";
        public const string Patients = "/Medical/Patients";
        public const string PatientTemplate = "/Medical/Patients/{patientId}";
        public const string PatientNew = "/Medical/Patients/New";
        public const string PatientEditTemplate = "/Medical/Patients/{patientId}/Edit";
        public const string PatientProcedureTemplate = "/Medical/Patients/{patientId}/{procedureId}";
        public const string PatientEventTemplate = "/Medical/Patients/{patientId}/{procedureId}/{eventId}";

        public static string Patient(object? patientId) => $"{Patients}/{Value(patientId)}";
        public static string PatientEdit(object? patientId) => $"{Patient(patientId)}/Edit";
        public static string PatientProcedure(object? patientId, object? procedureId) => $"{Patient(patientId)}/{Value(procedureId)}";
        public static string PatientEvent(object? patientId, object? procedureId, object? eventId) =>
            $"{PatientProcedure(patientId, procedureId)}/{Value(eventId)}";
    }

    public static class Sea
    {
        public const string Dashboard = "/sea";
        public const string OwnerDashboard = "/sea/Owner";
        public const string OwnerSettings = "/sea/Owner/Settings";
        public const string OwnerSettingsUsers = "/sea/Owner/Settings/Users";
        public const string OwnerSettingsAdvanced = "/sea/Owner/Settings/Advanced";
        public const string OwnerSettingsStatistics = "/sea/Owner/Settings/Statistics";

        public const string AdminDashboard = "/sea/Admin";
        public const string AdminProjectsSettings = "/sea/Admin/Settings/Projects";
        public const string AdminReminders = "/sea/Admin/Reminders";

        public const string Projects = "/sea/Projects";
        public const string ProjectTemplate = "/sea/Projects/{projectId}";
        public const string ProjectNew = "/sea/Projects/New";
        public const string ProjectEditTemplate = "/sea/Projects/Edit/{projectId}";
        public const string ProcedureTemplate = "/sea/Projects/{projectId}/{procedureId}";
        public const string EventTemplate = "/sea/Projects/{projectId}/{procedureId}/{eventId}";

        public const string TemplateProjects = "/sea/Admin/Settings/Projects/Templates";
        public const string TemplateProjectTemplate = "/sea/Admin/Settings/Projects/Templates/{projectId}";
        public const string TemplateProjectNew = "/sea/Admin/Settings/Projects/Templates/New";
        public const string TemplateProjectEditTemplate = "/sea/Admin/Settings/Projects/Templates/Edit/{projectId}";
        public const string TemplateProcedureTemplate = "/sea/Admin/Settings/Projects/Templates/{projectId}/{procedureId}";
        public const string TemplateEventTemplate = "/sea/Admin/Settings/Projects/Templates/{projectId}/{procedureId}/{eventId}";

        public static string Project(object? projectId) => $"{Projects}/{Value(projectId)}";
        public static string ProjectEdit(object? projectId) => $"{Projects}/Edit/{Value(projectId)}";
        public static string Procedure(object? projectId, object? procedureId) => $"{Project(projectId)}/{Value(procedureId)}";
        public static string Event(object? projectId, object? procedureId, object? eventId) => $"{Procedure(projectId, procedureId)}/{Value(eventId)}";

        public static string TemplateProject(object? projectId) => $"{TemplateProjects}/{Value(projectId)}";
        public static string TemplateProjectEdit(object? projectId) => $"{TemplateProjects}/Edit/{Value(projectId)}";
        public static string TemplateProcedure(object? projectId, object? procedureId) => $"{TemplateProject(projectId)}/{Value(procedureId)}";
        public static string TemplateEvent(object? projectId, object? procedureId, object? eventId) =>
            $"{TemplateProcedure(projectId, procedureId)}/{Value(eventId)}";
    }

    private static string Value(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
}
