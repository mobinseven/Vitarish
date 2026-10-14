using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Clinic.Infrastructure.Serializers;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Communications.Domain.Enums;
using Vitarish.Modules.Base.Core.Platform.Idempotency;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Tests.Infrastructure;

public sealed class SurgeryToolchainTests
{
    private const string TenantId = "11111111-1111-1111-1111-111111111111";
    private const string OtherTenantId = "99999999-9999-9999-9999-999999999999";
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CompleteAsync_before_the_scheduled_start_throws_and_writes_nothing()
    {
        Guid surgeryId = Guid.NewGuid();
        Guid patientId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        Mock<IRemindersService> reminders = new();
        projectObjects.Setup(value => value.GetAsync(surgeryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Read(surgeryId, patientId, SurgeryStatus.Planned, Now.AddHours(1)));

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, reminders, db);

        Func<Task> action = () => toolchain.CompleteAsync(surgeryId, Guid.NewGuid(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        projectObjects.Verify(value => value.UpdateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<JsonElement>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        reminders.Verify(value => value.ReplaceBySourceAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderWriteDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_after_the_start_marks_the_surgery_completed_and_clears_pending_reminders()
    {
        Guid surgeryId = Guid.NewGuid();
        Guid patientId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        Mock<IRemindersService> reminders = new();
        projectObjects.Setup(value => value.GetAsync(surgeryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Read(surgeryId, patientId, SurgeryStatus.Planned, Now.AddHours(-1)));
        JsonElement updatedData = default;
        projectObjects.Setup(value => value.UpdateAsync(
                surgeryId, SurgeryObjectTypes.OwnerEntityType, patientId, SurgeryObjectTypes.ObjectType,
                It.IsAny<JsonElement>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, string ownerEntityType, Guid ownerEntityId, string objectType, JsonElement data, Guid token, CancellationToken _) =>
            {
                updatedData = data;
                return new ProjectObjectReadDto(id, ownerEntityType, ownerEntityId, objectType, data, Guid.NewGuid(), Now.UtcDateTime, null, null, null, null, null);
            });
        IReadOnlyList<ReminderWriteDto>? replaced = null;
        reminders.Setup(value => value.ReplaceBySourceAsync(surgeryId, It.IsAny<IReadOnlyList<ReminderWriteDto>>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, IReadOnlyList<ReminderWriteDto> list, CancellationToken _) =>
            {
                replaced = list;
                return Task.CompletedTask;
            });

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, reminders, db);

        SurgeryReadModel? result = await toolchain.CompleteAsync(surgeryId, Guid.NewGuid(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().Be(SurgeryStatus.Completed);
        updatedData.GetProperty("Status").GetString().Should().Be(nameof(SurgeryStatus.Completed));
        replaced.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_on_a_completed_surgery_throws_and_writes_nothing()
    {
        Guid surgeryId = Guid.NewGuid();
        Guid patientId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        projectObjects.Setup(value => value.GetAsync(surgeryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Read(surgeryId, patientId, SurgeryStatus.Completed, Now.AddHours(-1)));

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, new Mock<IRemindersService>(), db);

        SurgeryWriteModel surgery = new(
            surgeryId, Guid.Empty, SurgeryCatalogDefaults.LegacyGeneralSurgeryId, "Surgery", null, SurgeryStatus.Planned,
            Now.AddHours(1), [], [], [], [], Guid.NewGuid(), 30, "OR 1", null);
        Func<Task> action = () => toolchain.UpdateAsync(surgery, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        projectObjects.Verify(value => value.UpdateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<JsonElement>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_throws_when_the_caller_may_not_plan_and_writes_nothing()
    {
        Guid patientId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        Mock<IRemindersService> reminders = new();

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, reminders, db, authorization: PlanningMock(canPlan: false));

        SurgeryWriteModel surgery = new(
            Guid.Empty, patientId, SurgeryCatalogDefaults.LegacyGeneralSurgeryId, "Surgery", "key", SurgeryStatus.Planned,
            Now.AddHours(1), [], [], [], [], Guid.Empty, 30, "OR 1", null);
        Func<Task> action = () => toolchain.CreateAsync(surgery, CancellationToken.None);

        await action.Should().ThrowAsync<SurgeryPlanningForbiddenException>();
        projectObjects.Verify(value => value.CreateAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<JsonElement>(),
            It.IsAny<CancellationToken>(), It.IsAny<Guid?>()), Times.Never);
        reminders.Verify(value => value.ReplaceBySourceAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderWriteDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_throws_when_the_caller_may_not_plan_and_writes_nothing()
    {
        Guid surgeryId = Guid.NewGuid();
        Guid patientId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        projectObjects.Setup(value => value.GetAsync(surgeryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Read(surgeryId, patientId, SurgeryStatus.Planned, Now.AddHours(1)));

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, new Mock<IRemindersService>(), db, authorization: PlanningMock(canPlan: false));

        SurgeryWriteModel surgery = new(
            surgeryId, Guid.Empty, SurgeryCatalogDefaults.LegacyGeneralSurgeryId, "Surgery", null, SurgeryStatus.Planned,
            Now.AddHours(1), [], [], [], [], Guid.NewGuid(), 30, "OR 1", null);
        Func<Task> action = () => toolchain.UpdateAsync(surgery, CancellationToken.None);

        await action.Should().ThrowAsync<SurgeryPlanningForbiddenException>();
        projectObjects.Verify(value => value.UpdateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<JsonElement>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_throws_when_the_caller_may_not_plan_and_writes_nothing()
    {
        Guid surgeryId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, new Mock<IRemindersService>(), db, authorization: PlanningMock(canPlan: false));

        Func<Task> action = () => toolchain.CompleteAsync(surgeryId, Guid.NewGuid(), CancellationToken.None);

        await action.Should().ThrowAsync<SurgeryPlanningForbiddenException>();
        projectObjects.Verify(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        projectObjects.Verify(value => value.UpdateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<JsonElement>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_throws_when_the_caller_may_not_plan_and_writes_nothing()
    {
        Guid surgeryId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, new Mock<IRemindersService>(), db, authorization: PlanningMock(canPlan: false));

        Func<Task> action = () => toolchain.CancelAsync(surgeryId, Guid.NewGuid(), CancellationToken.None);

        await action.Should().ThrowAsync<SurgeryPlanningForbiddenException>();
        projectObjects.Verify(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        projectObjects.Verify(value => value.UpdateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<JsonElement>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_sends_the_clinics_cancellation_message_to_every_recipient()
    {
        Guid surgeryId = Guid.NewGuid();
        Guid patientId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        Mock<IRemindersService> reminders = new();
        Mock<IPatientDirectory> patients = new();
        Mock<ISurgerySettingsStore> settingsStore = new();
        projectObjects.Setup(value => value.GetAsync(surgeryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Read(surgeryId, patientId, SurgeryStatus.Planned, Now.AddHours(1)));
        projectObjects.Setup(value => value.UpdateAsync(
                surgeryId, SurgeryObjectTypes.OwnerEntityType, patientId, SurgeryObjectTypes.ObjectType,
                It.IsAny<JsonElement>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, string ownerEntityType, Guid ownerEntityId, string objectType, JsonElement data, Guid token, CancellationToken _) =>
                new ProjectObjectReadDto(id, ownerEntityType, ownerEntityId, objectType, data, Guid.NewGuid(), Now.UtcDateTime, null, null, null, null, null));
        patients.Setup(value => value.FindByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientView { Id = patientId, Name = "Patient One", Phone = "+111", Email = "patient@example.com" });
        settingsStore.Setup(value => value.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SurgerySettings("Clinic cancellation", "The clinic cancelled this surgery.", Guid.NewGuid()));
        IReadOnlyList<ReminderWriteDto>? sent = null;
        reminders.Setup(value => value.ReplaceBySourceAsync(surgeryId, It.IsAny<IReadOnlyList<ReminderWriteDto>>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, IReadOnlyList<ReminderWriteDto> list, CancellationToken _) =>
            {
                sent = list;
                return Task.CompletedTask;
            });

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, reminders, db, patients, settingsStore: settingsStore);

        SurgeryReadModel? result = await toolchain.CancelAsync(surgeryId, Guid.NewGuid(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().Be(SurgeryStatus.Cancelled);
        sent.Should().NotBeNull().And.NotBeEmpty();
        sent!.Should().OnlyContain(reminder =>
            reminder.Subject == "Clinic cancellation" && reminder.TextBody == "The clinic cancelled this surgery.");
    }

    [Fact]
    public async Task CancelAsync_falls_back_to_the_built_in_cancellation_message_without_settings()
    {
        Guid surgeryId = Guid.NewGuid();
        Guid patientId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        Mock<IRemindersService> reminders = new();
        Mock<IPatientDirectory> patients = new();
        Mock<ISurgerySettingsStore> settingsStore = new();
        projectObjects.Setup(value => value.GetAsync(surgeryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Read(surgeryId, patientId, SurgeryStatus.Planned, Now.AddHours(1)));
        projectObjects.Setup(value => value.UpdateAsync(
                surgeryId, SurgeryObjectTypes.OwnerEntityType, patientId, SurgeryObjectTypes.ObjectType,
                It.IsAny<JsonElement>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, string ownerEntityType, Guid ownerEntityId, string objectType, JsonElement data, Guid token, CancellationToken _) =>
                new ProjectObjectReadDto(id, ownerEntityType, ownerEntityId, objectType, data, Guid.NewGuid(), Now.UtcDateTime, null, null, null, null, null));
        patients.Setup(value => value.FindByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientView { Id = patientId, Name = "Patient One", Phone = "+111", Email = "patient@example.com" });
        settingsStore.Setup(value => value.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SurgerySettings?)null);
        IReadOnlyList<ReminderWriteDto>? sent = null;
        reminders.Setup(value => value.ReplaceBySourceAsync(surgeryId, It.IsAny<IReadOnlyList<ReminderWriteDto>>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, IReadOnlyList<ReminderWriteDto> list, CancellationToken _) =>
            {
                sent = list;
                return Task.CompletedTask;
            });

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, reminders, db, patients, settingsStore: settingsStore);

        SurgeryReadModel? result = await toolchain.CancelAsync(surgeryId, Guid.NewGuid(), CancellationToken.None);

        result.Should().NotBeNull();
        sent.Should().NotBeNull().And.NotBeEmpty();
        sent!.Should().OnlyContain(reminder =>
            reminder.Subject == "Surgery cancelled" && reminder.TextBody == "The scheduled surgery has been cancelled.");
    }

    [Fact]
    public async Task CreateAsync_rejects_a_team_member_outside_the_clinic()
    {
        Guid patientId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        Mock<IPatientDirectory> patients = new();
        Mock<ISurgeryCatalogStore> catalogs = new();
        Mock<ISurgeryTeamDirectory> teamDirectory = new();
        Mock<IOperationReceiptStore<SurgeryCreateOperation>> operations = PermissiveOperations();
        patients.Setup(value => value.FindByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientView { Id = patientId, Name = "Patient One" });
        catalogs.Setup(value => value.FindAsync(SurgeryCatalogDefaults.LegacyGeneralSurgeryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Catalog());
        teamDirectory.Setup(value => value.FindMembersAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SurgeryTeamContact("u1", "Dr. Who", "doc@example.com", "+222")]);

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, new Mock<IRemindersService>(), db, patients, operations, catalogs, teamDirectory);

        SurgeryWriteModel surgery = new(
            Guid.Empty, patientId, SurgeryCatalogDefaults.LegacyGeneralSurgeryId, "Surgery", "key", SurgeryStatus.Planned,
            Now.AddHours(1), [], [], [], [Policy("First")], Guid.Empty, 30, "OR 1",
            [new("u1", "Client One", "Surgeon"), new("u2", "Client Two", "Nurse")]);
        Func<Task> action = () => toolchain.CreateAsync(surgery, CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
        projectObjects.Verify(value => value.CreateAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<JsonElement>(),
            It.IsAny<CancellationToken>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_rejects_an_unknown_surgery_type()
    {
        Guid patientId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        Mock<IPatientDirectory> patients = new();
        Mock<ISurgeryCatalogStore> catalogs = new();
        Mock<IOperationReceiptStore<SurgeryCreateOperation>> operations = PermissiveOperations();
        patients.Setup(value => value.FindByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientView { Id = patientId, Name = "Patient One" });
        catalogs.Setup(value => value.FindAsync("missing-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SurgeryCatalog?)null);

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, new Mock<IRemindersService>(), db, patients, operations, catalogs, new Mock<ISurgeryTeamDirectory>());

        SurgeryWriteModel surgery = new(
            Guid.Empty, patientId, "missing-type", "Surgery", "key", SurgeryStatus.Planned,
            Now.AddHours(1), [], [], [], [Policy("First")], Guid.Empty, 30, "OR 1", null);
        Func<Task> action = () => toolchain.CreateAsync(surgery, CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
        projectObjects.Verify(value => value.CreateAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<JsonElement>(),
            It.IsAny<CancellationToken>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_resolves_team_names_and_sends_reminders_to_the_patient_and_every_team_member()
    {
        Guid patientId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        Mock<IRemindersService> reminders = new();
        Mock<IPatientDirectory> patients = new();
        Mock<ISurgeryCatalogStore> catalogs = new();
        Mock<ISurgeryTeamDirectory> teamDirectory = new();
        Mock<IOperationReceiptStore<SurgeryCreateOperation>> operations = PermissiveOperations();
        patients.Setup(value => value.FindByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientView { Id = patientId, Name = "Patient One", Phone = "+111", Email = "patient@example.com" });
        catalogs.Setup(value => value.FindAsync(SurgeryCatalogDefaults.LegacyGeneralSurgeryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Catalog());
        teamDirectory.Setup(value => value.FindMembersAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new SurgeryTeamContact("u1", "Dr. Who", "doc@example.com", "+222"),
                new SurgeryTeamContact("u2", "Nurse Joy", "joy@example.com", "+333")
            ]);
        JsonElement captured = default;
        projectObjects.Setup(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectObjectReadDto?)null);
        projectObjects.Setup(value => value.CreateAsync(
                SurgeryObjectTypes.OwnerEntityType, patientId, SurgeryObjectTypes.ObjectType,
                It.IsAny<JsonElement>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()))
            .ReturnsAsync((string ownerEntityType, Guid ownerEntityId, string objectType, JsonElement data, CancellationToken _, Guid? id) =>
            {
                captured = data;
                return new ProjectObjectReadDto(id!.Value, ownerEntityType, ownerEntityId, objectType, data, Guid.NewGuid(), Now.UtcDateTime, null, null, null, null, null);
            });
        IReadOnlyList<ReminderWriteDto>? sent = null;
        reminders.Setup(value => value.ReplaceBySourceAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderWriteDto>>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, IReadOnlyList<ReminderWriteDto> list, CancellationToken _) =>
            {
                sent = list;
                return Task.CompletedTask;
            });

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, reminders, db, patients, operations, catalogs, teamDirectory);

        SurgeryWriteModel surgery = new(
            Guid.Empty, patientId, SurgeryCatalogDefaults.LegacyGeneralSurgeryId, "Surgery", "key", SurgeryStatus.Planned,
            Now.AddDays(3), [], [], [], [Policy("First")], Guid.Empty, 60, "  OR 2  ",
            [new(" u1 ", "Client One", " Surgeon "), new("u2", "Client Two", "Nurse")]);

        SurgeryReadModel result = await toolchain.CreateAsync(surgery, CancellationToken.None);

        result.Status.Should().Be(SurgeryStatus.Planned);
        SurgeryWriteModel stored = JsonSerializer.Deserialize(captured, SurgeryJsonSerializerContext.Default.SurgeryWriteModel)!;
        stored.Location.Should().Be("OR 2");
        stored.Team!.Select(member => member.UserId).Should().Equal("u1", "u2");
        stored.Team!.Select(member => member.DisplayName).Should().Equal("Dr. Who", "Nurse Joy");
        stored.Team!.Select(member => member.Role).Should().Equal("Surgeon", "Nurse");
        stored.PersonsToNotify.Should().Equal("Surgeon", "Nurse");

        sent.Should().NotBeNull();
        sent!.Select(reminder => reminder.Recipient)
            .Should().BeEquivalentTo("+111", "patient@example.com", "doc@example.com", "+222", "joy@example.com", "+333");
        sent!.Should().Contain(reminder => reminder.Kind == ReminderKind.Sms && reminder.Recipient == "+111");
        sent!.Should().Contain(reminder => reminder.Kind == ReminderKind.Email && reminder.Recipient == "patient@example.com");
        sent!.Should().Contain(reminder => reminder.Kind == ReminderKind.Email && reminder.Recipient == "doc@example.com");
        sent!.Should().Contain(reminder => reminder.Kind == ReminderKind.Sms && reminder.Recipient == "+222");
    }

    [Fact]
    public async Task CreateAsync_accepts_the_surgery_back_from_a_store_that_reserializes_its_data()
    {
        Guid patientId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        Mock<IRemindersService> reminders = new();
        Mock<IPatientDirectory> patients = new();
        Mock<ISurgeryCatalogStore> catalogs = new();
        Mock<ISurgeryTeamDirectory> teamDirectory = new();
        patients.Setup(value => value.FindByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientView { Id = patientId, Name = "Patient One" });
        catalogs.Setup(value => value.FindAsync(SurgeryCatalogDefaults.LegacyGeneralSurgeryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Catalog());
        projectObjects.Setup(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectObjectReadDto?)null);
        projectObjects.Setup(value => value.CreateAsync(
                SurgeryObjectTypes.OwnerEntityType, patientId, SurgeryObjectTypes.ObjectType,
                It.IsAny<JsonElement>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()))
            .ReturnsAsync((string ownerEntityType, Guid ownerEntityId, string objectType, JsonElement data, CancellationToken _, Guid? id) =>
            {
                // The SEA store keeps data as a dictionary and serializes it again on read.
                Dictionary<string, object?> stored = JsonSerializer.Deserialize<Dictionary<string, object?>>(data.GetRawText())!;
                JsonElement reread = JsonSerializer.SerializeToElement(stored);
                return new ProjectObjectReadDto(id!.Value, ownerEntityType, ownerEntityId, objectType, reread, Guid.NewGuid(), Now.UtcDateTime, null, null, null, null, null);
            });

        await using InMemoryClinicDbContext db = CreateContext();
        SurgeryToolchain toolchain = CreateToolchain(projectObjects, reminders, db, patients, PermissiveOperations(), catalogs, teamDirectory);

        SurgeryWriteModel surgery = new(
            Guid.Empty, patientId, SurgeryCatalogDefaults.LegacyGeneralSurgeryId, "جراحی بینی", "fresh-key", SurgeryStatus.Planned,
            new DateTimeOffset(2026, 7, 1, 9, 30, 0, TimeSpan.FromHours(3.5)), ["قیچی"], [], [], [Policy("یادآوری")], Guid.Empty, 90, "اتاق عمل ۲", []);

        SurgeryReadModel result = await toolchain.CreateAsync(surgery, CancellationToken.None);

        result.Name.Should().Be("جراحی بینی");
        result.Location.Should().Be("اتاق عمل ۲");
    }

    [Fact]
    public async Task FindConflictsAsync_returns_only_overlapping_tenant_planned_surgeries_that_share_a_location_or_team_member()
    {
        Guid patientId = Guid.NewGuid();
        Guid sameLocationId = Guid.NewGuid();
        Guid sharedMemberId = Guid.NewGuid();
        Guid nonOverlappingId = Guid.NewGuid();
        Guid cancelledId = Guid.NewGuid();
        Guid otherTenantId = Guid.NewGuid();
        Guid excludedId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();

        await using InMemoryClinicDbContext db = CreateContext();
        db.SurgeryViews.AddRange(
            Row(sameLocationId, patientId, TenantId, SurgeryStatus.Planned, Now, 60, "OR 2"),
            Row(sharedMemberId, patientId, TenantId, SurgeryStatus.Planned, Now.AddMinutes(30), 60, "OR 3", [new("u1", "Dr", "Surgeon")]),
            Row(nonOverlappingId, patientId, TenantId, SurgeryStatus.Planned, Now.AddHours(10), 60, "OR 2"),
            Row(cancelledId, patientId, TenantId, SurgeryStatus.Cancelled, Now, 60, "OR 2"),
            Row(otherTenantId, patientId, OtherTenantId, SurgeryStatus.Planned, Now, 60, "OR 2"),
            Row(excludedId, patientId, TenantId, SurgeryStatus.Planned, Now, 60, "OR 2"));
        await db.SaveChangesAsync();

        SurgeryToolchain toolchain = CreateToolchain(projectObjects, new Mock<IRemindersService>(), db);

        IReadOnlyList<SurgeryConflict> conflicts = await toolchain.FindConflictsAsync(
            new SurgeryConflictProbe(Now, 60, "OR 2", ["u1"], excludedId), CancellationToken.None);

        conflicts.Select(conflict => conflict.Entry.Surgery.Id).Should().Equal(sameLocationId, sharedMemberId);
        conflicts.Single(conflict => conflict.Entry.Surgery.Id == sameLocationId).Reasons.Should().Equal(SurgeryConflictReason.SameLocation);
        SurgeryConflict shared = conflicts.Single(conflict => conflict.Entry.Surgery.Id == sharedMemberId);
        shared.Reasons.Should().Equal(SurgeryConflictReason.SharedTeamMember);
        shared.SharedTeamUserIds.Should().Equal("u1");
    }

    [Fact]
    public async Task GetCalendarAsync_returns_in_range_tenant_surgeries_of_every_status_with_patient_names()
    {
        Guid patientId = Guid.NewGuid();
        Guid plannedId = Guid.NewGuid();
        Guid cancelledId = Guid.NewGuid();
        Guid completedId = Guid.NewGuid();
        Guid otherTenantId = Guid.NewGuid();
        Guid beforeRangeId = Guid.NewGuid();
        Guid afterRangeId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();

        await using InMemoryClinicDbContext db = CreateContext();
        db.PatientViews.Add(new PatientView { Id = patientId, TenantId = TenantId, Name = "Patient One" });
        db.SurgeryViews.AddRange(
            Row(plannedId, patientId, TenantId, SurgeryStatus.Planned, new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero), 60, "OR 2"),
            Row(cancelledId, Guid.NewGuid(), TenantId, SurgeryStatus.Cancelled, new DateTimeOffset(2026, 6, 3, 9, 0, 0, TimeSpan.Zero), null, null),
            Row(completedId, Guid.NewGuid(), TenantId, SurgeryStatus.Completed, new DateTimeOffset(2026, 6, 4, 9, 0, 0, TimeSpan.Zero), 30, null),
            Row(otherTenantId, Guid.NewGuid(), OtherTenantId, SurgeryStatus.Planned, new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero), 60, "OR 2"),
            Row(beforeRangeId, Guid.NewGuid(), TenantId, SurgeryStatus.Planned, new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero), 60, "OR 2"),
            Row(afterRangeId, Guid.NewGuid(), TenantId, SurgeryStatus.Planned, new DateTimeOffset(2026, 6, 9, 9, 0, 0, TimeSpan.Zero), 60, "OR 2"));
        await db.SaveChangesAsync();

        SurgeryToolchain toolchain = CreateToolchain(projectObjects, new Mock<IRemindersService>(), db);

        IReadOnlyList<SurgeryCalendarEntry> entries = await toolchain.GetCalendarAsync(
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 8, 0, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        entries.Select(entry => entry.Surgery.Id).Should().Equal(plannedId, cancelledId, completedId);
        entries.Single(entry => entry.Surgery.Id == plannedId).PatientDisplayName.Should().Be("Patient One");
        entries.Where(entry => entry.Surgery.Id != plannedId).Select(entry => entry.PatientDisplayName).Should().OnlyContain(name => name == null);
    }

    private static Mock<IOperationReceiptStore<SurgeryCreateOperation>> PermissiveOperations()
    {
        Mock<IOperationReceiptStore<SurgeryCreateOperation>> operations = new();
        operations.Setup(value => value.GetAsync(It.IsAny<OperationReceiptKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OperationReceipt<SurgeryCreateOperation>?)null);
        operations.Setup(value => value.TryClaimAsync(
                It.IsAny<OperationReceiptKey>(), It.IsAny<SurgeryCreateOperation>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        operations.Setup(value => value.TryUpdateAsync(
                It.IsAny<OperationReceiptKey>(), It.IsAny<long>(), It.IsAny<SurgeryCreateOperation>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return operations;
    }

    private static InMemoryClinicDbContext CreateContext()
    {
        DbContextOptions<ClinicDbContext> options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InMemoryClinicDbContext(options, new TestRequestContext(TenantId), new HttpContextAccessor());
    }

    private static SurgeryToolchain CreateToolchain(
        Mock<IProjectObjectService> projectObjects,
        Mock<IRemindersService> reminders,
        ClinicDbContext dbContext,
        Mock<IPatientDirectory>? patients = null,
        Mock<IOperationReceiptStore<SurgeryCreateOperation>>? operations = null,
        Mock<ISurgeryCatalogStore>? catalogs = null,
        Mock<ISurgeryTeamDirectory>? teamDirectory = null,
        Mock<ISurgerySettingsStore>? settingsStore = null,
        Mock<ISurgeryPlanningAuthorization>? authorization = null) =>
        new(
            projectObjects: projectObjects.Object,
            reminders: reminders.Object,
            patients: (patients ?? new Mock<IPatientDirectory>()).Object,
            dbContext: dbContext,
            requestContext: new TestRequestContext(TenantId),
            operations: (operations ?? new Mock<IOperationReceiptStore<SurgeryCreateOperation>>()).Object,
            catalogs: (catalogs ?? new Mock<ISurgeryCatalogStore>()).Object,
            teamDirectory: (teamDirectory ?? new Mock<ISurgeryTeamDirectory>()).Object,
            timeProvider: new FakeTimeProvider(Now),
            settingsStore: (settingsStore ?? new Mock<ISurgerySettingsStore>()).Object,
            authorization: (authorization ?? PlanningMock(canPlan: true)).Object);

    private static Mock<ISurgeryPlanningAuthorization> PlanningMock(bool canPlan)
    {
        Mock<ISurgeryPlanningAuthorization> authorization = new();
        authorization.Setup(value => value.CanPlan()).Returns(canPlan);
        return authorization;
    }

    private static SurgeryReminderPolicy Policy(string name) => new(name, 60, "Subject", "Body");

    private static SurgeryCatalog Catalog() =>
        new(SurgeryCatalogDefaults.LegacyGeneralSurgeryId, "General Surgery", ["scalpel"], ["Surgeon"], [Policy("Default")], Guid.NewGuid());

    private static ProjectObjectReadDto Read(Guid id, Guid patientId, SurgeryStatus status, DateTimeOffset scheduledAt) =>
        new(id, SurgeryObjectTypes.OwnerEntityType, patientId, SurgeryObjectTypes.ObjectType,
            JsonSerializer.SerializeToElement(
                new SurgeryWriteModel(id, patientId, SurgeryCatalogDefaults.LegacyGeneralSurgeryId, "Surgery", "key", status,
                    scheduledAt, [], [], [], [], Guid.NewGuid(), 30, "OR 1", null),
                SurgeryJsonSerializerContext.Default.SurgeryWriteModel),
            Guid.NewGuid(), Now.UtcDateTime, null, null, null, null, null);

    private static SurgeryView Row(
        Guid id,
        Guid patientId,
        string tenantId,
        SurgeryStatus status,
        DateTimeOffset scheduledAt,
        int? durationMinutes,
        string? location,
        IReadOnlyList<SurgeryTeamMember>? team = null) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            PatientId = patientId,
            ObjectType = SurgeryObjectTypes.ObjectType,
            ConcurrencyToken = Guid.NewGuid(),
            Data = JsonDocument.Parse(JsonSerializer.Serialize(
                new SurgeryWriteModel(id, patientId, SurgeryCatalogDefaults.LegacyGeneralSurgeryId, "Surgery", "key", status,
                    scheduledAt, [], [], [], [], Guid.NewGuid(), durationMinutes, location, team),
                SurgeryJsonSerializerContext.Default.SurgeryWriteModel)),
            Status = status.ToString(),
            ScheduledAt = scheduledAt,
            CreatedAt = Now.UtcDateTime,
            UpdatedAt = null,
            DurationMinutes = durationMinutes,
            Location = location,
            Team = JsonDocument.Parse("[]")
        };

    private sealed class InMemoryClinicDbContext(
        DbContextOptions<ClinicDbContext> options,
        IRequestContext requestContext,
        IHttpContextAccessor httpContextAccessor)
        : ClinicDbContext(options, requestContext, httpContextAccessor)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            _ = modelBuilder.Entity<PatientView>().HasKey(patient => patient.Id);
            _ = modelBuilder.Entity<SurgeryView>().HasKey(surgery => surgery.Id);
            _ = modelBuilder.Entity<SurgerySuggestionView>().HasKey(suggestion => new { suggestion.TenantId, suggestion.Kind, suggestion.Value });
        }
    }

    private sealed class TestRequestContext(string tenantId) : IRequestContext
    {
        public string Authority => "clinic.tests";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => "clinic-surgery-test";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { RequestId = "clinic-surgery-test", Authority = Authority, TenantId = TenantId };
    }
}
