using System.Globalization;
using System.Text.Json;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Domain.Enums;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Staff;
using Vitarish.Intelligence.Application.GuidedSurfaces;

namespace Vitarish.Clinic.Application.Assistant.Presentation;

/// <summary>The record kinds Clinic assistant surfaces show; the web keys field labels and screens by them.</summary>
public static class ClinicPresentedRecordKinds
{
    public const string PatientCandidate = "clinic.patient-candidate";
    public const string PatientProfile = "clinic.patient-profile";
    public const string Appointment = "clinic.appointment";
    public const string AppointmentSlot = "clinic.appointment-slot";
    public const string AppointmentCount = "clinic.appointment-count";
    public const string Shift = "clinic.shift";
    public const string ShiftOccurrence = "clinic.shift-occurrence";
    public const string Reminder = "clinic.reminder";
    public const string Prescription = "clinic.prescription";
    public const string Procedure = "clinic.procedure";
    public const string ProcedureEvent = "clinic.procedure-event";
    public const string EventAttachment = "clinic.event-attachment";
    public const string Surgery = "clinic.surgery";
    public const string SurgeryConflict = "clinic.surgery-conflict";
    public const string StaffMember = "clinic.staff-member";
    public const string SetupGoal = "clinic.setup-goal";
}

/// <summary>Reference values that are not an entity ID.</summary>
public static class ClinicPresentedReferences
{
    /// <summary>The patient the patient assistant is bound to; the web already holds that patient.</summary>
    public const string CurrentPatient = "current";

    public const string Count = "count";
}

/// <summary>Values of presented status fields that the read tools do not themselves name.</summary>
public static class ClinicPresentedStatuses
{
    public const string Recorded = "recorded";
    public const string NotRecorded = "notRecorded";
    public const string Committed = "committed";
    public const string Draft = "draft";
    public const string Sent = "sent";
    public const string Pending = "pending";
    public const string Closed = "closed";
    public const string Open = "open";
    public const string Done = "done";
    public const string Next = "next";
    public const string Yes = "yes";
}

/// <summary>Names of the Clinic read tools whose results can be presented.</summary>
public static class ClinicPresentableToolNames
{
    public const string SearchPatients = "search_patients";
    public const string ListOperationsAppointments = "list_operations_appointments";
    public const string GetOperationsAppointment = "get_operations_appointment";
    public const string CountOperationsAppointments = "count_operations_appointments";
    public const string ListOperationsShifts = "list_operations_shifts";
    public const string GetOperationsShift = "get_operations_shift";
    public const string GetOperationsShiftOccurrences = "get_operations_shift_occurrences";
    public const string GetOperationsAvailability = "get_operations_availability";
    public const string GetNextPatientAppointment = "get_next_patient_appointment";
    public const string ListPatientAppointments = "list_patient_appointments";
    public const string CountPatientAppointments = "count_patient_appointments";
    public const string CheckAppointmentAvailability = "check_appointment_availability";
    public const string ListPatientReminderStatus = "list_patient_reminder_status";
    public const string GetPatientFactualOverview = "get_patient_factual_overview";
    public const string GetPatientProfile = "get_patient_profile";
    public const string ListPatientPrescriptionHistory = "list_patient_prescription_history";
    public const string GetPatientPrescription = "get_patient_prescription";
    public const string GetPatientPrescriptionAttention = "get_patient_prescription_attention";
    public const string ListPatientProcedureProgress = "list_patient_procedure_progress";
    public const string ListPatientProcedureSteps = "list_patient_procedure_steps";
    public const string GetPatientProcedureStep = "get_patient_procedure_step";
    public const string ListPatientStepFiles = "list_patient_step_files";
    public const string ListOperationsPatientProcedures = "list_operations_patient_procedures";
    public const string ListOperationsProcedureSteps = "list_operations_procedure_steps";
    public const string GetOperationsProcedureStep = "get_operations_procedure_step";
    public const string ListOperationsStepFiles = "list_operations_step_files";
    public const string ListPatientSurgeries = "list_patient_surgeries";
    public const string FindSurgeryConflicts = "find_surgery_conflicts";
    public const string ListOperationsSurgeries = "list_operations_surgeries";
    public const string GetOperationsSurgery = "get_operations_surgery";
    public const string FindOperationsSurgeryConflicts = "find_operations_surgery_conflicts";
    public const string GetClinicSetupProgress = "get_clinic_setup_progress";
}

/// <summary>
/// Which Clinic assistant read results a presented surface may show, and which of their fields (ADR-0022
/// decision 8). Only values the owning screen shows are projected, masked as that screen masks them; national IDs,
/// full phone numbers, clinical free text and internal tokens are never projected except as the record reference.
/// </summary>
public static class ClinicAssistantPresentations
{
    /// <summary>
    /// Read tools whose results are not presented, with why. Every read tool of a Clinic assistant is either
    /// projected by <see cref="All"/> or named here; a coverage test enforces it.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Exempt = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["resolve_operations_services"] = "Lookup that only resolves service IDs for another tool.",
        ["resolve_operations_service_types"] = "Lookup that only resolves service type IDs for another tool.",
        ["resolve_operations_providers"] = "Lookup that only resolves provider IDs for another tool.",
        ["list_surgery_types"] = "Lookup of surgery type IDs for planning a surgery.",
        ["list_operations_surgery_types"] = "Lookup of surgery type IDs for planning a surgery.",
        ["list_surgery_staff"] = "Lookup of team member user IDs for planning a surgery.",
        ["list_operations_surgery_staff"] = "Lookup of team member user IDs for planning a surgery.",
        ["get_surgery_suggestions"] = "Free-text location and role suggestions for planning a surgery.",
        ["get_operations_surgery_suggestions"] = "Free-text location and role suggestions for planning a surgery.",
        ["get_surgery_cancellation_message"] = "Message template text, not a record a screen lists.",
        ["get_prescription_template_requirements"] = "Template requirements that only guide preparing a prescription.",
        ["search_prescription_templates"] = "Lookup that only resolves a template for preparing a prescription.",
        ["get_patient_recorded_clinical_facts"] = "Clinical free text is never projected.",
        [ClinicalEventToolNames.ProcedureContext] = "Internal state fingerprint that only guides preparing a clinical event.",
        ["search"] = "Cross-module search owned by the Search module; results open through its own surface.",
        ["contents_list_sites"] = "Contents catalog read owned by the Contents module.",
        ["contents_list_types"] = "Contents catalog read owned by the Contents module.",
        ["contents_list_entries"] = "Contents catalog read owned by the Contents module.",
        ["contents_get_entry"] = "Contents entry body is free text owned by the Contents module."
    };

    public static IEnumerable<AgentToolPresentation> All()
    {
        IReadOnlyCollection<string> operations = AssistantSubjects.OperationsAgentKeys;
        IReadOnlyCollection<string> patient = AssistantSubjects.PatientAgentKeys;

        yield return new(operations, ClinicPresentableToolNames.SearchPatients, PatientCandidates);
        yield return new(operations, ClinicPresentableToolNames.ListOperationsAppointments, OperationsAppointmentList);
        yield return new(operations, ClinicPresentableToolNames.GetOperationsAppointment, OperationsAppointment);
        yield return new(operations, ClinicPresentableToolNames.CountOperationsAppointments, OperationsAppointmentCount);
        yield return new(operations, ClinicPresentableToolNames.ListOperationsShifts, ShiftList);
        yield return new(operations, ClinicPresentableToolNames.GetOperationsShift, Shift);
        yield return new(operations, ClinicPresentableToolNames.GetOperationsShiftOccurrences, ShiftOccurrences);
        yield return new(operations, ClinicPresentableToolNames.GetOperationsAvailability, AvailableSlots);
        yield return new(operations, ClinicPresentableToolNames.ListOperationsSurgeries, OperationsSurgeryList);
        yield return new(operations, ClinicPresentableToolNames.GetOperationsSurgery, OperationsSurgery);
        yield return new(operations, ClinicPresentableToolNames.FindOperationsSurgeryConflicts, SurgeryConflicts);
        yield return new(operations, ClinicPresentableToolNames.GetClinicSetupProgress, SetupGoals);
        yield return new(operations, AssistantStaffManagement.ListToolName, StaffMembers);
        yield return new(operations, ClinicPresentableToolNames.ListOperationsPatientProcedures, Procedures);
        yield return new(operations, ClinicPresentableToolNames.ListOperationsProcedureSteps, ProcedureSteps);
        yield return new(operations, ClinicPresentableToolNames.GetOperationsProcedureStep, ProcedureStepDetail);
        yield return new(operations, ClinicPresentableToolNames.ListOperationsStepFiles, StepFiles);

        yield return new(patient, ClinicPresentableToolNames.GetNextPatientAppointment, NextPatientAppointment);
        yield return new(patient, ClinicPresentableToolNames.ListPatientAppointments, PatientAppointmentList);
        yield return new(patient, ClinicPresentableToolNames.CountPatientAppointments, PatientAppointmentCount);
        yield return new(patient, ClinicPresentableToolNames.CheckAppointmentAvailability, CheckedSlot);
        yield return new(patient, ClinicPresentableToolNames.ListPatientReminderStatus, Reminders);
        yield return new(patient, ClinicPresentableToolNames.GetPatientFactualOverview, PatientOverview);
        yield return new(patient, ClinicPresentableToolNames.GetPatientProfile, PatientProfile);
        yield return new(patient, ClinicPresentableToolNames.ListPatientPrescriptionHistory, PrescriptionHistory);
        yield return new(patient, ClinicPresentableToolNames.GetPatientPrescription, PrescriptionDetail);
        yield return new(patient, ClinicPresentableToolNames.GetPatientPrescriptionAttention, PrescriptionAttention);
        yield return new(patient, ClinicPresentableToolNames.ListPatientProcedureProgress, Procedures);
        yield return new(patient, ClinicPresentableToolNames.ListPatientProcedureSteps, ProcedureSteps);
        yield return new(patient, ClinicPresentableToolNames.GetPatientProcedureStep, ProcedureStepDetail);
        yield return new(patient, ClinicPresentableToolNames.ListPatientStepFiles, StepFiles);
        yield return new(patient, ClinicPresentableToolNames.ListPatientSurgeries, PatientSurgeryList);
        yield return new(patient, ClinicPresentableToolNames.FindSurgeryConflicts, SurgeryConflicts);
    }

    /// <summary>Patient search: the reference is the run-scoped candidate token, never a patient ID.</summary>
    internal static PresentableResult? PatientCandidates(JsonElement output)
    {
        if (!TryArray(output, "candidates", out JsonElement candidates)) return null;
        return List(ClinicPresentedRecordKinds.PatientCandidate, candidates, Bool(output, "hasMore"), candidate =>
        {
            string? token = Text(candidate, "candidateToken");
            return token is null ? null : new(
                ClinicPresentedRecordKinds.PatientCandidate,
                token,
                PresentableField.Text(Text(candidate, "displayName")),
                Fields(
                    ("dateOfBirth", DateField(candidate, "dateOfBirth")),
                    ("maskedPhone", TextField(candidate, "maskedPhone")),
                    ("maskedIdentification", TextField(candidate, "maskedIdentification"))));
        });
    }

    // Appointments. Operations results are wrapped ({status,value}); patient results are plain objects.

    internal static PresentableResult? OperationsAppointmentList(JsonElement output) =>
        TryArrayValue(output, out JsonElement values) ? List(ClinicPresentedRecordKinds.Appointment, values, false, AppointmentRecord) : null;

    internal static PresentableResult? OperationsAppointment(JsonElement output) =>
        TryObjectValue(output, out JsonElement value) ? Single(ClinicPresentedRecordKinds.Appointment, AppointmentRecord(value)) : null;

    internal static PresentableResult? OperationsAppointmentCount(JsonElement output) =>
        TryValue(output, out JsonElement value) && value.ValueKind == JsonValueKind.Number
            ? Count(value.GetDouble(), null)
            : null;

    internal static PresentableResult? NextPatientAppointment(JsonElement output) =>
        TryProperty(output, "appointment", out JsonElement appointment) && appointment.ValueKind == JsonValueKind.Object
            ? Single(ClinicPresentedRecordKinds.Appointment, AppointmentRecord(appointment))
            : null;

    internal static PresentableResult? PatientAppointmentList(JsonElement output) =>
        TryArray(output, "appointments", out JsonElement values)
            ? List(ClinicPresentedRecordKinds.Appointment, values, Bool(output, "hasMore"), AppointmentRecord)
            : null;

    internal static PresentableResult? PatientAppointmentCount(JsonElement output) =>
        Number(output, "count") is { } count ? Count(count, Text(output, "period")) : null;

    private static PresentableRecord? AppointmentRecord(JsonElement appointment)
    {
        string? id = Text(appointment, "reservationId") ?? Text(appointment, "id");
        return id is null ? null : new(
            ClinicPresentedRecordKinds.Appointment,
            id,
            PresentableField.Date(Text(appointment, "date")),
            Fields(
                ("startTime", ClockField(appointment, "startTime")),
                ("endTime", ClockField(appointment, "endTime")),
                ("status", EnumField<ReservationStatus>(appointment, "status")),
                ("estimatedSessionMinutes", MinutesField(appointment, "estimatedSessionLength")),
                ("confirmedAt", DateTimeField(appointment, "confirmedAt"))));
    }

    private static PresentableResult Count(double count, string? period) => new(
        ClinicPresentedRecordKinds.AppointmentCount,
        IsList: false,
        [new(
            ClinicPresentedRecordKinds.AppointmentCount,
            ClinicPresentedReferences.Count,
            PresentableField.Number(count),
            Fields(("count", PresentableField.Number(count)), ("period", period is null ? null : PresentableField.Status(period))))]);

    /// <summary>One matching slot the patient assistant checked; null output means nothing matched.</summary>
    internal static PresentableResult? CheckedSlot(JsonElement slot)
    {
        string? shiftId = Text(slot, "shiftId");
        return shiftId is null ? null : Single(ClinicPresentedRecordKinds.AppointmentSlot, new(
            ClinicPresentedRecordKinds.AppointmentSlot,
            shiftId,
            PresentableField.Date(Text(slot, "date")),
            Fields(
                ("startTime", ClockField(slot, "startTime")),
                ("durationMinutes", MinutesField(slot, "duration")),
                ("service", TextField(slot, "service")),
                ("practitioner", TextField(slot, "practitioner")))));
    }

    // Shifts and availability.

    internal static PresentableResult? ShiftList(JsonElement output) =>
        TryArrayValue(output, out JsonElement values) ? List(ClinicPresentedRecordKinds.Shift, values, false, ShiftRecord) : null;

    internal static PresentableResult? Shift(JsonElement output) =>
        TryObjectValue(output, out JsonElement value) ? Single(ClinicPresentedRecordKinds.Shift, ShiftRecord(value)) : null;

    private static PresentableRecord? ShiftRecord(JsonElement shift)
    {
        string? id = Text(shift, "id");
        return id is null ? null : new(
            ClinicPresentedRecordKinds.Shift,
            id,
            PresentableField.Text(TimeRange(shift)),
            Fields(
                ("dayOfWeek", EnumField<DayOfWeek>(shift, "dayOfWeek")),
                ("status", EnumField<ShiftStatus>(shift, "status")),
                ("totalSeats", NumberField(shift, "totalSeats"))));
    }

    internal static PresentableResult? ShiftOccurrences(JsonElement output) =>
        TryArrayValue(output, out JsonElement values) ? List(ClinicPresentedRecordKinds.ShiftOccurrence, values, false, OccurrenceRecord) : null;

    /// <summary>Bookable slots: each pairs a shift with the dated occurrence the slot falls on.</summary>
    internal static PresentableResult? AvailableSlots(JsonElement output) =>
        TryArrayValue(output, out JsonElement values)
            ? List(ClinicPresentedRecordKinds.ShiftOccurrence, values, false, slot =>
                TryProperty(slot, "occurrence", out JsonElement occurrence) && occurrence.ValueKind == JsonValueKind.Object
                    ? OccurrenceRecord(occurrence)
                    : null)
            : null;

    private static PresentableRecord? OccurrenceRecord(JsonElement occurrence)
    {
        string? shiftId = Text(occurrence, "shiftId");
        return shiftId is null ? null : new(
            ClinicPresentedRecordKinds.ShiftOccurrence,
            shiftId,
            PresentableField.Date(Text(occurrence, "date")),
            Fields(
                ("startTime", ClockField(occurrence, "startTime")),
                ("endTime", ClockField(occurrence, "endTime")),
                ("availableSeats", NumberField(occurrence, "availableSeats")),
                ("totalSeats", NumberField(occurrence, "totalSeats")),
                ("state", PresentableField.Status(Bool(occurrence, "isClosed") ? ClinicPresentedStatuses.Closed : ClinicPresentedStatuses.Open))));
    }

    private static string? TimeRange(JsonElement shift) =>
        ClockTime(Text(shift, "startTime")) is { } start && ClockTime(Text(shift, "endTime")) is { } end ? $"{start}-{end}" : null;

    // Patient record.

    internal static PresentableResult? PatientOverview(JsonElement output) =>
        TryProperty(output, "overview", out JsonElement overview) && overview.ValueKind == JsonValueKind.Object
            ? Single(ClinicPresentedRecordKinds.PatientProfile, new(
                ClinicPresentedRecordKinds.PatientProfile,
                ClinicPresentedReferences.CurrentPatient,
                PresentableField.Text(Text(overview, "name")),
                Fields(
                    ("dateOfBirth", DateField(overview, "dateOfBirth")),
                    ("allergies", RecordedField(overview, "hasRecordedAllergies")),
                    ("medications", RecordedField(overview, "hasRecordedMedications")))))
            : null;

    /// <summary>The profile screen shows contact details, but a presented card carries none of them.</summary>
    internal static PresentableResult? PatientProfile(JsonElement output) =>
        TryProperty(output, "profile", out JsonElement profile) && profile.ValueKind == JsonValueKind.Object
            ? Single(ClinicPresentedRecordKinds.PatientProfile, new(
                ClinicPresentedRecordKinds.PatientProfile,
                ClinicPresentedReferences.CurrentPatient,
                PresentableField.Text(Text(profile, "name")),
                Fields(
                    ("dateOfBirth", DateField(profile, "dateOfBirth")),
                    ("gender", StatusField(profile, "gender")),
                    ("insuranceProvider", TextField(profile, "insuranceProvider")))))
            : null;

    private static PresentableField? RecordedField(JsonElement value, string name) =>
        TryProperty(value, name, out JsonElement flag) && flag.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? PresentableField.Status(flag.ValueKind == JsonValueKind.True ? ClinicPresentedStatuses.Recorded : ClinicPresentedStatuses.NotRecorded)
            : null;

    // Reminders.

    internal static PresentableResult? Reminders(JsonElement output) =>
        TryArray(output, "reminders", out JsonElement values)
            ? List(ClinicPresentedRecordKinds.Reminder, values, Bool(output, "hasMore"), reminder =>
            {
                string? id = Text(reminder, "id");
                return id is null ? null : new(
                    ClinicPresentedRecordKinds.Reminder,
                    id,
                    PresentableField.DateTime(Text(reminder, "remindAt")),
                    Fields(
                        ("kind", StatusField(reminder, "kind")),
                        ("delivery", PresentableField.Status(Bool(reminder, "isSent") ? ClinicPresentedStatuses.Sent : ClinicPresentedStatuses.Pending)),
                        ("sentAt", DateTimeField(reminder, "sentAt"))));
            })
            : null;

    // Prescriptions.

    internal static PresentableResult? PrescriptionHistory(JsonElement output) =>
        TryArray(output, "prescriptions", out JsonElement values)
            ? List(ClinicPresentedRecordKinds.Prescription, values, Bool(output, "hasMore"), PrescriptionSummary)
            : null;

    internal static PresentableResult? PrescriptionAttention(JsonElement output) =>
        TryArray(output, "attention", out JsonElement values)
            ? List(ClinicPresentedRecordKinds.Prescription, values, Bool(output, "hasMore"), PrescriptionSummary)
            : null;

    private static PresentableRecord? PrescriptionSummary(JsonElement prescription)
    {
        string? id = Text(prescription, "id");
        return id is null ? null : new(
            ClinicPresentedRecordKinds.Prescription,
            id,
            PresentableField.DateTime(Text(prescription, "createdAt")),
            Fields(
                ("status", StatusField(prescription, "status")),
                ("itemCount", NumberField(prescription, "itemCount")),
                ("items", JoinedNames(prescription, "itemDisplayNames")),
                ("submittedAt", DateTimeField(prescription, "submittedAt"))));
    }

    internal static PresentableResult? PrescriptionDetail(JsonElement output)
    {
        if (!TryProperty(output, "prescription", out JsonElement prescription) || prescription.ValueKind != JsonValueKind.Object) return null;
        string? id = Text(prescription, "id");
        if (id is null) return null;
        List<string> names = [];
        int count = 0;
        if (TryArray(prescription, "items", out JsonElement items))
            foreach (JsonElement item in items.EnumerateArray())
            {
                count++;
                if (Text(item, "displayName") is { Length: > 0 } name) names.Add(name);
            }
        return Single(ClinicPresentedRecordKinds.Prescription, new(
            ClinicPresentedRecordKinds.Prescription,
            id,
            PresentableField.DateTime(Text(prescription, "createdAt")),
            Fields(
                ("status", StatusField(prescription, "status")),
                ("itemCount", PresentableField.Number(count)),
                ("items", names.Count == 0 ? null : PresentableField.Text(string.Join(", ", names.Take(MaxJoinedNames)))),
                ("submittedAt", DateTimeField(prescription, "submittedAt")))));
    }

    private const int MaxJoinedNames = 3;

    private static PresentableField? JoinedNames(JsonElement value, string name)
    {
        if (!TryArray(value, name, out JsonElement array)) return null;
        string[] names = array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            .Select(item => item.GetString()!)
            .Take(MaxJoinedNames)
            .ToArray();
        return names.Length == 0 ? null : PresentableField.Text(string.Join(", ", names));
    }

    // Procedures, events, attachments. Free-text descriptions are not projected.

    internal static PresentableResult? Procedures(JsonElement output) =>
        TryArray(output, "procedures", out JsonElement values)
            ? List(ClinicPresentedRecordKinds.Procedure, values, Bool(output, "hasMore"), procedure =>
            {
                string? id = Text(procedure, "id");
                if (id is null) return null;
                TryProperty(procedure, "stats", out JsonElement stats);
                return new(
                    ClinicPresentedRecordKinds.Procedure,
                    id,
                    PresentableField.Text(Text(procedure, "name")),
                    Fields(
                        ("progress", StatusField(stats, "state")),
                        ("events", NumberField(stats, "events")),
                        ("committedEvents", NumberField(stats, "committedEvents")),
                        ("lastUpdate", DateTimeField(stats, "lastUpdate"))));
            })
            : null;

    /// <summary>Steps of a procedure; a step restricted by access rules carries no id and is not projected.</summary>
    internal static PresentableResult? ProcedureSteps(JsonElement output) =>
        TryArray(output, "steps", out JsonElement values)
            ? List(ClinicPresentedRecordKinds.ProcedureEvent, values, Bool(output, "hasMore"), ProcedureStepRecord)
            : null;

    /// <summary>One step: title and commit state only; its notes are clinical free text and are never projected.</summary>
    internal static PresentableResult? ProcedureStepDetail(JsonElement output) =>
        TryProperty(output, "step", out JsonElement value) && value.ValueKind == JsonValueKind.Object
            ? Single(ClinicPresentedRecordKinds.ProcedureEvent, ProcedureStepRecord(value))
            : null;

    private static PresentableRecord? ProcedureStepRecord(JsonElement item)
    {
        string? id = Text(item, "id");
        return id is null ? null : new(
            ClinicPresentedRecordKinds.ProcedureEvent,
            id,
            PresentableField.Text(Text(item, "title")),
            Fields(
                ("state", PresentableField.Status(Bool(item, "isCommitted") ? ClinicPresentedStatuses.Committed : ClinicPresentedStatuses.Draft)),
                ("committedAt", DateTimeField(item, "committedAt")),
                ("updatedAt", DateTimeField(item, "sourceUpdatedAt"))));
    }

    internal static PresentableResult? StepFiles(JsonElement output) =>
        TryArray(output, "files", out JsonElement values)
            ? List(ClinicPresentedRecordKinds.EventAttachment, values, Bool(output, "hasMore"), attachment =>
            {
                string? id = Text(attachment, "id");
                return id is null ? null : new(
                    ClinicPresentedRecordKinds.EventAttachment,
                    id,
                    PresentableField.Text(Text(attachment, "fileName")),
                    Fields(
                        ("contentType", TextField(attachment, "contentType")),
                        ("sizeBytes", NumberField(attachment, "size"))));
            })
            : null;

    // Surgeries.

    internal static PresentableResult? PatientSurgeryList(JsonElement output) =>
        TryArray(output, "surgeries", out JsonElement values) ? List(ClinicPresentedRecordKinds.Surgery, values, false, SurgeryRecord) : null;

    internal static PresentableResult? OperationsSurgeryList(JsonElement output) =>
        TryArray(output, "surgeries", out JsonElement values) ? List(ClinicPresentedRecordKinds.Surgery, values, false, SurgeryRecord) : null;

    internal static PresentableResult? OperationsSurgery(JsonElement output) =>
        Single(ClinicPresentedRecordKinds.Surgery, SurgeryRecord(output));

    private static PresentableRecord? SurgeryRecord(JsonElement surgery)
    {
        string? id = Text(surgery, "id");
        return id is null ? null : new(
            ClinicPresentedRecordKinds.Surgery,
            id,
            PresentableField.Text(Text(surgery, "name")),
            Fields(
                ("status", StatusField(surgery, "status")),
                ("scheduledAt", DateTimeField(surgery, "scheduledAt")),
                ("durationMinutes", NumberField(surgery, "durationMinutes")),
                ("location", TextField(surgery, "location")),
                ("patient", TextField(surgery, "patientDisplayName"))));
    }

    private const string SameLocationReason = "SameLocation";
    private const string SharedTeamMemberReason = "SharedTeamMember";

    internal static PresentableResult? SurgeryConflicts(JsonElement output) =>
        TryArray(output, "conflicts", out JsonElement values)
            ? List(ClinicPresentedRecordKinds.SurgeryConflict, values, false, conflict =>
            {
                string? id = Text(conflict, "surgeryId");
                if (id is null) return null;
                HashSet<string> reasons = TryArray(conflict, "reasons", out JsonElement array)
                    ? array.EnumerateArray().Select(item => item.GetString()).OfType<string>().ToHashSet(StringComparer.Ordinal)
                    : [];
                return new(
                    ClinicPresentedRecordKinds.SurgeryConflict,
                    id,
                    PresentableField.Text(Text(conflict, "name")),
                    Fields(
                        ("scheduledAt", DateTimeField(conflict, "scheduledAt")),
                        ("durationMinutes", NumberField(conflict, "durationMinutes")),
                        ("location", TextField(conflict, "location")),
                        ("patient", TextField(conflict, "patientDisplayName")),
                        ("sameLocation", reasons.Contains(SameLocationReason) ? PresentableField.Status(ClinicPresentedStatuses.Yes) : null),
                        ("sharedTeamMember", reasons.Contains(SharedTeamMemberReason) ? PresentableField.Status(ClinicPresentedStatuses.Yes) : null)));
            })
            : null;

    // Setup and staff.

    internal static PresentableResult? SetupGoals(JsonElement output)
    {
        if (!TryArray(output, "goals", out JsonElement goals)) return null;
        string? next = Text(output, "nextGoal");
        return List(ClinicPresentedRecordKinds.SetupGoal, goals, false, goal =>
        {
            string? name = Text(goal, "goal");
            if (name is null) return null;
            string state = Bool(goal, "done") ? ClinicPresentedStatuses.Done
                : string.Equals(name, next, StringComparison.Ordinal) ? ClinicPresentedStatuses.Next
                : ClinicPresentedStatuses.Open;
            return new(
                ClinicPresentedRecordKinds.SetupGoal,
                name,
                PresentableField.Status(name),
                Fields(("state", PresentableField.Status(state))));
        });
    }

    /// <summary>Practice members with the contact the list tool already masks.</summary>
    internal static PresentableResult? StaffMembers(JsonElement output) =>
        TryArray(output, "members", out JsonElement members)
            ? List(ClinicPresentedRecordKinds.StaffMember, members, false, member =>
            {
                string? userId = Text(member, "userId");
                return userId is null ? null : new(
                    ClinicPresentedRecordKinds.StaffMember,
                    userId,
                    PresentableField.Text(Text(member, "name")),
                    Fields(
                        ("role", StatusField(member, "role")),
                        ("maskedContact", TextField(member, "contact"))));
            })
            : null;

    // Shared projection helpers.

    private static PresentableResult List(string kind, JsonElement array, bool hasMore, Func<JsonElement, PresentableRecord?> project) =>
        new(kind, IsList: true, array.EnumerateArray().Select(project).OfType<PresentableRecord>().ToArray(), hasMore);

    private static PresentableResult? Single(string kind, PresentableRecord? record) =>
        record is null ? null : new(kind, IsList: false, [record]);

    private static IReadOnlyDictionary<string, PresentableField> Fields(params (string Name, PresentableField? Field)[] fields) =>
        fields.Where(field => field.Field is not null).ToDictionary(field => field.Name, field => field.Field!, StringComparer.Ordinal);

    private static PresentableField? TextField(JsonElement value, string name) => Text(value, name) is { } text ? PresentableField.Text(text) : null;
    private static PresentableField? StatusField(JsonElement value, string name) => Text(value, name) is { } text ? PresentableField.Status(text) : null;
    private static PresentableField? DateField(JsonElement value, string name) => Text(value, name) is { } text ? PresentableField.Date(text) : null;
    private static PresentableField? DateTimeField(JsonElement value, string name) => Text(value, name) is { } text ? PresentableField.DateTime(text) : null;
    private static PresentableField? NumberField(JsonElement value, string name) => Number(value, name) is { } number ? PresentableField.Number(number) : null;
    private static PresentableField? ClockField(JsonElement value, string name) => ClockTime(Text(value, name)) is { } time ? PresentableField.Text(time) : null;

    private static PresentableField? MinutesField(JsonElement value, string name) =>
        TimeSpan.TryParse(Text(value, name), CultureInfo.InvariantCulture, out TimeSpan span) ? PresentableField.Number(span.TotalMinutes) : null;

    /// <summary>A status the tool serializes as a name or, without a string enum converter, as its number.</summary>
    private static PresentableField? EnumField<TEnum>(JsonElement value, string name) where TEnum : struct, Enum
    {
        if (!TryProperty(value, name, out JsonElement property)) return null;
        if (property.ValueKind == JsonValueKind.String) return PresentableField.Status(property.GetString());
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out int number)) return null;
        TEnum parsed = (TEnum)Enum.ToObject(typeof(TEnum), number);
        return Enum.IsDefined(parsed) ? PresentableField.Status(parsed.ToString()) : null;
    }

    private static string? ClockTime(string? time) => time is { Length: >= 5 } && time[2] == ':' ? time[..5] : time;

    /// <summary>The value of a successful wrapped tool result; the status is a name or, unconverted, 0 for success.</summary>
    private static bool TryValue(JsonElement output, out JsonElement value)
    {
        value = default;
        if (!TryProperty(output, "status", out JsonElement status) ||
            !(status.ValueKind == JsonValueKind.Number ? status.TryGetInt32(out int code) && code == 0 : status.GetString() == nameof(AgentSchedulingStatus.Succeeded)))
            return false;
        return TryProperty(output, "value", out value) && value.ValueKind != JsonValueKind.Null;
    }

    private static bool TryArrayValue(JsonElement output, out JsonElement array) =>
        TryValue(output, out array) && array.ValueKind == JsonValueKind.Array;

    private static bool TryObjectValue(JsonElement output, out JsonElement value) =>
        TryValue(output, out value) && value.ValueKind == JsonValueKind.Object;

    /// <summary>Tool results are serialized with camelCase or default naming depending on the tool, so names match ignoring case.</summary>
    internal static bool TryProperty(JsonElement value, string name, out JsonElement property)
    {
        property = default;
        if (value.ValueKind != JsonValueKind.Object) return false;
        foreach (JsonProperty candidate in value.EnumerateObject())
        {
            if (!string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
            property = candidate.Value;
            return true;
        }
        return false;
    }

    internal static bool TryArray(JsonElement value, string name, out JsonElement array) =>
        TryProperty(value, name, out array) && array.ValueKind == JsonValueKind.Array;

    internal static string? Text(JsonElement value, string name) =>
        TryProperty(value, name, out JsonElement property)
            ? property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                JsonValueKind.True or JsonValueKind.False => property.GetRawText(),
                _ => null
            }
            : null;

    internal static double? Number(JsonElement value, string name) =>
        TryProperty(value, name, out JsonElement property) && property.ValueKind == JsonValueKind.Number
            ? property.GetDouble()
            : double.TryParse(Text(value, name), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) ? parsed : null;

    internal static bool Bool(JsonElement value, string name) =>
        TryProperty(value, name, out JsonElement property) && property.ValueKind == JsonValueKind.True;
}
