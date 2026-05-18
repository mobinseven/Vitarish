using System.Reflection;
using System.Xml.Linq;
using FluentAssertions;
using Vitarish.Main.Common;

namespace Main.Tests.Common;

public class AppRoutesTests
{
    // ── Appointments ─────────────────────────────────────────────────────────

    [Fact]
    public void AdminShiftCreateForDay_WithoutDayOfWeek_UsesBaseRoute()
    {
        AppRoutes.Appointments.AdminShiftCreateForDay()
            .Should().Be(AppRoutes.Appointments.OwnerShiftCreate);
    }

    [Fact]
    public void AdminShiftCreateForDay_WithDayOfWeek_AppendsSegment()
    {
        AppRoutes.Appointments.AdminShiftCreateForDay((int)DayOfWeek.Wednesday)
            .Should().Be("/admin/appointments/shifts/new/3");
    }

    [Fact]
    public void AdminShiftOccurrence_AppendsOccurrenceSegment()
    {
        Guid id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        AppRoutes.Appointments.AdminShiftOccurrence(id)
            .Should().Be($"/admin/appointments/shifts/{id:D}/occurrence");
    }

    [Fact]
    public void AdminShiftEdit_AppendsEditSegment()
    {
        Guid id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        AppRoutes.Appointments.AdminShiftEdit(id)
            .Should().Be($"/admin/appointments/shifts/{id:D}/edit");
    }

    [Fact]
    public void AdminShift_BuildsCorrectPath()
    {
        Guid id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        AppRoutes.Appointments.AdminShift(id)
            .Should().Be($"/admin/appointments/shifts/{id:D}");
    }

    [Fact]
    public void Reservation_BuildsCorrectPath()
    {
        Guid id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        AppRoutes.Appointments.Reservation(id)
            .Should().Be($"/appointments/reservations/{id:D}");
    }

    // ── Medical ──────────────────────────────────────────────────────────────

    [Fact]
    public void PatientEvent_ComposesExplicitlyNestedRoute()
    {
        Guid patientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid procedureId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid eventId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        AppRoutes.Clinic.PatientEvent(patientId, procedureId, eventId)
            .Should().Be($"/medical/patients/{patientId:D}/procedures/{procedureId:D}/events/{eventId:D}");
    }

    [Fact]
    public void PatientProcedure_ComposesNestedRoute()
    {
        Guid patientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid procedureId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        AppRoutes.Clinic.PatientProcedure(patientId, procedureId)
            .Should().Be($"/medical/patients/{patientId:D}/procedures/{procedureId:D}");
    }

    [Fact]
    public void PatientEdit_AppendsEditSegment()
    {
        Guid id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        AppRoutes.Clinic.PatientEdit(id)
            .Should().Be($"/medical/patients/{id:D}/edit");
    }

    // ── Sea ──────────────────────────────────────────────────────────────────

    [Fact]
    public void TemplateEvent_ComposesExplicitlyNestedRoute()
    {
        Guid projectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid procedureId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid eventId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        AppRoutes.Sea.TemplateEvent(projectId, procedureId, eventId)
            .Should().Be($"/sea/admin/settings/projects/templates/{projectId:D}/procedures/{procedureId:D}/events/{eventId:D}");
    }

    [Fact]
    public void SeaProjectEdit_PutsIdBeforeOperation()
    {
        Guid id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        AppRoutes.Sea.ProjectEdit(id).Should().Be($"/sea/projects/{id:D}/edit");
    }

    [Fact]
    public void SeaProcedure_ComposesNestedRoute()
    {
        Guid projectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid procedureId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        AppRoutes.Sea.Procedure(projectId, procedureId)
            .Should().Be($"/sea/projects/{projectId:D}/procedures/{procedureId:D}");
    }

    // ── Admin ────────────────────────────────────────────────────────────────

    [Fact]
    public void AdminOwnerTenant_ComposesExplicitTenantSegment()
    {
        Guid ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid tenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        AppRoutes.PlatformAdmin.OwnerTenant(ownerId, tenantId)
            .Should().Be($"/admin/owners/{ownerId:D}/tenants/{tenantId:D}");
    }

    // ── Account ──────────────────────────────────────────────────────────────

    [Fact]
    public void UnauthorizedWithRequestedPath_AddsEncodedQuery()
    {
        AppRoutes.Account.UnauthorizedWithRequestedPath("/medical/patients?id=42")
            .Should().Be("/account/unauthorized?requestedPath=%2Fmedical%2Fpatients%3Fid%3D42");
    }

    [Fact]
    public void UnauthorizedWithRequestedPath_NullOrEmpty_ReturnsBaseRoute()
    {
        AppRoutes.Account.UnauthorizedWithRequestedPath(null).Should().Be(AppRoutes.Account.Unauthorized);
        AppRoutes.Account.UnauthorizedWithRequestedPath("").Should().Be(AppRoutes.Account.Unauthorized);
        AppRoutes.Account.UnauthorizedWithRequestedPath("  ").Should().Be(AppRoutes.Account.Unauthorized);
    }

    // ── Routes are lowercase canonical ───────────────────────────────────────

    [Theory]
    [InlineData(AppRoutes.Home)]
    [InlineData(AppRoutes.Common.Error)]
    [InlineData(AppRoutes.Common.Privacy)]
    [InlineData(AppRoutes.Account.Login)]
    [InlineData(AppRoutes.Account.LoginPhone)]
    [InlineData(AppRoutes.Account.Logout)]
    [InlineData(AppRoutes.Account.Unauthorized)]
    [InlineData(AppRoutes.Account.Settings)]
    [InlineData(AppRoutes.PlatformAdmin.Home)]
    [InlineData(AppRoutes.Appointments.UserShifts)]
    [InlineData(AppRoutes.Appointments.UserReservations)]
    [InlineData(AppRoutes.Appointments.OwnerShifts)]
    [InlineData(AppRoutes.Appointments.OwnerReservations)]
    [InlineData(AppRoutes.Commerce.Shop)]
    [InlineData(AppRoutes.Commerce.OwnerDashboard)]
    [InlineData(AppRoutes.Commerce.AdminDashboard)]
    [InlineData(AppRoutes.Clinic.OwnerDashboard)]
    [InlineData(AppRoutes.Clinic.Patients)]
    [InlineData(AppRoutes.Sea.Dashboard)]
    [InlineData(AppRoutes.Sea.Projects)]
    [InlineData(AppRoutes.Sea.OwnerTemplateProjects)]
    public void RouteConstant_IsLowercaseKebabCase(string route)
    {
        // Route templates may contain {param:constraint} segments – strip those before checking
        string[] segments = route.TrimStart('/').Split('/');
        foreach (string segment in segments)
        {
            // Skip empty (root "/") and route-parameter segments like "{shiftId:guid}"
            if (string.IsNullOrEmpty(segment) || segment.StartsWith('{'))
            {
                continue;
            }
            segment.Should().Be(segment.ToLowerInvariant(),
                because: $"route segment '{segment}' in '{route}' should be lowercase");
        }
    }


    [Fact]
    public void RouteSegments_HaveLocalizationEntries()
    {
        ISet<string> resourceNames = LoadStringsResourceNames("Strings.resx");
        string[] missingSegments = GetRouteSegments()
            .Where(segment => !resourceNames.Contains(segment))
            .Order(StringComparer.Ordinal)
            .ToArray();

        missingSegments.Should().BeEmpty(
            because: "NavService uses literal route segments as breadcrumb localization keys");
    }


    [Fact]
    public void RouteSegments_HavePersianLocalizationEntries()
    {
        ISet<string> resourceNames = LoadStringsResourceNames("Strings.fa.resx");
        string[] missingSegments = GetRouteSegments()
            .Where(segment => !resourceNames.Contains(segment))
            .Order(StringComparer.Ordinal)
            .ToArray();

        missingSegments.Should().BeEmpty(
            because: "NavService uses literal route segments as breadcrumb localization keys in every culture resource");
    }

    private static IEnumerable<string> GetRouteSegments()
    {
        return GetRouteFields(typeof(AppRoutes))
            .Select(field => field.GetRawConstantValue() as string)
            .OfType<string>()
            .Where(route => !string.IsNullOrWhiteSpace(route))
            .SelectMany(GetLiteralSegments)
            .Distinct(StringComparer.Ordinal);
    }

    private static IEnumerable<FieldInfo> GetRouteFields(Type type)
    {
        if (type == typeof(AppRoutes.QueryKeys))
        {
            return [];
        }

        IEnumerable<FieldInfo> fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string));

        return fields.Concat(type.GetNestedTypes(BindingFlags.Public).SelectMany(GetRouteFields));
    }

    private static IEnumerable<string> GetLiteralSegments(string route)
    {
        return route.Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(segment => !segment.StartsWith('{'));
    }

    private static ISet<string> LoadStringsResourceNames(string resourceFileName)
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string resourcePath = Path.Combine(directory, "src", "Interfaces", "Interfaces.Base", "Localization", resourceFileName);
            if (File.Exists(resourcePath))
            {
                return XDocument.Load(resourcePath)
                    .Root!
                    .Elements("data")
                    .Select(element => element.Attribute("name")?.Value)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)!;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new FileNotFoundException($"Could not locate src/Interfaces/Interfaces.Base/Localization/{resourceFileName}.");
    }

    // ── Search ───────────────────────────────────────────────────────────────

    [Fact]
    public void SearchGlobal_WithQuery_AppendsQueryString()
    {
        AppRoutes.Search.GlobalSearch("test query")
            .Should().Be("/search?q=test%20query");
    }

    [Fact]
    public void SearchScoped_BuildsCorrectPath()
    {
        AppRoutes.Search.Scoped("patients", "Smith")
            .Should().Be("/search/patients?q=Smith");
    }

    // ── Links ────────────────────────────────────────────────────────────────

    [Fact]
    public void Links_Short_BuildsCorrectPath()
    {
        AppRoutes.Links.Short("abc123").Should().Be("/r/abc123");
    }

    [Fact]
    public void Links_Qr_BuildsCorrectPath()
    {
        AppRoutes.Links.Qr("xyz789").Should().Be("/qr/xyz789");
    }

    [Fact]
    public void Links_Short_EmptyCode_Throws()
    {
        FluentActions.Invoking(() => AppRoutes.Links.Short(""))
            .Should().Throw<ArgumentException>();
    }
}

