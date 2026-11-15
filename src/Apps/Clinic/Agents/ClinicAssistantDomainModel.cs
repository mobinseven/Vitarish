using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Application.Agents;
using Names = Vitarish.Clinic.Application.Assistant.Presentation.ClinicPresentableToolNames;

namespace Vitarish.Clinic.Agents;

/// <summary>
/// What clinic users talk about, for both Clinic assistants (ADR-0023). Each operation lists the patient-page tool and
/// the practice-workspace tool; the domain section names whichever the composed assistant exposes.
/// </summary>
public static class ClinicAssistantDomainModel
{
    /// <summary>Where the domain section sits among the Clinic instructions: right after the policy.</summary>
    public const int InstructionOrder = 1;

    private const string StepScreen = "the user can do this on the step's screen in the patient's procedure";
    private const string PatientPage = "the user can do this from the patient's page";

    public static AgentDomainModel Create() => new("Clinic",
    [
        new("Patient",
            "A person who receives care at this practice. Everything below belongs to one patient.",
            ["بیمار", "مراجعه‌کننده", "client"],
            ["Has procedures, appointments, prescriptions and surgeries."],
            operations:
            [
                new("find", [Names.SearchPatients], "returns candidate tokens that other practice tools take; tokens last for this turn only"),
                new("read the profile", [Names.GetPatientProfile, Names.GetPatientFactualOverview])
            ]),
        new("Procedure",
            "A treatment or care workflow for one patient, such as an implant treatment or a course of root canal visits. It is made of ordered steps.",
            ["روند درمان", "روند", "رویه", "طرح درمان", "پروسیجر", "treatment", "treatment plan", "care plan", "case"],
            [
                "Belongs to one patient; a patient can have several procedures.",
                "Has an ordered chain of steps. Its progress is how many steps are committed out of all steps.",
                "Where it stands is its current step (the last committed step) and its next step (the first open step)."
            ],
            notToConfuseWith: ["an appointment, which is a booked time slot", "a surgery booking", "a procedure template"],
            operations:
            [
                new("list with progress, current step and next step", [Names.ListPatientProcedureProgress, Names.ListOperationsPatientProcedures]),
                new("add", [Names.PreparePatientProcedure, Names.PrepareOperationsPatientProcedure], "an empty procedure; practice owners only", PatientPage)
            ]),
        new("Step",
            "One ordered item of a procedure: a visit or session, a milestone, a measurement, an intervention or a follow-up checkpoint. Steps have a fixed order and a 1-based position.",
            ["مرحله", "گام", "رویداد", "رویداد بالینی", "ویزیت", "جلسه", "event", "clinical event", "visit", "session", "milestone"],
            [
                "Belongs to one procedure of one patient.",
                "Has notes, files and reminders."
            ],
            [
                new("open", "Not done yet.", ["باز", "انجام‌نشده", "در انتظار", "pending", "not done"]),
                new("committed", "Marked done and accepted in the workflow. This is a workflow state, not a clinical signature or legal attestation.",
                    ["انجام شده", "ثبت‌شده", "نهایی‌شده", "تمام‌شده", "done", "completed", "finished", "finalised", "closed"])
            ],
            ["an appointment: a performed and documented visit is a step, a booked time slot is an appointment"],
            [
                new("list in order", [Names.ListPatientProcedureSteps, Names.ListOperationsProcedureSteps]),
                new("read with its notes, files and reminder count", [Names.GetPatientProcedureStep, Names.GetOperationsProcedureStep]),
                new("add a documented visit", [ClinicalEventToolNames.Prepare], "for a visit the clinician performed, with clinical content", PatientPage),
                new("commit (mark done) or reopen", [Names.PrepareStepStatus, Names.PrepareOperationsStepStatus], "a proposal the user confirms", StepScreen),
                new("edit, reorder or delete", [], whenUnavailable: StepScreen)
            ]),
        new("Note",
            "Text written on one step: an observation, a measurement narrative or a handoff note.",
            ["یادداشت", "توضیح", "مشاهده", "comment", "observation"],
            ["Belongs to one step; a procedure has no notes of its own."],
            operations:
            [
                new("read", [Names.GetPatientProcedureStep, Names.GetOperationsProcedureStep], "the step's latest notes are in the step"),
                new("add", [Names.PrepareStepNote, Names.PrepareOperationsStepNote], "in the user's own words; a proposal the user confirms", StepScreen)
            ]),
        new("File",
            "A document or image attached to one step, such as a consent form, a scan, a photo or a lab result. Only its name, type and size can be read, never its content.",
            ["فایل", "پیوست", "مدرک", "سند", "تصویر", "عکس", "اسکن", "رضایت‌نامه", "attachment", "document", "image", "photo", "scan"],
            ["Belongs to one step."],
            operations:
            [
                new("list", [Names.ListPatientStepFiles, Names.ListOperationsStepFiles]),
                new("add", [], whenUnavailable: "files are uploaded on the step's screen")
            ]),
        new("Reminder",
            "A message scheduled to be sent at a time, by SMS or email, about one step or one appointment.",
            ["یادآور", "یادآوری", "reminder", "alert"],
            ["Belongs to one step or one appointment; the step's reminder count is in the step."],
            notToConfuseWith: ["the appointment or step it reminds about"],
            operations:
            [
                new("read delivery status", [Names.ListPatientReminderStatus]),
                new("add to a step", [Names.PrepareStepReminder, Names.PrepareOperationsStepReminder], "sent to the signed-in user; a proposal the user confirms", StepScreen)
            ]),
        new("Access role",
            "A named group of staff, such as assistants or a specialist team, that may read or change chosen steps. A step with roles is restricted to them.",
            ["نقش", "نقش دسترسی", "تیم درمان", "role", "care team", "access"],
            ["Staff members are assigned roles; steps grant read or change access to roles."],
            operations: [new("manage", [Names.ManageCareTeamAccess], "roles, members, a step's roles and practice statistics", "practice owners can do this in the practice workspace")]),
        new("Procedure template",
            "A reusable standard procedure with its standard steps, used to start a patient's procedure. It is not about any patient.",
            ["الگو", "قالب", "پروتکل", "template", "protocol", "routine"],
            ["Its content is never a patient's record."]),
        new("Appointment",
            "A booked time slot for a patient with the practice.",
            ["نوبت", "رزرو", "booking", "reservation"],
            operations:
            [
                new("list", [Names.ListPatientAppointments, Names.ListOperationsAppointments]),
                new("find the next one", [Names.GetNextPatientAppointment])
            ]),
        new("Prescription",
            "A medication or item order written for a patient.",
            ["نسخه", "prescription"],
            operations: [new("list", [Names.ListPatientPrescriptionHistory])]),
        new("Surgery",
            "A planned or performed surgical operation for a patient, with its team, room and time.",
            ["جراحی", "عمل جراحی", "surgery"],
            operations: [new("list", [Names.ListPatientSurgeries, Names.ListOperationsSurgeries])])
    ],
    [
        "When the user asks how a patient or a procedure is doing, its status, latest state, progress or where it stands, read procedure progress: it already gives each procedure's current and next step.",
        "\"The last step\" or \"the latest session\" means the last committed step unless the user clearly means the most recently changed one.",
        "When the user names a procedure or step loosely, match it by name or position; if several match, ask which one.",
        "A restricted step is hidden by access rules: say it exists at its position and that the user cannot open it; never guess its content.",
        "When the user says an existing step is done and asks to mark, record, register or tick it (\"ثبتش کن\", \"تیک بزن\", \"انجام‌شده کن\"), propose committing that step; add a documented visit only when the user describes a new visit with clinical content to record.",
        "Never describe a committed step as signed or legally attested.",
        "Ids are for tools only; tell the user names, titles and positions."
    ]);
}
