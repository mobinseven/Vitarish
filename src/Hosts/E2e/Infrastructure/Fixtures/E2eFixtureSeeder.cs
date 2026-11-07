using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Parties.Domain.Models;
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
using MediatR;
using Vitarish.Clinic.Application.Commands.Patients;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Sea.Infrastructure.Data;
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
    public const string EmailChangeUserName = "e2e-email-change-user";
    public const string PhoneChangeUserName = "e2e-phone-change-user";
    public const string TenantBOwnerUserName = "e2e-tenant-b-owner";
    public const string PlatformAdminUserName = "e2e-platform-admin";
    public const string UserManagementPlatformAdminUserName = "e2e-user-management-admin";
    public const string LockoutPlatformAdminUserName = "e2e-lockout-platform-admin";
    public const string EmailSignInPlatformAdminUserName = "e2e-email-sign-in-platform-admin";
    public const string EmailSignInPlatformAdminEmail = "e2e-email-sign-in-platform-admin@example.test";
    /// <summary>Has a confirmed email and no phone and no role, so it can create a clinic only by its email.</summary>
    public const string EmailOnlyClinicCreatorUserName = "e2e-email-only-clinic-creator";
    public const string EmailOnlyClinicCreatorEmail = "e2e-email-only-clinic-creator@example.test";
    /// <summary>Has a confirmed email, no password, no phone and no workspace, like a person who has just signed up by email code.</summary>
    public const string PasswordlessEmailAccountUserName = "e2e-platform-home-account-user";
    public const string PasswordlessEmailAccountEmail = "e2e-platform-home-account-user@example.test";
    /// <summary>Has two-step sign-in on, so a correct password is followed by a text-message code.</summary>
    public const string TwoFactorPlatformAdminUserName = "e2e-two-factor-platform-admin";
    public const string TwoFactorPlatformAdminPhoneNumber = "+989130000001";
    /// <summary>Has two-step sign-in on with a confirmed email and no confirmed phone.</summary>
    public const string EmailTwoFactorPlatformAdminUserName = "e2e-email-two-factor-platform-admin";
    public const string EmailTwoFactorPlatformAdminEmail = "e2e-email-two-factor-platform-admin@example.test";
    /// <summary>Passwordless account for the self-service first-password and two-step sign-in flow.</summary>
    public const string PasswordlessPhoneAccountUserName = "+989130000002";
    public const string PasswordlessPhoneAccountPhoneNumber = "+989130000002";
    public const string PasswordlessPhoneAccountEmail = "e2e-passwordless-phone-account@example.test";
    /// <summary>Staff in tenant A only; registers a passkey and signs in with it.</summary>
    public const string PasskeyUserName = "e2e-passkey-user";
    /// <summary>Staff in tenant A and an owner of tenant B, so the landing home lists two workspaces.</summary>
    public const string SignInUserName = "e2e-sign-in-user";
    public const string PatientName = "E2E Patient";
    public const string PatientFirstName = "E2E";
    public const string PatientLastName = "Patient";
    private static readonly Guid TenantAId = Guid.Parse("9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92");
    private static readonly Guid TenantBId = Guid.Parse("b2d2e4a1-2fd8-4781-8d9f-6c96f2a5e7b4");
    private const string TenantAFileChecksum = "720B672F1CF84CDE6F1788EECF15D8FA940BBEE7D6610D1B2E2546E85969A726";
    private const string TenantBFileChecksum = "C869566813A0DFFB02AB1863035E73FC9F29E569B795BEF12E4EB7F6D80DFDA7";
    // An 8x8 solid teal PNG: tenant B's public logo, which the sign-in clinic list shows.
    private static readonly byte[] TenantBLogoPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAIAAABLbSncAAAAEUlEQVR42mPgq2nDihiGlgQAdE5EAa8umwAAAAAASUVORK5CYII=");

    public static async Task SeedAsync(IServiceProvider services, string password, CancellationToken cancellationToken)
    {
        Console.WriteLine("E2E fixture seeding started.");
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

        Console.WriteLine("E2E fixture seeding: ensuring identity 1 of 13.");
        User user = await EnsureUserAsync(
            users,
            UserName,
            Guid.Parse("7b0f8d4d-3d6c-4d43-9f5e-9d5d3f9e6f4c"),
            "e2e-owner@example.test",
            "E2E",
            "Owner",
            password);
        Console.WriteLine("E2E fixture seeding: ensuring identity 2 of 13.");
        User tenantUser = await EnsureUserAsync(
            users,
            TenantUserName,
            Guid.Parse("5a4f5e2b-0ec6-4ad9-a0a1-4dbf0c6c2a19"),
            "e2e-tenant-user@example.test",
            "E2E",
            "Tenant User",
            password);
        Console.WriteLine("E2E fixture seeding: ensuring identity 3 of 13.");
        User emailChangeUser = await EnsureUserAsync(
            users,
            EmailChangeUserName,
            Guid.Parse("b7f0c2e8-4a1d-4e9b-8c3f-5d2a7b6e9f14"),
            "e2e-email-change-user@example.test",
            "E2E",
            "Email Change User",
            password);
        Console.WriteLine("E2E fixture seeding: ensuring identity 4 of 13.");
        User tenantBOwner = await EnsureUserAsync(
            users,
            TenantBOwnerUserName,
            Guid.Parse("c65fcb80-8db3-4e2f-b3b9-1c4d7b1e0a62"),
            "e2e-tenant-b-owner@example.test",
            "E2E",
            "Tenant B Owner",
            password);
        Console.WriteLine("E2E fixture seeding: ensuring identity 5 of 13.");
        User platformAdmin = await EnsureUserAsync(
            users,
            PlatformAdminUserName,
            Guid.Parse("f43b8f0e-3c4f-4ab3-b52e-2bf6f4d4d2a7"),
            "e2e-platform-admin@example.test",
            "E2E",
            "Platform Admin",
            password);
        Console.WriteLine("E2E fixture seeding: ensuring identity 6 of 13.");
        User userManagementPlatformAdmin = await EnsureUserAsync(
            users,
            UserManagementPlatformAdminUserName,
            Guid.Parse("8a3e2c1d-6b7f-4f90-a5d2-1c8e9b4f7a63"),
            "e2e-user-management-admin@example.test",
            "E2E",
            "User Management Admin",
            password);
        Console.WriteLine("E2E fixture seeding: ensuring identity 7 of 13.");
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
        Console.WriteLine("E2E fixture seeding: ensuring identity 8 of 13.");
        User emailSignInPlatformAdmin = await EnsureUserAsync(
            users,
            EmailSignInPlatformAdminUserName,
            Guid.Parse("5c7d2e91-3a4b-4c8d-9e0f-1a2b3c4d5e6f"),
            EmailSignInPlatformAdminEmail,
            "E2E",
            "Email Sign-In Platform Admin",
            password,
            // Like production's first admin, whose address is never confirmed
            // before the first email-code sign-in.
            emailConfirmed: false);
        Console.WriteLine("E2E fixture seeding: ensuring identity 9 of 13.");
        User phoneChangeUser = await EnsureUserAsync(
            users,
            PhoneChangeUserName,
            Guid.Parse("0d6e3b7a-9c41-4f25-8a6b-2e7f1c9d4a83"),
            "e2e-phone-change-user@example.test",
            "E2E",
            "Phone Change User",
            password);
        Console.WriteLine("E2E fixture seeding: ensuring identity 10 of 13.");
        User signInUser = await EnsureUserAsync(
            users,
            SignInUserName,
            Guid.Parse("3f9a6c2e-8d14-4b7a-9e05-6c1d2b8f4a97"),
            "e2e-sign-in-user@example.test",
            "E2E",
            "Sign-In User",
            password);

        Console.WriteLine("E2E fixture seeding: ensuring identity 11 of 13.");
        User twoFactorPlatformAdmin = await EnsureUserAsync(
            users,
            TwoFactorPlatformAdminUserName,
            Guid.Parse("6e1f0a2b-4c3d-4e5f-8a9b-0c1d2e3f4a5b"),
            "e2e-two-factor-platform-admin@example.test",
            "E2E",
            "Two-Factor Platform Admin",
            password);
        if (!twoFactorPlatformAdmin.TwoFactorEnabled || twoFactorPlatformAdmin.PhoneNumber != TwoFactorPlatformAdminPhoneNumber)
        {
            twoFactorPlatformAdmin.PhoneNumber = TwoFactorPlatformAdminPhoneNumber;
            twoFactorPlatformAdmin.PhoneNumberConfirmed = true;
            twoFactorPlatformAdmin.TwoFactorEnabled = true;
            IdentityResult twoFactorUpdate = await users.UpdateAsync(twoFactorPlatformAdmin);
            if (!twoFactorUpdate.Succeeded)
                throw new InvalidOperationException("E2E two-factor platform-admin setup failed.");
        }
        Console.WriteLine("E2E fixture seeding: ensuring identity 12 of 13.");
        User passkeyUser = await EnsureUserAsync(
            users,
            PasskeyUserName,
            Guid.Parse("2b8c4d6e-1f3a-4b5c-9d7e-8f0a1b2c3d4e"),
            "e2e-passkey-user@example.test",
            "E2E",
            "Passkey User",
            password);

        Console.WriteLine("E2E fixture seeding: ensuring identity 13 of 14.");
        User emailTwoFactorPlatformAdmin = await EnsureUserAsync(
            users,
            EmailTwoFactorPlatformAdminUserName,
            Guid.Parse("c4e7b1a2-9f0d-4c33-a816-2d4b5e6f7081"),
            EmailTwoFactorPlatformAdminEmail,
            "E2E",
            "Email Two-Factor Platform Admin",
            password);
        if (!emailTwoFactorPlatformAdmin.EmailConfirmed
            || emailTwoFactorPlatformAdmin.Email != EmailTwoFactorPlatformAdminEmail
            || emailTwoFactorPlatformAdmin.PhoneNumber is not null
            || emailTwoFactorPlatformAdmin.PhoneNumberConfirmed
            || !emailTwoFactorPlatformAdmin.TwoFactorEnabled)
        {
            emailTwoFactorPlatformAdmin.Email = EmailTwoFactorPlatformAdminEmail;
            emailTwoFactorPlatformAdmin.EmailConfirmed = true;
            emailTwoFactorPlatformAdmin.PhoneNumber = null;
            emailTwoFactorPlatformAdmin.PhoneNumberConfirmed = false;
            emailTwoFactorPlatformAdmin.TwoFactorEnabled = true;
            IdentityResult emailTwoFactorUpdate = await users.UpdateAsync(emailTwoFactorPlatformAdmin);
            if (!emailTwoFactorUpdate.Succeeded)
                throw new InvalidOperationException("E2E email two-factor platform-admin setup failed.");
        }

        Console.WriteLine("E2E fixture seeding: ensuring identity 14 of 15.");
        User passwordlessPhoneAccount = await EnsurePasswordlessPhoneAccountAsync(users);

        Console.WriteLine("E2E fixture seeding: ensuring identity 15 of 16.");
        _ = await EnsureUserAsync(
            users,
            EmailOnlyClinicCreatorUserName,
            Guid.Parse("7a1f9c3e-5b2d-4e68-8c4a-0d9e1b2f3a45"),
            EmailOnlyClinicCreatorEmail,
            "E2E",
            "Email Only Clinic Creator",
            password);

        Console.WriteLine("E2E fixture seeding: ensuring identity 16 of 16.");
        _ = await EnsurePasswordlessEmailAccountAsync(users);

        Console.WriteLine("E2E fixture seeding: identity accounts are ready.");
        Guid ownerId = Guid.Parse(user.Id);
        PartiesDbContext parties = scope.ServiceProvider.GetRequiredService<PartiesDbContext>();
        IRequestContextWriter requestContext = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
        Console.WriteLine("E2E fixture seeding: preparing tenant A.");
        Tenant? tenant = await parties.Tenants.SingleOrDefaultAsync(item => item.Id == TenantAId, cancellationToken);
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = TenantAId,
                OwnerId = ownerId,
                Name = "E2E Clinic",
                AppId = Vitarish.Clinic.Application.ClinicTenant.AppId
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
        tenant.AppId = Vitarish.Clinic.Application.ClinicTenant.AppId;

        requestContext.SetContext("e2e", null, ownerId.ToString(), tenant.Id.ToString(), user.Id);

        Guid tenantBOwnerId = Guid.Parse(tenantBOwner.Id);
        Console.WriteLine("E2E fixture seeding: preparing tenant B.");
        Tenant? tenantB = await parties.Tenants.SingleOrDefaultAsync(item => item.Id == TenantBId, cancellationToken);
        if (tenantB is null)
        {
            tenantB = new Tenant
            {
                Id = TenantBId,
                OwnerId = tenantBOwnerId,
                Name = "E2E Clinic B",
                AppId = Vitarish.Clinic.Application.ClinicTenant.AppId
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
        tenantB.AppId = Vitarish.Clinic.Application.ClinicTenant.AppId;

        if (!await parties.Owners.AnyAsync(owner => owner.Id == tenantBOwnerId, cancellationToken))
            _ = parties.Owners.Add(new Owner { Id = tenantBOwnerId, Name = "E2E Tenant B Owner" });

        requestContext.SetContext("e2e", null, tenantBOwnerId.ToString(), tenantB.Id.ToString(), tenantBOwner.Id);
        _ = await parties.SaveChangesAsync(cancellationToken);

        requestContext.SetContext("e2e", null, ownerId.ToString(), tenant.Id.ToString(), user.Id);

        Console.WriteLine("E2E fixture seeding: preparing tenant A resources.");
        Guid patientId = Guid.Parse("2d7a3eb9-1d47-4c63-9a45-19d7e6a0fb18");
        // In the product a patient is a procedure project; the clinic patient view is derived from it.
        SeaDbContext sea = scope.ServiceProvider.GetRequiredService<SeaDbContext>();
        await SeedPatientAsync(
            sea,
            scope.ServiceProvider.GetRequiredService<ISender>(),
            new PatientWriteDto
            {
                Id = patientId,
                Name = PatientName,
                IdentificationCode = "E2E-0001",
                Template = false,
                FirstName = PatientFirstName,
                LastName = PatientLastName,
                Email = "patient@example.test",
                Phone = "+989000000000",
                Tags = ["افزایش قد", "پیگیری"]
            },
            cancellationToken);

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

        IdentityResult passwordlessPhoneAccountResult = await usersService.ReplaceTenantRoleAsync(passwordlessPhoneAccount.Id, tenant.Id, IdentityDefinitions.TenantUser, cancellationToken);
        if (!passwordlessPhoneAccountResult.Succeeded)
            throw new InvalidOperationException("E2E passwordless phone account role assignment failed.");

        IdentityResult emailChangeUserResult = await usersService.ReplaceTenantRoleAsync(emailChangeUser.Id, tenant.Id, IdentityDefinitions.TenantUser, cancellationToken);
        if (!emailChangeUserResult.Succeeded)
            throw new InvalidOperationException("E2E email-change user role assignment failed.");

        IdentityResult phoneChangeUserResult = await usersService.ReplaceTenantRoleAsync(phoneChangeUser.Id, tenant.Id, IdentityDefinitions.TenantUser, cancellationToken);
        if (!phoneChangeUserResult.Succeeded)
            throw new InvalidOperationException("E2E phone-change user role assignment failed.");

        IdentityResult passkeyUserResult = await usersService.ReplaceTenantRoleAsync(passkeyUser.Id, tenant.Id, IdentityDefinitions.TenantUser, cancellationToken);
        if (!passkeyUserResult.Succeeded)
            throw new InvalidOperationException("E2E passkey user role assignment failed.");

        IdentityResult signInUserResult = await usersService.ReplaceTenantRoleAsync(signInUser.Id, tenant.Id, IdentityDefinitions.TenantUser, cancellationToken);
        if (!signInUserResult.Succeeded)
            throw new InvalidOperationException("E2E sign-in user tenant-A role assignment failed.");

        Console.WriteLine("E2E fixture seeding: preparing tenant B resources and access.");
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
        IdentityResult signInUserTenantBResult = await tenantBUsersService.ReplaceTenantRoleAsync(signInUser.Id, tenantB.Id, IdentityDefinitions.TenantOwner, cancellationToken);
        if (!signInUserTenantBResult.Succeeded)
            throw new InvalidOperationException("E2E sign-in user tenant-B role assignment failed.");
        AppointmentsDbContext tenantBAppointments = tenantBScope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();
        CommunicationsDbContext tenantBCommunications = tenantBScope.ServiceProvider.GetRequiredService<CommunicationsDbContext>();
        Guid tenantBPatientId = Guid.Parse("f4c8109a-6f22-4a31-9ab8-48d7ad0d7e2f");
        await SeedPatientAsync(
            tenantBScope.ServiceProvider.GetRequiredService<SeaDbContext>(),
            tenantBScope.ServiceProvider.GetRequiredService<ISender>(),
            new PatientWriteDto
            {
                Id = tenantBPatientId,
                Name = "E2E Tenant B Patient",
                IdentificationCode = "E2E-B-0001",
                Template = false,
                FirstName = "Tenant B",
                LastName = "Patient",
                Email = "tenant-b-patient@example.test",
                Phone = "+989000000001"
            },
            cancellationToken);

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

        Console.WriteLine("E2E fixture seeding: giving tenant B a logo.");
        Guid tenantBLogoId = Guid.Parse("6d1e8b3f-2a94-4c57-b0d8-9f3a7e5c1b26");
        string tenantBLogoChecksum = Convert.ToHexString(SHA256.HashData(TenantBLogoPng));
        if (!await tenantBFiles.Set<FileInformation>().AnyAsync(file => file.Id == tenantBLogoId, cancellationToken))
        {
            _ = tenantBFiles.Set<FileInformation>().Add(new FileInformation
            {
                Id = tenantBLogoId,
                TenantId = tenantB.Id.ToString(),
                ContentType = "image/png",
                Checksum = tenantBLogoChecksum,
                Url = "e2e/tenant-b/logo.png",
                Metadata = $"{{\"size\":{TenantBLogoPng.Length}}}"
            });
            _ = await tenantBFiles.SaveChangesAsync(cancellationToken);
        }
        await EnsureObjectAsync(objectStorage, tenantBLogoChecksum, TenantBLogoPng, "image/png", cancellationToken);
        FileInformationReadDto tenantBLogo = new()
        {
            Id = tenantBLogoId,
            Owner = tenantB.Id.ToString(),
            Checksum = tenantBLogoChecksum,
            ContentType = "image/png",
            Metadata = new FileMetadata { Size = TenantBLogoPng.Length }
        };
        _ = await tenantBFileIntegrity.BeginLegacyEnrollmentAsync(tenantBLogo, cancellationToken);
        if (!(await tenantBFileIntegrity.VerifyAsync(tenantBLogo, cancellationToken)).IsVerified)
            throw new InvalidOperationException("E2E tenant-B logo integrity enrollment failed.");
        PartiesDbContext tenantBParties = tenantBScope.ServiceProvider.GetRequiredService<PartiesDbContext>();
        Tenant tenantBWithLogo = await tenantBParties.Tenants.SingleAsync(item => item.Id == tenantB.Id, cancellationToken);
        string tenantBLogoUrl = tenantBScope.ServiceProvider.GetRequiredService<ITenantImageFiles>().ContentUrl(tenantBLogoId);
        if (tenantBWithLogo.LogoUrl != tenantBLogoUrl)
        {
            tenantBWithLogo.LogoUrl = tenantBLogoUrl;
            _ = await tenantBParties.SaveChangesAsync(cancellationToken);
        }

        // Production startup binds the onboarding host to its platform-owned tenant; the E2E host skips module startup.
        Console.WriteLine("E2E fixture seeding: binding the onboarding host.");
        string? onboardingUrl = scope.ServiceProvider.GetRequiredService<IOptions<OnboardingHostOptions>>().Value.OnboardingUrl;
        if (!string.IsNullOrWhiteSpace(onboardingUrl))
        {
            _ = await scope.ServiceProvider.GetRequiredService<IAppTenantProvisioner>().EnsureAsync(new EnsureAppTenantRequest(
                OnboardingTenant.OwnerName,
                OnboardingTenant.AppId,
                OnboardingTenant.Name,
                new Uri(onboardingUrl, UriKind.Absolute).Host), cancellationToken);
        }

        Console.WriteLine("E2E fixture seeding: binding the sign-in host.");
        string? signInUrl = scope.ServiceProvider.GetRequiredService<IOptions<SignInHostOptions>>().Value.SignInUrl;
        if (!string.IsNullOrWhiteSpace(signInUrl))
        {
            _ = await scope.ServiceProvider.GetRequiredService<IAppTenantProvisioner>().EnsureAsync(new EnsureAppTenantRequest(
                SignInTenant.OwnerName,
                SignInTenant.AppId,
                SignInTenant.Name,
                new Uri(signInUrl, UriKind.Absolute).Host), cancellationToken);
        }

        Console.WriteLine("E2E fixture seeding: assigning platform-admin access.");
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
        if (!await users.IsInRoleAsync(emailSignInPlatformAdmin, IdentityDefinitions.PlatformAdmin))
        {
            IdentityResult result = await usersService.AddUserToRoleAsync(emailSignInPlatformAdmin, IdentityDefinitions.PlatformAdmin, cancellationToken);
            if (!result.Succeeded)
                throw new InvalidOperationException("E2E email sign-in platform-admin role assignment failed.");
        }
        if (!await users.IsInRoleAsync(twoFactorPlatformAdmin, IdentityDefinitions.PlatformAdmin))
        {
            IdentityResult result = await usersService.AddUserToRoleAsync(twoFactorPlatformAdmin, IdentityDefinitions.PlatformAdmin, cancellationToken);
            if (!result.Succeeded)
                throw new InvalidOperationException("E2E two-factor platform-admin role assignment failed.");
        }
        if (!await users.IsInRoleAsync(emailTwoFactorPlatformAdmin, IdentityDefinitions.PlatformAdmin))
        {
            IdentityResult result = await usersService.AddUserToRoleAsync(emailTwoFactorPlatformAdmin, IdentityDefinitions.PlatformAdmin, cancellationToken);
            if (!result.Succeeded)
                throw new InvalidOperationException("E2E email two-factor platform-admin role assignment failed.");
        }

    }

    private static async Task<User> EnsureUserAsync(
        UserManager<User> users,
        string userName,
        Guid id,
        string email,
        string firstName,
        string lastName,
        string password,
        bool emailConfirmed = true)
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
            EmailConfirmed = emailConfirmed,
            FirstName = firstName,
            LastName = lastName
        };
        IdentityResult created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
            throw new InvalidOperationException($"E2E user creation failed: {string.Join(';', created.Errors.Select(error => error.Code))}");
        return user;
    }

    private static async Task<User> EnsurePasswordlessPhoneAccountAsync(UserManager<User> users)
    {
        User? existing = await users.FindByNameAsync(PasswordlessPhoneAccountUserName);
        if (existing is not null)
        {
            if (await users.HasPasswordAsync(existing))
            {
                IdentityResult removedPassword = await users.RemovePasswordAsync(existing);
                if (!removedPassword.Succeeded)
                    throw new InvalidOperationException($"E2E passwordless phone account reset failed: {string.Join(';', removedPassword.Errors.Select(error => error.Code))}");
            }

            if (existing.TwoFactorEnabled)
            {
                IdentityResult disabledTwoFactor = await users.SetTwoFactorEnabledAsync(existing, false);
                if (!disabledTwoFactor.Succeeded)
                    throw new InvalidOperationException($"E2E passwordless phone account two-factor reset failed: {string.Join(';', disabledTwoFactor.Errors.Select(error => error.Code))}");
            }

            return existing;
        }

        User user = new()
        {
            Id = Guid.Parse("3bc0e1b2-041f-4e73-bc31-36f3b9c2a0ab").ToString(),
            UserName = PasswordlessPhoneAccountUserName,
            NormalizedUserName = PasswordlessPhoneAccountUserName.ToUpperInvariant(),
            Email = PasswordlessPhoneAccountEmail,
            NormalizedEmail = PasswordlessPhoneAccountEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            PhoneNumber = PasswordlessPhoneAccountPhoneNumber,
            PhoneNumberConfirmed = true,
            FirstName = "E2E",
            LastName = "Passwordless Account"
        };
        IdentityResult created = await users.CreateAsync(user);
        if (!created.Succeeded)
            throw new InvalidOperationException($"E2E passwordless phone account creation failed: {string.Join(';', created.Errors.Select(error => error.Code))}");
        return user;
    }

    private static async Task<User> EnsurePasswordlessEmailAccountAsync(UserManager<User> users)
    {
        User? existing = await users.FindByNameAsync(PasswordlessEmailAccountUserName);
        if (existing is not null)
        {
            // A rerun starts from a person who has no password yet.
            if (await users.HasPasswordAsync(existing))
            {
                IdentityResult removedPassword = await users.RemovePasswordAsync(existing);
                if (!removedPassword.Succeeded)
                    throw new InvalidOperationException($"E2E passwordless email account reset failed: {string.Join(';', removedPassword.Errors.Select(error => error.Code))}");
            }

            return existing;
        }

        User user = new()
        {
            Id = Guid.Parse("4d2e8a1c-6b3f-4a97-b1d5-9c7e0f2a3b68").ToString(),
            UserName = PasswordlessEmailAccountUserName,
            NormalizedUserName = PasswordlessEmailAccountUserName.ToUpperInvariant(),
            Email = PasswordlessEmailAccountEmail,
            NormalizedEmail = PasswordlessEmailAccountEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "E2E",
            LastName = "Platform Home Account"
        };
        IdentityResult created = await users.CreateAsync(user);
        if (!created.Succeeded)
            throw new InvalidOperationException($"E2E passwordless email account creation failed: {string.Join(';', created.Errors.Select(error => error.Code))}");
        return user;
    }

    /// <summary>Writes the patient's procedure project and derives the clinic patient view from it.</summary>
    private static async Task SeedPatientAsync(SeaDbContext sea, ISender sender, PatientWriteDto patient, CancellationToken cancellationToken)
    {
        if (!await sea.Projects.AnyAsync(project => project.Id == patient.Id, cancellationToken))
        {
            _ = sea.Projects.Add(new Vitarish.Sea.Domain.Entities.Project
            {
                Id = patient.Id,
                Name = patient.Name ?? string.Empty,
                IdentificationCode = patient.IdentificationCode,
                Template = patient.Template,
                Metadata = new Dictionary<string, object?>(patient.Metadata)
            });
            _ = await sea.SaveChangesAsync(cancellationToken);
        }

        await sender.Send(new PatientViewRefreshCommand(), cancellationToken);
    }

    private static Task EnsureObjectAsync(IObjectStorageService storage, string checksum, string content, CancellationToken cancellationToken) =>
        EnsureObjectAsync(storage, checksum, System.Text.Encoding.UTF8.GetBytes(content), "text/plain", cancellationToken);

    private static async Task EnsureObjectAsync(IObjectStorageService storage, string checksum, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        if (await storage.ExistsAsync(checksum, cancellationToken))
            return;

        await using MemoryStream stream = new(content, writable: false);
        await storage.UploadAsync(checksum, contentType, stream, stream.Length, cancellationToken);
    }
}
