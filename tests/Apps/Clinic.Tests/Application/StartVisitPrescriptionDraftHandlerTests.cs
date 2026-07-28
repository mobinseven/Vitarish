using FluentAssertions;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Commands.Visits;
using Vitarish.Clinic.Application.Handlers;
using Vitarish.Clinic.Application.Results;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Modules.Base.Core.Platform.Idempotency;

namespace Vitarish.Clinic.Tests.Application;

public sealed class StartVisitPrescriptionDraftHandlerTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid ClinicId = Guid.NewGuid();
    private static readonly Guid DoctorId = Guid.NewGuid();

    [Fact]
    public async Task Creates_one_event_and_linked_draft()
    {
        Fixture fixture = new();

        StartVisitPrescriptionDraftResult result = await fixture.Handler.Handle(
            Command(),
            CancellationToken.None);

        result.Outcome.Should().Be(StartVisitPrescriptionDraftOutcome.Succeeded);
        result.EventId.Should().Be(fixture.Visits.EventId);
        result.PrescriptionId.Should().NotBeNull();
        result.PrescriptionId!.Value.Should().NotBe(Guid.Empty);
        fixture.Visits.CreateCalls.Should().Be(1);
        fixture.Prescriptions.CreateCalls.Should().Be(1);
        fixture.Prescriptions.LastEncounterId.Should().Be(result.EventId!.Value);
        fixture.Prescriptions.LastPatientId.Should().Be(PatientId);
        fixture.Prescriptions.LastDoctorId.Should().Be(DoctorId);
        fixture.Prescriptions.LastClinicId.Should().Be(ClinicId);
    }

    [Fact]
    public async Task Replays_completed_receipt_without_new_mutations()
    {
        Fixture fixture = new();
        StartVisitPrescriptionDraftCommand command = Command();

        StartVisitPrescriptionDraftResult first = await fixture.Handler.Handle(command, CancellationToken.None);
        StartVisitPrescriptionDraftResult replay = await fixture.Handler.Handle(command, CancellationToken.None);

        replay.Should().BeEquivalentTo(first);
        fixture.Visits.CreateCalls.Should().Be(1);
        fixture.Prescriptions.CreateCalls.Should().Be(1);
    }

    [Fact]
    public async Task Concurrent_duplicate_requests_claim_one_operation()
    {
        Fixture fixture = new();
        fixture.Visits.Delay = TimeSpan.FromMilliseconds(25);
        StartVisitPrescriptionDraftCommand command = Command();

        StartVisitPrescriptionDraftResult[] results = await Task.WhenAll(
            fixture.Handler.Handle(command, CancellationToken.None),
            fixture.Handler.Handle(command, CancellationToken.None));

        results.Should().Contain(result => result.Outcome == StartVisitPrescriptionDraftOutcome.Succeeded);
        results.Should().OnlyContain(result =>
            result.Outcome == StartVisitPrescriptionDraftOutcome.Succeeded ||
            result.Outcome == StartVisitPrescriptionDraftOutcome.InProgress);
        fixture.Visits.CreateCalls.Should().Be(1);
        fixture.Prescriptions.CreateCalls.Should().Be(1);
    }

    [Fact]
    public async Task Compensates_empty_event_when_medical_rejects_draft()
    {
        Fixture fixture = new();
        fixture.Prescriptions.Result = new(false, Error: "medical unavailable");

        StartVisitPrescriptionDraftResult result = await fixture.Handler.Handle(
            Command(),
            CancellationToken.None);

        result.Outcome.Should().Be(StartVisitPrescriptionDraftOutcome.DependencyFailure);
        fixture.Visits.RemoveCalls.Should().Be(1);
        result.EventId.Should().BeNull();
        result.PrescriptionId.Should().NotBeNull();
        result.PrescriptionId!.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Does_not_replay_unknown_event_mutation_outcome()
    {
        Fixture fixture = new();
        fixture.Visits.ThrowOnCreate = true;
        StartVisitPrescriptionDraftCommand command = Command();

        StartVisitPrescriptionDraftResult first = await fixture.Handler.Handle(command, CancellationToken.None);
        StartVisitPrescriptionDraftResult replay = await fixture.Handler.Handle(command, CancellationToken.None);

        first.Outcome.Should().Be(StartVisitPrescriptionDraftOutcome.UnknownMutationOutcome);
        replay.Outcome.Should().Be(StartVisitPrescriptionDraftOutcome.UnknownMutationOutcome);
        fixture.Visits.CreateCalls.Should().Be(1);
        fixture.Prescriptions.CreateCalls.Should().Be(0);
    }

    private static StartVisitPrescriptionDraftCommand Command() =>
        new(PatientId, "tamin", "Visit", "operation-1");

    private sealed class Fixture
    {
        public FakeVisits Visits { get; } = new();
        public FakePrescriptions Prescriptions { get; } = new();
        public StartVisitPrescriptionDraftHandler Handler { get; }

        public Fixture()
        {
            Handler = new(
                new FakePatients(),
                new FakeActorContext(),
                Visits,
                Prescriptions,
                new InMemoryReceiptStore<VisitPrescriptionOperation>());
        }
    }

    private sealed class FakeActorContext : IClinicActorContext
    {
        public bool TryGetActor(out ClinicActor actor)
        {
            actor = new(ClinicId.ToString(), ClinicId, DoctorId);
            return true;
        }
    }

    private sealed class FakePatients : IPatientDirectory
    {
        public Task<PatientView?> FindByIdAsync(Guid patientId, CancellationToken cancellationToken) =>
            Task.FromResult<PatientView?>(patientId == PatientId
                ? new PatientView
                {
                    Id = PatientId,
                    TenantId = ClinicId.ToString(),
                    Name = "Patient",
                    IdentificationCode = "001"
                }
                : null);

        public Task<PatientView?> FindByIdentificationCodeAsync(string identificationCode, CancellationToken cancellationToken) =>
            Task.FromResult<PatientView?>(null);

        public Task<PatientCreationResult> CreateAsync(PatientWriteDto patient, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PatientUpdateResult> UpdateAsync(PatientWriteDto patient, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeVisits : IVisitsEventWriter
    {
        public Guid ProcedureId { get; } = Guid.NewGuid();
        public Guid EventId { get; } = Guid.NewGuid();
        public int CreateCalls;
        public int RemoveCalls;
        public bool ThrowOnCreate { get; set; }
        public TimeSpan Delay { get; set; }

        public async Task<VisitEventCreationResult> CreateAsync(
            Guid patientId,
            string title,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref CreateCalls);
            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }

            if (ThrowOnCreate)
            {
                throw new InvalidOperationException("unknown");
            }

            return new(true, ProcedureId, EventId);
        }

        public Task<bool> RemoveEmptyAsync(
            Guid procedureId,
            Guid eventId,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref RemoveCalls);
            return Task.FromResult(true);
        }
    }

    private sealed class FakePrescriptions : IPrescriptionDraftWriter
    {
        public int CreateCalls;
        public Guid LastPatientId { get; private set; }
        public Guid LastDoctorId { get; private set; }
        public Guid LastClinicId { get; private set; }
        public Guid LastEncounterId { get; private set; }
        public PrescriptionDraftCreationResult Result { get; set; } = new(true);

        public Task<PrescriptionDraftCreationResult> CreateAsync(
            Guid prescriptionId,
            Guid patientId,
            string? patientNationalId,
            Guid doctorId,
            Guid clinicId,
            Guid encounterId,
            string providerKey,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref CreateCalls);
            LastPatientId = patientId;
            LastDoctorId = doctorId;
            LastClinicId = clinicId;
            LastEncounterId = encounterId;
            return Task.FromResult(Result.Succeeded
                ? Result with { PrescriptionId = prescriptionId }
                : Result);
        }
    }

    private sealed class InMemoryReceiptStore<T> : IOperationReceiptStore<T>
    {
        private readonly object _gate = new();
        private readonly Dictionary<OperationReceiptKey, OperationReceipt<T>> _receipts = [];

        public Task<OperationReceipt<T>?> GetAsync(
            OperationReceiptKey key,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult(_receipts.GetValueOrDefault(key));
            }
        }

        public Task<bool> TryClaimAsync(
            OperationReceiptKey key,
            T value,
            TimeSpan lifetime,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (_receipts.ContainsKey(key))
                {
                    return Task.FromResult(false);
                }

                _receipts[key] = new(0, value);
                return Task.FromResult(true);
            }
        }

        public Task<bool> TryUpdateAsync(
            OperationReceiptKey key,
            long expectedRevision,
            T value,
            TimeSpan lifetime,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (!_receipts.TryGetValue(key, out OperationReceipt<T>? current) ||
                    current.Revision != expectedRevision)
                {
                    return Task.FromResult(false);
                }

                _receipts[key] = new(expectedRevision + 1, value);
                return Task.FromResult(true);
            }
        }
    }
}
