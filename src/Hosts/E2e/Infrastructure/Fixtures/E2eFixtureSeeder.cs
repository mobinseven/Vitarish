using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Users.Domain.Entities;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Appointments.Domain.Entities;
using Vitarish.Appointments.Domain.Enums;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Communications.Domain.Entities;
using Vitarish.Communications.Domain.Enums;
using Vitarish.Communications.Infrastructure.Data;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;

namespace Vitarish.E2e.Infrastructure.Fixtures;

internal static class E2eFixtureSeeder
{
    public const string UserName = "e2e-owner";
    public const string TenantUserName = "e2e-tenant-user";
    public const string PlatformAdminUserName = "e2e-platform-admin";
    public const string PatientName = "E2E Patient";
    public const string PatientFirstName = "E2E";
    public const string PatientLastName = "Patient";

    public static async Task SeedAsync(IServiceProvider services, string password, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        UserManager<User> users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        RoleManager<IdentityRole> roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        User user = await EnsureUserAsync(
            users,
            UserName,
            Guid.Parse("7b0f8d4d-3d6c-4d43-9f5e-9d5d3f9e6f4c"),
            "e2e-owner@example.test",
            "E2E",
            "Owner",
            password);
        User tenantUser = await EnsureUserAsync(
            users,
            TenantUserName,
            Guid.Parse("5a4f5e2b-0ec6-4ad9-a0a1-4dbf0c6c2a19"),
            "e2e-tenant-user@example.test",
            "E2E",
            "Tenant User",
            password);
        User platformAdmin = await EnsureUserAsync(
            users,
            PlatformAdminUserName,
            Guid.Parse("f43b8f0e-3c4f-4ab3-b52e-2bf6f4d4d2a7"),
            "e2e-platform-admin@example.test",
            "E2E",
            "Platform Admin",
            password);

        Guid ownerId = Guid.Parse(user.Id);
        PartiesDbContext parties = scope.ServiceProvider.GetRequiredService<PartiesDbContext>();
        IRequestContextWriter requestContext = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
        Tenant? tenant = await parties.Tenants.SingleOrDefaultAsync(item => item.AppId == "private-practice-owner", cancellationToken);
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = Guid.Parse("9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92"),
                OwnerId = ownerId,
                Name = "E2E Clinic",
                AppId = "private-practice-owner"
            };
            _ = parties.Tenants.Add(tenant);
            _ = parties.TenantIndicators.Add(new TenantIndicator
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Method = IndicatorMethod.Hostname,
                Value = "localhost",
                Status = IndicatorStatus.Active,
                IsPrimary = true
            });
            _ = parties.Owners.Add(new Owner { Id = ownerId, Name = "E2E Owner" });
            requestContext.SetContext("e2e", null, ownerId.ToString(), tenant.Id.ToString(), user.Id);
            _ = await parties.SaveChangesAsync(cancellationToken);
        }

        requestContext.SetContext("e2e", null, ownerId.ToString(), tenant.Id.ToString(), user.Id);

        Guid patientId = Guid.Parse("2d7a3eb9-1d47-4c63-9a45-19d7e6a0fb18");
        ClinicDbContext clinic = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        if (!await clinic.Set<PatientView>().AnyAsync(patient => patient.Id == patientId, cancellationToken))
        {
            _ = clinic.Set<PatientView>().Add(new PatientView
            {
                Id = patientId,
                TenantId = tenant.Id.ToString(),
                Name = PatientName,
                FirstName = PatientFirstName,
                LastName = PatientLastName,
                IdentificationCode = "E2E-0001",
                Email = "patient@example.test",
                Phone = "+989000000000",
                Template = false,
                CreatedAt = DateTime.UtcNow
            });
            _ = await clinic.SaveChangesAsync(cancellationToken);
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DayOfWeek dayOfWeek = today.DayOfWeek;
        Guid shiftId = Guid.Parse("f6b9fc45-e3c3-4fa0-a39e-6f21a0a37cb0");
        AppointmentsDbContext appointments = scope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();
        if (!await appointments.Shifts.AnyAsync(shift => shift.Id == shiftId, cancellationToken))
        {
            _ = appointments.Shifts.Add(new Shift
            {
                Id = shiftId,
                Status = ShiftStatus.Active,
                Visibility = ShiftVisibility.Public,
                DayOfWeek = dayOfWeek,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                TimeZone = "Asia/Tehran",
                TotalSeats = 2
            });
            _ = appointments.Reservations.Add(new Reservation
            {
                Id = Guid.Parse("5d1cecbf-a27a-4ef7-bf4c-4edaf7a8b5c2"),
                ShiftId = shiftId,
                Date = today,
                ReservingPartyId = patientId.ToString(),
                Status = ReservationStatus.Confirmed,
                EstimatedSessionLength = TimeSpan.FromMinutes(60),
                ConfirmedAt = DateTime.UtcNow
            });
            _ = await appointments.SaveChangesAsync(cancellationToken);
        }

        CommunicationsDbContext communications = scope.ServiceProvider.GetRequiredService<CommunicationsDbContext>();
        Guid reminderId = Guid.Parse("b1a0f95e-8c6b-4f2c-9b0f-2fa9e0c8e2d3");
        if (!await communications.Reminders.AnyAsync(reminder => reminder.Id == reminderId, cancellationToken))
        {
            _ = communications.Reminders.Add(new Reminder
            {
                Id = reminderId,
                SourceId = patientId,
                SourceKind = "E2eFixture",
                TemplatePurpose = "e2e-fixture",
                Kind = ReminderKind.Email,
                Recipient = "patient@example.test",
                RecipientUserId = user.Id,
                Subject = "E2E reminder",
                TextBody = "E2E fixture reminder",
                RemindAt = DateTime.UtcNow.AddHours(1)
            });
            _ = await communications.SaveChangesAsync(cancellationToken);
        }

        string roleName = $"{tenant.Id}:{IdentityDefinitions.TenantOwner}";
        if (!await roles.RoleExistsAsync(roleName))
            _ = (await roles.CreateAsync(new IdentityRole(roleName))).Succeeded;
        if (!await users.IsInRoleAsync(user, roleName))
        {
            IdentityResult result = await users.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
                throw new InvalidOperationException("E2E tenant-owner role assignment failed.");
        }

        string tenantUserRoleName = $"{tenant.Id}:{IdentityDefinitions.TenantUser}";
        if (!await roles.RoleExistsAsync(tenantUserRoleName))
            _ = (await roles.CreateAsync(new IdentityRole(tenantUserRoleName))).Succeeded;
        if (!await users.IsInRoleAsync(tenantUser, tenantUserRoleName))
        {
            IdentityResult result = await users.AddToRoleAsync(tenantUser, tenantUserRoleName);
            if (!result.Succeeded)
                throw new InvalidOperationException("E2E tenant-user role assignment failed.");
        }

        if (!await roles.RoleExistsAsync(IdentityDefinitions.PlatformAdmin))
            _ = (await roles.CreateAsync(new IdentityRole(IdentityDefinitions.PlatformAdmin))).Succeeded;
        if (!await users.IsInRoleAsync(platformAdmin, IdentityDefinitions.PlatformAdmin))
        {
            IdentityResult result = await users.AddToRoleAsync(platformAdmin, IdentityDefinitions.PlatformAdmin);
            if (!result.Succeeded)
                throw new InvalidOperationException("E2E platform-admin role assignment failed.");
        }
    }

    private static async Task<User> EnsureUserAsync(
        UserManager<User> users,
        string userName,
        Guid id,
        string email,
        string firstName,
        string lastName,
        string password)
    {
        User? user = await users.FindByNameAsync(userName);
        if (user is not null)
            return user;

        user = new User
        {
            Id = id.ToString(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName
        };
        IdentityResult created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
            throw new InvalidOperationException($"E2E user creation failed: {string.Join(';', created.Errors.Select(error => error.Code))}");
        return user;
    }
}
