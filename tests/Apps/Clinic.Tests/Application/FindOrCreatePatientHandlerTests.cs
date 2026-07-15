using FluentAssertions;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Commands.Patients;
using Vitarish.Clinic.Application.Handlers;
using Vitarish.Clinic.Application.Results;
using Vitarish.Clinic.Domain.Enums;
using Vitarish.Clinic.Domain.Models;

namespace Clinic.Tests.Application;

public sealed class FindOrCreatePatientHandlerTests
{
    [Fact]
    public async Task Existing_exact_match_is_returned_without_creating_a_patient()
    {
        PatientView existing = Patient(Guid.NewGuid(), "1234567890");
        StubPatientDirectory directory = new([existing]);
        FindOrCreatePatientHandler handler = new(directory);

        FindOrCreatePatientResult result = await handler.Handle(
            ValidCommand(identificationCode: " 1234567890 "),
            CancellationToken.None);

        result.Should().BeEquivalentTo(new FindOrCreatePatientResult.Success(existing, false));
        directory.CreatedPatient.Should().BeNull();
        directory.LookedUpCodes.Should().Equal("1234567890");
    }

    [Fact]
    public async Task Missing_match_creates_the_complete_normalized_patient_profile()
    {
        PatientView created = Patient(Guid.NewGuid(), "1234567890");
        StubPatientDirectory directory = new([null])
        {
            CreationResult = new PatientCreationResult.Success(created)
        };
        FindOrCreatePatientHandler handler = new(directory);

        FindOrCreatePatientResult result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().BeEquivalentTo(new FindOrCreatePatientResult.Success(created, true));
        directory.CreatedPatient.Should().NotBeNull();
        directory.CreatedPatient!.IdentificationCode.Should().Be("1234567890");
        directory.CreatedPatient.Name.Should().Be("Ada Lovelace");
        directory.CreatedPatient.FirstName.Should().Be("Ada");
        directory.CreatedPatient.LastName.Should().Be("Lovelace");
        directory.CreatedPatient.DateOfBirth.Should().Be(new DateTime(1815, 12, 10, 0, 0, 0, DateTimeKind.Utc));
        directory.CreatedPatient.Gender.Should().Be(Gender.Female);
        directory.CreatedPatient.Education.Should().Be(Education.Professional);
        directory.CreatedPatient.Phone.Should().Be("+44 20 0000 0000");
        directory.CreatedPatient.Height.Should().Be(165);
        directory.CreatedPatient.Allergies.Should().Be("None");
        directory.CreatedPatient.EmergencyContactName.Should().Be("Charles Babbage");
    }

    [Fact]
    public async Task Concurrent_identification_code_conflict_returns_the_winning_patient()
    {
        PatientView winner = Patient(Guid.NewGuid(), "1234567890");
        StubPatientDirectory directory = new([null, winner])
        {
            CreationResult = new PatientCreationResult.Failure(
                PatientCreationFailureReason.Conflict,
                "Unique identification code conflict.")
        };
        FindOrCreatePatientHandler handler = new(directory);

        FindOrCreatePatientResult result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().BeEquivalentTo(new FindOrCreatePatientResult.Success(winner, false));
        directory.LookedUpCodes.Should().Equal("1234567890", "1234567890");
    }

    [Fact]
    public async Task Missing_names_are_rejected_only_after_lookup_misses()
    {
        StubPatientDirectory directory = new([null]);
        FindOrCreatePatientHandler handler = new(directory);

        FindOrCreatePatientResult result = await handler.Handle(
            ValidCommand(firstName: " ", lastName: null),
            CancellationToken.None);

        FindOrCreatePatientResult.Failure failure = result.Should()
            .BeOfType<FindOrCreatePatientResult.Failure>().Subject;
        failure.Reason.Should().Be(PatientCreationFailureReason.Validation);
        failure.ValidationErrors!.Keys.Should().BeEquivalentTo("firstName", "lastName");
        directory.CreatedPatient.Should().BeNull();
    }

    private static FindOrCreatePatientCommand ValidCommand(
        string? identificationCode = "1234567890",
        string? firstName = " Ada ",
        string? lastName = " Lovelace ") => new(
            identificationCode,
            firstName,
            lastName,
            " +44 20 0000 0000 ",
            " ada@example.test ",
            new DateTime(1815, 12, 10, 0, 0, 0, DateTimeKind.Utc),
            null,
            Gender.Female,
            "London",
            "St James's Square",
            Education.Professional,
            "Mathematician",
            "Lord Byron",
            165,
            55,
            "Charles Babbage",
            "Private",
            "Consultation",
            "None",
            "None",
            "Analytical Engine notes",
            "Charles Babbage",
            "+44 20 1111 1111");

    private static PatientView Patient(Guid id, string identificationCode) => new()
    {
        Id = id,
        TenantId = "tenant-1",
        Name = "Ada Lovelace",
        IdentificationCode = identificationCode,
        CreatedAt = DateTime.UtcNow
    };

    private sealed class StubPatientDirectory(IEnumerable<PatientView?> lookupResults) : IPatientDirectory
    {
        private readonly Queue<PatientView?> _lookupResults = new(lookupResults);

        public List<string> LookedUpCodes { get; } = [];

        public PatientWriteDto? CreatedPatient { get; private set; }

        public PatientCreationResult CreationResult { get; init; } =
            new PatientCreationResult.Failure(PatientCreationFailureReason.Unexpected, "Not configured.");

        public Task<PatientView?> FindByIdentificationCodeAsync(
            string identificationCode,
            CancellationToken cancellationToken)
        {
            LookedUpCodes.Add(identificationCode);
            return Task.FromResult(_lookupResults.Dequeue());
        }

        public Task<PatientCreationResult> CreateAsync(
            PatientWriteDto patient,
            CancellationToken cancellationToken)
        {
            CreatedPatient = patient;
            return Task.FromResult(CreationResult);
        }
    }
}
