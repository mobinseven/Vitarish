using System.Text.Json;
using FluentAssertions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.ProjectManagement.Application;
using Xunit;

namespace Vitarish.ProjectManagement.Application.Tests;

public sealed class ProjectManagementPresentationBuilderTests
{
    private static readonly Guid EventId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProcedureId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData("health", "status")]
    [InlineData("get_capabilities", "status")]
    [InlineData("list_projects", "project")]
    [InlineData("get_project", "project")]
    [InlineData("get_project_snapshot", "project")]
    [InlineData("list_procedures", "procedure")]
    [InlineData("get_procedure", "procedure")]
    [InlineData("list_events", "event")]
    [InlineData("get_event", "event")]
    [InlineData("get_event_capabilities", "event")]
    [InlineData("get_event_write_permission", "event")]
    [InlineData("get_event_reminders_count", "reminder")]
    [InlineData("list_reminders", "reminder")]
    [InlineData("get_reminder", "reminder")]
    [InlineData("list_attachments", "attachment")]
    [InlineData("count_attachments", "attachment")]
    [InlineData("list_roles", "role")]
    [InlineData("list_user_roles", "role")]
    [InlineData("list_event_roles", "role")]
    [InlineData("get_event_read_roles", "role")]
    [InlineData("get_event_write_roles", "role")]
    [InlineData("get_statistics", "statistics")]
    public void Every_current_read_operation_has_a_safe_display_mapping(string operation, string family)
    {
        ProjectManagementPresentationBuilder builder = new();
        string? scopeId = ScopeId(operation);
        object payload = operation switch
        {
            "list_procedures" => new { projectId = ProjectId },
            "list_events" => new { procedureId = ProcedureId },
            _ when scopeId is not null => new { id = Guid.Parse(scopeId), eventId = EventId },
            _ => new { }
        };
        object output = operation switch
        {
            "list_projects" => new[] { new { id = ProjectId, name = "Project" } },
            "list_procedures" => new[] { new { id = ProcedureId, projectId = ProjectId, name = "Procedure" } },
            "list_events" => new[] { new { id = EventId, projectId = ProjectId, procedureId = ProcedureId, title = "Event" } },
            "list_roles" or "list_user_roles" or "list_event_roles" or "get_event_read_roles" or "get_event_write_roles" => new[] { new { id = ProjectId, name = "Role" } },
            "get_project" or "get_project_snapshot" => new { id = ProjectId, name = "Project" },
            "get_procedure" => new { id = ProcedureId, projectId = ProjectId, name = "Procedure" },
            "get_event" => new { id = EventId, projectId = ProjectId, procedureId = ProcedureId, title = "Event" },
            "get_event_capabilities" => new { canRead = true, canWrite = false },
            "get_reminder" => new { id = Guid.Parse("44444444-4444-4444-4444-444444444444"), sourceId = EventId, subject = "Reminder" },
            "list_reminders" => new[] { new { id = Guid.Parse("44444444-4444-4444-4444-444444444444"), sourceId = EventId, subject = "Reminder" } },
            "list_attachments" => new[] { new { id = Guid.Parse("55555555-5555-5555-5555-555555555555"), eventId = EventId, fileName = "note.txt", contentType = "text/plain", size = 10, content = "hello" } },
            "get_statistics" => new { projects = 1, procedures = 2, events = 3 },
            "get_event_write_permission" or "get_event_reminders_count" or "count_attachments" => true,
            _ => new { succeeded = true }
        };
        List<AgentToolExecution> executions = [];
        if (operation is "list_attachments" or "count_attachments" or "get_event_reminders_count" or "list_reminders" or "get_reminder")
        {
            executions.Add(Execution("get_event", new { id = EventId }, new { id = EventId, projectId = ProjectId, procedureId = ProcedureId }));
        }

        executions.Add(Execution(operation, payload, output));
        JsonElement result = JsonSerializer.SerializeToElement(new { parts = new[] { new { type = "sea-reference", operation, scopeId } }, actions = Array.Empty<object>() });

        ProjectManagementPresentationResult presentation = builder.Build(result, executions, new("tenant", "user", null, "en", SubjectKind: "tenant"), DateTimeOffset.UtcNow);

        presentation.Parts.Should().ContainSingle().Which.Should().BeOfType<ProjectManagementResourceCardPart>().Which.Family.Should().Be(family);
    }

    [Fact]
    public void Mutation_references_and_unverified_parents_are_rejected_without_file_data()
    {
        ProjectManagementPresentationBuilder builder = new();
        AgentToolExecution attachmentExecution = Execution("list_attachments", new { id = EventId }, new[] { new { id = Guid.NewGuid(), eventId = EventId, contentType = "image/png", content = "data:image/png;base64,secret" } });
        JsonElement mutation = JsonSerializer.SerializeToElement(new { parts = new[] { new { type = "sea-reference", operation = "delete_event" } }, actions = Array.Empty<object>() });
        FluentActions.Invoking(() => builder.Build(mutation, [attachmentExecution], new("tenant", "user", null, "en"), DateTimeOffset.UtcNow)).Should().Throw<UnauthorizedAccessException>();
        JsonElement attachment = JsonSerializer.SerializeToElement(new { parts = new[] { new { type = "sea-reference", operation = "list_attachments", scopeId = EventId.ToString("D") } }, actions = Array.Empty<object>() });
        FluentActions.Invoking(() => builder.Build(attachment, [attachmentExecution], new("tenant", "user", null, "en"), DateTimeOffset.UtcNow)).Should().Throw<UnauthorizedAccessException>();
    }

    private static string? ScopeId(string operation) => operation switch
    {
        "get_project" or "get_project_snapshot" => ProjectId.ToString("D"),
        "get_procedure" or "list_events" => ProcedureId.ToString("D"),
        "list_procedures" => ProjectId.ToString("D"),
        "get_event" or "get_event_capabilities" or "get_event_write_permission" or "get_event_reminders_count" or "list_reminders" or "list_attachments" or "count_attachments" or "list_event_roles" or "get_event_read_roles" or "get_event_write_roles" => EventId.ToString("D"),
        "get_reminder" => "44444444-4444-4444-4444-444444444444",
        _ => null
    };

    private static AgentToolExecution Execution(string operation, object payload, object output) => new("sea_manage", Guid.NewGuid().ToString("N"), JsonSerializer.Serialize(output), false, JsonSerializer.Serialize(new { operation, payload }));
}
