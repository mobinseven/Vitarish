using System.Security.Claims;
using Microsoft.AspNetCore.Http;
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
using Vitarish.Files.Domain.Entities;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Infrastructure.Data;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.E2e.Infrastructure.Fixtures;

internal static class E2eFixtureSeeder
{
    public const string UserName = "e2e-owner";
    public const string TenantUserName = "e2e-tenant-user";
    public const string TenantBOwnerUserName = "e2e-tenant-b-owner";
    public const string PlatformAdminUserName = "e2e-platform-admin";
    public const string UserManagementPlatformAdminUserName = "e2e-user-management-admin";
    public const string LockoutPlatformAdminUserName = "e2e-lockout-platform-admin";
    public const string PatientName = "E2E Patient";
    public const string PatientFirstName = "E2E";
    public const string PatientLastName = "Patient";
    private const string TenantAFileChecksum = "720B672F1CF84CDE6F1788EECF15D8FA940BBEE7D6610D1B2E2546E85969A726";
    private const string TenantBFileChecksum = "C869566813A0DFFB02AB1863035E73FC9F29E569B795BEF12E4EB7F6D80DFDA7";

    public static async Task SeedAsync(IServiceProvider services, string password, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        UserManager<User> users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        RoleManager<IdentityRole> roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        IUsersService usersService = scope.ServiceProvider.GetRequiredService<IUsersService>();
        IHttpContextAccessor httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)],
                "e2e-fixture"))
        };

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
        User tenantBOwner = await EnsureUserAsync(
            users,
            TenantBOwnerUserName,
            Guid.Parse("c65fcb80-8db3-4e2f-b3b9-1c4d7b1e0a62"),
            "e2e-tenant-b-owner@example.test",
            "E2E",
            "Tenant B Owner",
            password);
        User platformAdmin = await EnsureUserAsync(
            users,
            PlatformAdminUserName,
            Guid.Parse("f43b8f0e-3c4f-4ab3-b52e-2bf6f4d4d2a7"),
            "e2e-platform-admin@example.test",
            "E2E",
            "Platform Admin",
            password);
        User userManagementPlatformAdmin = await EnsureUserAsync(
            users,
            UserManagementPlatformAdminUserName,
            Guid.Parse("8a3e2c1d-6b7f-4f90-a5d2-1c8e9b4f7a63"),
            "e2e-user-management-admin@example.test",
            "E2E",
            "User Management Admin",
            password);
        User lockoutPlatformAdmin = await EnsureUserAsync(
            users,
            LockoutPlatformAdminUserName,
            Guid.Parse("d08bc2a1-621d-4f60-a2e4-8cba70a279e1"),
            "e2e-lockout-platform-admin@example.test",
            "E2E",
            "Lockout Platform Admin",
            password);
        IdentityResult failedAttemptReset = await users.ResetAccessFailedCountAsync(lockoutPlatformAdmin);
        if (!failedAttemptReset.Succeeded)
            throw new InvalidOperationException("E2E lockout platform-admin failed-attempt reset failed.");
        IdentityResult lockoutReset = await users.SetLockoutEndDateAsync(lockoutPlatformAdmin, null);
        if (!lockoutReset.Succeeded)
            throw new InvalidOperationException("E2E lockout platform-admin lockout reset failed.");

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

        Guid tenantBOwnerId = Guid.Parse(tenantBOwner.Id);
        Tenant? tenantB = await parties.Tenants.SingleOrDefaultAsync(item => item.AppId == "private-practice-owner-tenant-b", cancellationToken);
        if (tenantB is null)
        {
            tenantB = new Tenant
            {
                Id = Guid.Parse("b2d2e4a1-2fd8-4781-8d9f-6c96f2a5e7b4"),
                OwnerId = tenantBOwnerId,
                Name = "E2E Clinic B",
                AppId = "private-practice-owner-tenant-b"
            };
            _ = parties.Tenants.Add(tenantB);
            _ = parties.TenantIndicators.Add(new TenantIndicator
            {
                Id = Guid.Parse("d88bd1d9-7dc4-4bc3-bb53-f4dd7bce0bb0"),
                TenantId = tenantB.Id,
                Method = IndicatorMethod.Hostname,
                Value = "tenant-b.localhost",
                Status = IndicatorStatus.Active,
                IsPrimary = true
            });
        }

        if (!await parties.Owners.AnyAsync(owner => owner.Id == tenantBOwnerId, cancellationToken))
            _ = parties.Owners.Add(new Owner { Id = tenantBOwnerId, Name = "E2E Tenant B Owner" });

        requestContext.SetContext("e2e", null, tenantBOwnerId.ToString(), tenantB.Id.ToString(), tenantBOwner.Id);
        _ = await parties.SaveChangesAsync(cancellationToken);

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
                Tags = ["افزایش قد", "پیگیری"],
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

        FilesDbContext files = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        IObjectStorageService objectStorage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        Guid fileId = Guid.Parse("4b8d1e7a-9f24-4d65-a8b1-6c0e2f5d7a91");
        if (!await files.Set<FileInformation>().AnyAsync(file => file.Id == fileId, cancellationToken))
        {
            _ = files.Set<FileInformation>().Add(new FileInformation
            {
                Id = fileId,
                TenantId = tenant.Id.ToString(),
                ContentType = "text/plain",
                Checksum = TenantAFileChecksum,
                Url = "e2e/tenant-a/file.txt",
                Metadata = "{\"size\":13}"
            });
            _ = await files.SaveChangesAsync(cancellationToken);
        }
        await EnsureObjectAsync(objectStorage, TenantAFileChecksum, "tenant-a-file", cancellationToken);
        IFileIntegrityService fileIntegrity = scope.ServiceProvider.GetRequiredService<IFileIntegrityService>();
        FileInformationReadDto tenantAFile = new()
        {
            Id = fileId,
            Owner = tenant.Id.ToString(),
            Checksum = TenantAFileChecksum,
            ContentType = "text/plain",
            Metadata = new FileMetadata { Size = 13 }
        };
        _ = await fileIntegrity.BeginLegacyEnrollmentAsync(tenantAFile, cancellationToken);
        FileIntegrityVerificationResult tenantAFileVerification = await fileIntegrity.VerifyAsync(tenantAFile, cancellationToken);
        if (!tenantAFileVerification.IsVerified)
            throw new InvalidOperationException("E2E tenant-A file integrity enrollment failed.");

        string roleName = $"{tenant.Id}:{IdentityDefinitions.TenantOwner}";
        if (!await roles.RoleExistsAsync(roleName))
            _ = (await roles.CreateAsync(new IdentityRole(roleName))).Succeeded;
        IdentityResult tenantOwnerResult = await usersService.ReplaceTenantRoleAsync(user.Id, tenant.Id, IdentityDefinitions.TenantOwner, cancellationToken);
        if (!tenantOwnerResult.Succeeded)
            throw new InvalidOperationException("E2E tenant-owner role assignment failed.");

        string tenantUserRoleName = $"{tenant.Id}:{IdentityDefinitions.TenantUser}";
        if (!await roles.RoleExistsAsync(tenantUserRoleName))
            _ = (await roles.CreateAsync(new IdentityRole(tenantUserRoleName))).Succeeded;
        IdentityResult tenantUserResult = await usersService.ReplaceTenantRoleAsync(tenantUser.Id, tenant.Id, IdentityDefinitions.TenantUser, cancellationToken);
        if (!tenantUserResult.Succeeded)
            throw new InvalidOperationException("E2E tenant-user role assignment failed.");

        string tenantBOwnerRoleName = $"{tenantB.Id}:{IdentityDefinitions.TenantOwner}";
        if (!await roles.RoleExistsAsync(tenantBOwnerRoleName))
            _ = (await roles.CreateAsync(new IdentityRole(tenantBOwnerRoleName))).Succeeded;

        await using AsyncServiceScope tenantBScope = services.CreateAsyncScope();
        IRequestContextWriter tenantBRequestContext = tenantBScope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
        tenantBRequestContext.SetContext("e2e", null, tenantBOwnerId.ToString(), tenantB.Id.ToString(), tenantBOwner.Id);
        IUsersService tenantBUsersService = tenantBScope.ServiceProvider.GetRequiredService<IUsersService>();
        IdentityResult tenantBOwnerResult = await tenantBUsersService.ReplaceTenantRoleAsync(tenantBOwner.Id, tenantB.Id, IdentityDefinitions.TenantOwner, cancellationToken);
        if (!tenantBOwnerResult.Succeeded)
            throw new InvalidOperationException("E2E tenant-B owner role assignment failed.");
        ClinicDbContext tenantBClinic = tenantBScope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        AppointmentsDbContext tenantBAppointments = tenantBScope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();
        CommunicationsDbContext tenantBCommunications = tenantBScope.ServiceProvider.GetRequiredService<CommunicationsDbContext>();
        Guid tenantBPatientId = Guid.Parse("f4c8109a-6f22-4a31-9ab8-48d7ad0d7e2f");
        if (!await tenantBClinic.Set<PatientView>().AnyAsync(patient => patient.Id == tenantBPatientId, cancellationToken))
        {
            _ = tenantBClinic.Set<PatientView>().Add(new PatientView
            {
                Id = tenantBPatientId,
                TenantId = tenantB.Id.ToString(),
                Name = "E2E Tenant B Patient",
                FirstName = "Tenant B",
                LastName = "Patient",
                IdentificationCode = "E2E-B-0001",
                Email = "tenant-b-patient@example.test",
                Phone = "+989000000001",
                Template = false,
                CreatedAt = DateTime.UtcNow
            });
            _ = await tenantBClinic.SaveChangesAsync(cancellationToken);
        }

        Guid tenantBShiftId = Guid.Parse("ab41d5d7-7c11-45a5-b9dc-24f9aa01f2c7");
        if (!await tenantBAppointments.Shifts.AnyAsync(shift => shift.Id == tenantBShiftId, cancellationToken))
        {
            _ = tenantBAppointments.Shifts.Add(new Shift
            {
                Id = tenantBShiftId,
                Status = ShiftStatus.Active,
                Visibility = ShiftVisibility.Public,
                DayOfWeek = dayOfWeek,
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(12, 0),
                TimeZone = "Asia/Tehran",
                TotalSeats = 3
            });
            _ = tenantBAppointments.Reservations.Add(new Reservation
            {
                Id = Guid.Parse("3a72a9a6-91cc-4ad6-8a23-04e0a0aee4c3"),
                ShiftId = tenantBShiftId,
                Date = today,
                ReservingPartyId = tenantBPatientId.ToString(),
                Status = ReservationStatus.Confirmed,
                EstimatedSessionLength = TimeSpan.FromMinutes(60),
                ConfirmedAt = DateTime.UtcNow
            });
            _ = await tenantBAppointments.SaveChangesAsync(cancellationToken);
        }

        Guid tenantBReminderId = Guid.Parse("8bb9f409-965a-4f8e-9a85-3a6fc4cc3b7e");
        if (!await tenantBCommunications.Reminders.AnyAsync(reminder => reminder.Id == tenantBReminderId, cancellationToken))
        {
            _ = tenantBCommunications.Reminders.Add(new Reminder
            {
                Id = tenantBReminderId,
                SourceId = tenantBPatientId,
                SourceKind = "E2eTenantBFixture",
                TemplatePurpose = "e2e-tenant-b-fixture",
                Kind = ReminderKind.Email,
                Recipient = "tenant-b-patient@example.test",
                RecipientUserId = tenantBOwner.Id,
                Subject = "E2E tenant B reminder",
                TextBody = "E2E tenant B fixture reminder",
                RemindAt = DateTime.UtcNow.AddHours(1)
            });
            _ = await tenantBCommunications.SaveChangesAsync(cancellationToken);
        }

        FilesDbContext tenantBFiles = tenantBScope.ServiceProvider.GetRequiredService<FilesDbContext>();
        Guid tenantBFileId = Guid.Parse("9e2f6a41-7c35-4b8d-a0e6-1f5d9c2b7a84");
        if (!await tenantBFiles.Set<FileInformation>().AnyAsync(file => file.Id == tenantBFileId, cancellationToken))
        {
            _ = tenantBFiles.Set<FileInformation>().Add(new FileInformation
            {
                Id = tenantBFileId,
                TenantId = tenantB.Id.ToString(),
                ContentType = "text/plain",
                Checksum = TenantBFileChecksum,
                Url = "e2e/tenant-b/file.txt",
                Metadata = "{\"size\":13}"
            });
            _ = await tenantBFiles.SaveChangesAsync(cancellationToken);
        }
        await EnsureObjectAsync(objectStorage, TenantBFileChecksum, "tenant-b-file", cancellationToken);
        IFileIntegrityService tenantBFileIntegrity = tenantBScope.ServiceProvider.GetRequiredService<IFileIntegrityService>();
        FileInformationReadDto tenantBFile = new()
        {
            Id = tenantBFileId,
            Owner = tenantB.Id.ToString(),
            Checksum = TenantBFileChecksum,
            ContentType = "text/plain",
            Metadata = new FileMetadata { Size = 13 }
        };
        _ = await tenantBFileIntegrity.BeginLegacyEnrollmentAsync(tenantBFile, cancellationToken);
        FileIntegrityVerificationResult tenantBFileVerification = await tenantBFileIntegrity.VerifyAsync(tenantBFile, cancellationToken);
        if (!tenantBFileVerification.IsVerified)
            throw new InvalidOperationException("E2E tenant-B file integrity enrollment failed.");

        if (!await roles.RoleExistsAsync(IdentityDefinitions.PlatformAdmin))
            _ = (await roles.CreateAsync(new IdentityRole(IdentityDefinitions.PlatformAdmin))).Succeeded;
        if (!await users.IsInRoleAsync(platformAdmin, IdentityDefinitions.PlatformAdmin))
        {
            IdentityResult result = await usersService.AddUserToRoleAsync(platformAdmin, IdentityDefinitions.PlatformAdmin, cancellationToken);
            if (!result.Succeeded)
                throw new InvalidOperationException("E2E platform-admin role assignment failed.");
        }
        if (!await users.IsInRoleAsync(userManagementPlatformAdmin, IdentityDefinitions.PlatformAdmin))
        {
            IdentityResult result = await usersService.AddUserToRoleAsync(userManagementPlatformAdmin, IdentityDefinitions.PlatformAdmin, cancellationToken);
            if (!result.Succeeded)
                throw new InvalidOperationException("E2E user-management platform-admin role assignment failed.");
        }
        if (!await users.IsInRoleAsync(lockoutPlatformAdmin, IdentityDefinitions.PlatformAdmin))
        {
            IdentityResult result = await usersService.AddUserToRoleAsync(lockoutPlatformAdmin, IdentityDefinitions.PlatformAdmin, cancellationToken);
            if (!result.Succeeded)
                throw new InvalidOperationException("E2E lockout platform-admin role assignment failed.");
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

    private static async Task EnsureObjectAsync(IObjectStorageService storage, string checksum, string content, CancellationToken cancellationToken)
    {
        if (await storage.ExistsAsync(checksum, cancellationToken))
            return;

        await using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(content), writable: false);
        await storage.UploadAsync(checksum, "text/plain", stream, stream.Length, cancellationToken);
    }
}
