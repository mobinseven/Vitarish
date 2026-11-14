using System.Text.Json;
using FluentAssertions;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Presentation;
using Vitarish.Clinic.Application.Assistant.Staff;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.Assistant;

/// <summary>
/// Each case feeds a projector the output its tool serializes (names follow the tool's own serializer: camelCase
/// for the typed read tools, default PascalCase for the scheduling tools) and checks what a surface may show.
/// </summary>
public sealed class ClinicAssistantPresentationsTests
{
    private const string Guid1 = "11111111-1111-1111-1111-111111111111";
    private const string Guid2 = "22222222-2222-2222-2222-222222222222";
    private const string Summary = """{"id":"@1","status":"Submitted","selectedProviderKey":"p","itemCount":4,"createdAt":"2030-01-01T00:00:00","submittedAt":"2030-01-01T01:00:00","trackingCode":"TRK-SECRET","itemDisplayNames":["A","B","C","D"],"sourceUpdatedAt":"2030-01-01T01:00:00"}""";

    [Fact]
    public void Patient_search_projects_the_token_and_masked_values_from_either_naming()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.SearchPatients,
            """{"Candidates":[{"CandidateToken":"cand_1","DisplayName":"Sara Ahmadi","DateOfBirth":"1990-04-02","MaskedIdentification":"1******9","MaskedPhone":"0******1"}],"HasMore":true}""");

        result.Kind.Should().Be(ClinicPresentedRecordKinds.PatientCandidate);
        result.IsList.Should().BeTrue();
        result.HasMore.Should().BeTrue();
        PresentableRecord record = result.Records.Single();
        record.Reference.Should().Be("cand_1");
        Title(record).Should().Be("Sara Ahmadi");
        result.FieldNames.Should().BeEquivalentTo("dateOfBirth", "maskedPhone", "maskedIdentification");
    }

    [Fact]
    public void Operations_appointment_list_projects_status_and_session_length_without_patient_identity()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.ListOperationsAppointments,
            """{"Status":"Succeeded","Value":[{"Id":"@1","Status":"Confirmed","ShiftId":"@2","Date":"2030-01-02","EstimatedSessionLength":"00:30:00","ConfirmedAt":"2030-01-01T08:00:00","CancelledAt":null,"IsHold":false,"ExpiresAt":null}],"OperationId":null,"Message":null}""");

        result.Kind.Should().Be(ClinicPresentedRecordKinds.Appointment);
        result.IsList.Should().BeTrue();
        PresentableRecord record = result.Records.Single();
        record.Reference.Should().Be(Guid1);
        Title(record).Should().Be("2030-01-02");
        Field(record, "status").Should().Be("Confirmed");
        Number(record, "estimatedSessionMinutes").Should().Be(30);
        Leaks(result, Guid2).Should().BeFalse("the shift id is internal");
    }

    [Fact]
    public void Operations_appointment_list_without_success_is_not_presented()
    {
        ClinicAssistantPresentations.All().Single(Is(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.ListOperationsAppointments))
            .Project(Parse("""{"Status":"NotFound","Value":null,"Message":"none"}""")).Should().BeNull();
    }

    [Fact]
    public void One_operations_appointment_is_a_single_record()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.GetOperationsAppointment,
            """{"Status":"Succeeded","Value":{"Id":"@1","Status":"Present","ShiftId":"@2","Date":"2030-01-02","EstimatedSessionLength":null,"ConfirmedAt":null,"CancelledAt":null,"IsHold":false,"ExpiresAt":null}}""");

        result.IsList.Should().BeFalse();
        result.Records.Single().Reference.Should().Be(Guid1);
        Field(result.Records.Single(), "status").Should().Be("Present");
    }

    [Fact]
    public void Operations_count_is_one_number_record_with_the_unconverted_numeric_status()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.CountOperationsAppointments,
            """{"Status":0,"Value":7,"OperationId":null,"Message":null}""");

        result.Kind.Should().Be(ClinicPresentedRecordKinds.AppointmentCount);
        result.IsList.Should().BeFalse();
        result.Records.Single().Fields["count"].Value.GetDouble().Should().Be(7);
        result.Records.Single().Title.Type.Should().Be(PresentableFieldType.Number);
    }

    [Fact]
    public void Operations_shifts_name_numeric_enums_and_use_the_time_range_as_title()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.ListOperationsShifts,
            """{"Status":0,"Value":[{"Status":1,"Visibility":0,"DayOfWeek":1,"StartTime":"08:00:00","EndTime":"12:00:00","TimeZone":"UTC","TotalSeats":4,"BookingOpensBefore":null,"BookingCloseBefore":null,"ServiceProviderId":"@2","ServiceId":null,"id":"@1","Metadata":{"x":1},"createdAt":"2030-01-01T00:00:00"}]}""");

        result.Kind.Should().Be(ClinicPresentedRecordKinds.Shift);
        PresentableRecord record = result.Records.Single();
        record.Reference.Should().Be(Guid1);
        Title(record).Should().Be("08:00-12:00");
        Field(record, "dayOfWeek").Should().Be("Monday");
        Field(record, "status").Should().Be("Active");
        Number(record, "totalSeats").Should().Be(4);
        result.FieldNames.Should().BeEquivalentTo("dayOfWeek", "status", "totalSeats");
        Leaks(result, Guid2).Should().BeFalse("the provider id is internal");
    }

    [Fact]
    public void One_operations_shift_is_a_single_record()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.GetOperationsShift,
            """{"Status":0,"Value":{"Status":2,"DayOfWeek":6,"StartTime":"09:00:00","EndTime":"10:30:00","TotalSeats":1,"id":"@1"}}""");

        result.IsList.Should().BeFalse();
        Field(result.Records.Single(), "status").Should().Be("Paused");
        Field(result.Records.Single(), "dayOfWeek").Should().Be("Saturday");
    }

    [Fact]
    public void Shift_occurrences_project_seats_and_open_state()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.GetOperationsShiftOccurrences,
            """{"Status":0,"Value":[{"ShiftId":"@1","Date":"2030-01-07","StartTime":"08:00:00","EndTime":"12:00:00","TotalSeats":4,"ReservationCount":1,"AvailableSeats":3,"EstimatedSessionLength":"00:20:00","IsClosed":false}]}""");

        result.Kind.Should().Be(ClinicPresentedRecordKinds.ShiftOccurrence);
        PresentableRecord record = result.Records.Single();
        record.Reference.Should().Be(Guid1);
        Title(record).Should().Be("2030-01-07");
        Number(record, "availableSeats").Should().Be(3);
        Field(record, "state").Should().Be("open");
    }

    [Fact]
    public void Availability_projects_the_occurrence_of_each_slot_and_skips_slots_without_one()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.GetOperationsAvailability,
            """{"Status":0,"Value":[{"Shift":{"id":"@1","Status":1},"Occurrence":{"ShiftId":"@1","Date":"2030-01-07","StartTime":"08:00:00","EndTime":"12:00:00","TotalSeats":4,"ReservationCount":4,"AvailableSeats":0,"IsClosed":true}},{"Shift":{"id":"@2"},"Occurrence":null}]}""");

        result.Records.Should().ContainSingle();
        Field(result.Records.Single(), "state").Should().Be("closed");
    }

    [Fact]
    public void Operations_surgeries_name_the_patient_but_no_internal_ids()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.ListOperationsSurgeries,
            """{"surgeries":[{"id":"@1","patientDisplayName":"Sara Ahmadi","catalogId":"@2","name":"Knee arthroscopy","status":"Planned","scheduledAt":"2030-01-12T09:00:00+03:30","durationMinutes":90,"location":"OR 2","team":[{"userId":"u1","displayName":"Dr Rad","role":"Surgeon"}],"tools":["drill"],"reminderPolicies":[]}]}""");

        result.Kind.Should().Be(ClinicPresentedRecordKinds.Surgery);
        PresentableRecord record = result.Records.Single();
        record.Reference.Should().Be(Guid1);
        Title(record).Should().Be("Knee arthroscopy");
        Field(record, "patient").Should().Be("Sara Ahmadi");
        Number(record, "durationMinutes").Should().Be(90);
        result.FieldNames.Should().BeEquivalentTo("status", "scheduledAt", "durationMinutes", "location", "patient");
        Leaks(result, Guid2, "Dr Rad").Should().BeFalse();
    }

    [Fact]
    public void One_operations_surgery_is_a_single_record()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.GetOperationsSurgery,
            """{"id":"@1","patientDisplayName":null,"catalogId":"c","name":"Cataract","status":"Completed","scheduledAt":"2030-01-12T09:00:00+00:00","durationMinutes":null,"location":null,"team":[],"tools":[],"reminderPolicies":[]}""");

        result.IsList.Should().BeFalse();
        result.Records.Single().Fields.Keys.Should().BeEquivalentTo("status", "scheduledAt");
    }

    [Fact]
    public void Surgery_conflicts_name_the_reasons_as_flags_for_both_assistants()
    {
        string output = """{"conflicts":[{"surgeryId":"@1","name":"Hip","patientDisplayName":null,"scheduledAt":"2030-01-12T09:00:00+00:00","durationMinutes":60,"location":"OR 1","reasons":["SameLocation"],"sharedTeamUserIds":["u1"]}]}""";

        foreach ((string agent, string tool) in new[]
        {
            (AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.FindSurgeryConflicts),
            (AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.FindOperationsSurgeryConflicts)
        })
        {
            PresentableResult result = Project(agent, tool, output);
            result.Kind.Should().Be(ClinicPresentedRecordKinds.SurgeryConflict);
            result.Records.Single().Reference.Should().Be(Guid1);
            result.Records.Single().Fields.Should().ContainKey("sameLocation").And.NotContainKey("sharedTeamMember");
            Leaks(result, "u1").Should().BeFalse("shared team user ids are internal");
        }
    }

    [Fact]
    public void Setup_goals_mark_done_next_and_open()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, ClinicPresentableToolNames.GetClinicSetupProgress,
            """{"goals":[{"goal":"ClinicProfile","done":true},{"goal":"StaffUsers","done":false},{"goal":"ShiftsAndBookingRules","done":false}],"nextGoal":"StaffUsers","workingHours":[{"day":"Monday","intervals":[{"opens":"08:00","closes":"16:00"}]}]}""");

        result.Records.Select(record => Field(record, "state")).Should().Equal("done", "next", "open");
        result.Records.Select(record => record.Reference).Should().Equal("ClinicProfile", "StaffUsers", "ShiftsAndBookingRules");
        result.FieldNames.Should().BeEquivalentTo("state");
    }

    [Fact]
    public void Staff_members_carry_only_the_masked_contact()
    {
        PresentableResult result = Project(AssistantSubjects.OperationsAgentKey, AssistantStaffManagement.ListToolName,
            """{"members":[{"userId":"user-9","name":"Mina Rad","role":"staff","contact":"09******11"}]}""");

        result.Kind.Should().Be(ClinicPresentedRecordKinds.StaffMember);
        PresentableRecord record = result.Records.Single();
        record.Reference.Should().Be("user-9");
        Title(record).Should().Be("Mina Rad");
        Field(record, "maskedContact").Should().Be("09******11");
        result.FieldNames.Should().BeEquivalentTo("role", "maskedContact");
    }

    [Fact]
    public void The_next_patient_appointment_is_a_single_record_and_none_found_is_not_presented()
    {
        PresentableResult result = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.GetNextPatientAppointment,
            """{"found":true,"appointment":{"ReservationId":"@1","Date":"2030-01-02","StartTime":"09:30:00","EndTime":"10:00:00","TimeZone":"UTC","Status":"Confirmed","ShiftId":"@2","ScheduleAvailable":true,"SourceUpdatedAt":"2030-01-01T00:00:00"},"HasMore":false}""");

        result.IsList.Should().BeFalse();
        PresentableRecord record = result.Records.Single();
        record.Reference.Should().Be(Guid1);
        Field(record, "startTime").Should().Be("09:30");
        Field(record, "endTime").Should().Be("10:00");
        Leaks(result, Guid2).Should().BeFalse();
        ClinicAssistantPresentations.All().Single(Is(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.GetNextPatientAppointment))
            .Project(Parse("""{"found":false,"appointment":null,"HasMore":false}""")).Should().BeNull();
    }

    [Fact]
    public void The_patient_appointment_list_keeps_has_more()
    {
        PresentableResult result = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.ListPatientAppointments,
            """{"period":"past","appointments":[{"ReservationId":"@1","Date":"2029-12-01","StartTime":null,"EndTime":null,"TimeZone":null,"Status":"Completed","ShiftId":"@2","ScheduleAvailable":false,"SourceUpdatedAt":"2029-12-01T00:00:00"}],"HasMore":true}""");

        result.IsList.Should().BeTrue();
        result.HasMore.Should().BeTrue();
        result.Records.Single().Fields.Keys.Should().BeEquivalentTo("status");
    }

    [Fact]
    public void The_patient_appointment_count_carries_the_period()
    {
        PresentableResult result = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.CountPatientAppointments,
            """{"period":"future","count":3}""");

        result.Kind.Should().Be(ClinicPresentedRecordKinds.AppointmentCount);
        Field(result.Records.Single(), "period").Should().Be("future");
        result.Records.Single().Fields["count"].Value.GetDouble().Should().Be(3);
    }

    [Fact]
    public void A_checked_slot_projects_service_and_practitioner_names_only()
    {
        PresentableResult result = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.CheckAppointmentAvailability,
            """{"ShiftId":"@1","Date":"2030-01-02","StartTime":"09:30:00","Duration":"00:30:00","ServiceId":"@2","Service":"Consultation","PractitionerId":null,"Practitioner":"Dr Rad","TimeZone":"UTC"}""");

        result.Kind.Should().Be(ClinicPresentedRecordKinds.AppointmentSlot);
        PresentableRecord record = result.Records.Single();
        record.Reference.Should().Be(Guid1);
        Number(record, "durationMinutes").Should().Be(30);
        Field(record, "service").Should().Be("Consultation");
        Leaks(result, Guid2).Should().BeFalse();
        ClinicAssistantPresentations.All().Single(Is(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.CheckAppointmentAvailability))
            .Project(Parse("null")).Should().BeNull();
    }

    [Fact]
    public void Reminders_project_delivery_without_template_details()
    {
        PresentableResult result = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.ListPatientReminderStatus,
            """{"found":true,"reminders":[{"id":"@1","kind":"Sms","sourceKind":"appointment","templatePurpose":"AppointmentReminder","remindAt":"2030-01-01T08:00:00","sentAt":"2030-01-01T08:00:05","isSent":true,"providerSubmittedAt":"2030-01-01T08:00:04","sourceUpdatedAt":"2030-01-01T08:00:05"}],"hasMore":false}""");

        PresentableRecord record = result.Records.Single();
        record.Title.Type.Should().Be(PresentableFieldType.DateTime);
        Field(record, "delivery").Should().Be("sent");
        result.FieldNames.Should().BeEquivalentTo("kind", "delivery", "sentAt");
        Leaks(result, "AppointmentReminder").Should().BeFalse();
    }

    [Fact]
    public void The_patient_overview_shows_recorded_flags_but_no_clinical_text()
    {
        PresentableResult result = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.GetPatientFactualOverview,
            """{"found":true,"sourceUpdatedAt":"2030-01-01T00:00:00+00:00","overview":{"name":"Sara Ahmadi","dateOfBirth":"1990-04-02T00:00:00","visitReason":"chest pain since monday","hasRecordedAllergies":true,"hasRecordedMedications":false}}""");

        result.Kind.Should().Be(ClinicPresentedRecordKinds.PatientProfile);
        PresentableRecord record = result.Records.Single();
        record.Reference.Should().Be(ClinicPresentedReferences.CurrentPatient);
        Field(record, "allergies").Should().Be("recorded");
        Field(record, "medications").Should().Be("notRecorded");
        Leaks(result, "chest pain").Should().BeFalse("visit reason is clinical free text");
    }

    [Fact]
    public void The_patient_profile_shows_no_contact_details()
    {
        PresentableResult result = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.GetPatientProfile,
            """{"found":true,"sourceUpdatedAt":"2030-01-01T00:00:00+00:00","profile":{"name":"Sara Ahmadi","firstName":"Sara","lastName":"Ahmadi","dateOfBirth":"1990-04-02T00:00:00","gender":"Female","phone":"09121234567","email":"sara@example.com","address":"12 Valiasr St","insuranceProvider":"Salamat"}}""");

        PresentableRecord record = result.Records.Single();
        Title(record).Should().Be("Sara Ahmadi");
        Field(record, "gender").Should().Be("Female");
        Field(record, "insuranceProvider").Should().Be("Salamat");
        Leaks(result, "09121234567", "sara@example.com", "Valiasr").Should().BeFalse();
    }

    [Fact]
    public void Prescription_history_projects_status_and_the_first_item_names_without_the_tracking_code()
    {
        PresentableResult history = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.ListPatientPrescriptionHistory,
            """{"prescriptions":[@S],"hasMore":true}""");
        PresentableResult attention = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.GetPatientPrescriptionAttention,
            """{"totalCount":9,"attentionCount":1,"attention":[@S],"hasMore":false}""");

        history.HasMore.Should().BeTrue();
        PresentableRecord record = history.Records.Single();
        record.Reference.Should().Be(Guid1);
        Field(record, "items").Should().Be("A, B, C");
        Number(record, "itemCount").Should().Be(4);
        Leaks(history, "TRK-SECRET").Should().BeFalse();
        attention.Kind.Should().Be(ClinicPresentedRecordKinds.Prescription);
        attention.Records.Single().Reference.Should().Be(Guid1);
    }

    [Fact]
    public void One_prescription_counts_its_items()
    {
        PresentableResult result = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.GetPatientPrescription,
            """{"found":true,"prescription":{"id":"@1","status":"Draft","selectedProviderKey":null,"versionNumber":2,"createdAt":"2030-01-01T00:00:00","submittedAt":null,"trackingCode":"TRK-SECRET","items":[{"category":0,"referenceCode":"R1","displayName":"Amoxicillin","quantity":1,"durationDays":7,"sortOrder":0}],"hasMoreItems":false,"sourceUpdatedAt":"2030-01-01T00:00:00"}}""");

        result.IsList.Should().BeFalse();
        Number(result.Records.Single(), "itemCount").Should().Be(1);
        Field(result.Records.Single(), "items").Should().Be("Amoxicillin");
        Leaks(result, "TRK-SECRET", "R1").Should().BeFalse();
    }

    [Fact]
    public void Procedure_progress_projects_state_and_counts_but_not_the_description()
    {
        PresentableResult result = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.ListPatientProcedureProgress,
            """{"procedures":[{"id":"@1","name":"Root canal","description":"private plan text","stats":{"state":"Half","procedures":1,"events":3,"committedEvents":2,"eventItems":5,"newItems":1,"unread":true,"lastUpdate":"2030-01-01T00:00:00"},"sourceUpdatedAt":"2030-01-01T00:00:00"}],"hasMore":false}""");

        PresentableRecord record = result.Records.Single();
        Title(record).Should().Be("Root canal");
        Field(record, "progress").Should().Be("Half");
        Number(record, "committedEvents").Should().Be(2);
        Leaks(result, "private plan text").Should().BeFalse();
    }

    [Fact]
    public void Procedure_events_and_one_event_show_title_and_commit_state_but_not_the_description()
    {
        PresentableResult list = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.ListPatientProcedureEvents,
            """{"found":true,"events":[{"id":"@1","title":"Visit 1","isCommitted":true,"committedAt":"2030-01-01T00:00:00","sourceUpdatedAt":"2030-01-01T00:00:00"}],"hasMore":false}""");
        PresentableResult detail = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.GetPatientProcedureEvent,
            """{"found":true,"event":{"id":"@1","title":"Visit 1","description":"diagnosis text","isCommitted":false,"committedAt":null,"sourceUpdatedAt":"2030-01-01T00:00:00"}}""");

        list.Records.Single().Reference.Should().Be(Guid1);
        Field(list.Records.Single(), "state").Should().Be("committed");
        detail.IsList.Should().BeFalse();
        Field(detail.Records.Single(), "state").Should().Be("draft");
        Leaks(detail, "diagnosis text").Should().BeFalse();
    }

    [Fact]
    public void Attachment_metadata_projects_name_kind_and_size()
    {
        PresentableResult result = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.ListPatientEventAttachmentMetadata,
            """{"found":true,"attachments":[{"id":"@1","fileName":"xray.png","contentType":"image/png","size":2048,"kind":"Image","sourceUpdatedAt":"2030-01-01T00:00:00"}],"hasMore":false}""");

        PresentableRecord record = result.Records.Single();
        Title(record).Should().Be("xray.png");
        Number(record, "sizeBytes").Should().Be(2048);
        Field(record, "kind").Should().Be("Image");
    }

    [Fact]
    public void Patient_surgeries_project_the_plan_without_contacts_or_patient_id()
    {
        PresentableResult result = Project(AssistantSubjects.PatientAgentKey, ClinicPresentableToolNames.ListPatientSurgeries,
            """{"surgeries":[{"id":"@1","patientId":"@2","catalogId":"c","name":"Knee arthroscopy","idempotencyKey":null,"status":"Planned","scheduledAt":"2030-01-12T09:00:00+03:30","tools":[],"personsToNotify":[],"contacts":[{"name":"Sara","destination":"09121234567","channel":"sms"}],"reminderPolicies":[],"concurrencyToken":"@2","createdAt":"2030-01-01T00:00:00","updatedAt":null,"durationMinutes":90,"location":"OR 2","team":[]}]}""");

        result.Records.Single().Reference.Should().Be(Guid1);
        Field(result.Records.Single(), "location").Should().Be("OR 2");
        Leaks(result, Guid2, "09121234567").Should().BeFalse();
    }

    [Fact]
    public void Every_registration_names_an_agent_and_a_distinct_tool_per_agent()
    {
        ClinicAssistantPresentations.All().SelectMany(presentation => presentation.AgentKeys.Select(key => (key, presentation.ToolName)))
            .Should().OnlyHaveUniqueItems();
        FluentActions.Invoking(() => new AgentToolPresentationCatalog(ClinicAssistantPresentations.All())).Should().NotThrow();
    }

    private static Func<AgentToolPresentation, bool> Is(string agentKey, string toolName) =>
        presentation => presentation.ToolName == toolName && presentation.AgentKeys.Contains(agentKey);

    private static JsonElement Parse(string json) => JsonDocument.Parse(json.Replace("@1", Guid1).Replace("@2", Guid2)).RootElement.Clone();

    private static PresentableResult Project(string agentKey, string toolName, string json) =>
        ClinicAssistantPresentations.All().Single(Is(agentKey, toolName)).Project(Parse(json.Replace("@S", Summary)))
            ?? throw new InvalidOperationException($"{toolName} projected nothing.");

    private static string? Title(PresentableRecord record) => record.Title.Value.GetString();

    private static string? Field(PresentableRecord record, string name) => record.Fields[name].Value.GetString();

    private static double Number(PresentableRecord record, string name) => record.Fields[name].Value.GetDouble();

    /// <summary>Whether any of the values appears anywhere in the projected result, which is all a surface can show.</summary>
    private static bool Leaks(PresentableResult result, params string[] values)
    {
        string json = JsonSerializer.Serialize(result, PresentableJsonContext.Default.PresentableResult);
        return values.Any(value => json.Contains(value, StringComparison.Ordinal));
    }
}
