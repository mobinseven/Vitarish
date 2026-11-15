using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;

namespace Vitarish.Intelligence.Tests.GuidedSurfaces;

public sealed class PresentedSurfaceTests
{
    private const string AgentKey = "test.agent";
    private const string ReadTool = "list_things";
    private const string RunId = "run-1";
    private const string Handle = "r1-run1";

    private readonly InMemoryStore _store = new();

    private static PresentableResult? Things(JsonElement output) =>
        output.TryGetProperty("items", out JsonElement items)
            ? new("test.thing", IsList: true,
                items.EnumerateArray().Select(item => new PresentableRecord(
                    "test.thing",
                    item.GetProperty("id").GetString()!,
                    PresentableField.Text(item.GetProperty("name").GetString()),
                    new Dictionary<string, PresentableField> { ["size"] = PresentableField.Number(item.GetProperty("size").GetDouble()) })).ToArray())
            : null;

    // The model fills an exact shape per component instead of writing the surface as free JSON text.
    [Fact]
    public void The_present_tool_takes_a_strict_shape_per_catalog_component()
    {
        using JsonDocument schema = JsonDocument.Parse(new PresentSurfaceTool(_store).Definition.ParametersJsonSchema);
        JsonElement items = schema.RootElement.GetProperty("properties").GetProperty("surface")
            .GetProperty("properties").GetProperty("components").GetProperty("items").GetProperty("anyOf");
        using JsonDocument catalog = JsonDocument.Parse(AssistantSurfaceCatalog.Json);

        items.EnumerateArray().Select(shape => shape.GetProperty("properties").GetProperty("component").GetProperty("enum")[0].GetString())
            .Should().BeEquivalentTo(catalog.RootElement.GetProperty("components").EnumerateObject().Select(component => component.Name));
        foreach (JsonElement shape in items.EnumerateArray())
        {
            shape.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
            shape.GetProperty("required").EnumerateArray().Select(name => name.GetString())
                .Should().BeEquivalentTo(shape.GetProperty("properties").EnumerateObject().Select(property => property.Name), "strict schemas require every property");
        }
        JsonElement list = items.EnumerateArray().Single(shape => shape.GetProperty("properties").GetProperty("component").GetProperty("enum")[0].GetString() == "RecordList");
        list.GetProperty("properties").GetProperty("result").GetProperty("required")[0].GetString().Should().Be("path");
        list.GetProperty("properties").GetProperty("fields").GetProperty("maxItems").GetInt32().Should().Be(3);
        JsonElement column = items.EnumerateArray().Single(shape => shape.GetProperty("properties").GetProperty("component").GetProperty("enum")[0].GetString() == "Column");
        column.GetProperty("properties").GetProperty("children").GetProperty("items").GetProperty("type").GetString().Should().Be("string");
    }

    private PresentableResultPublisher Publisher() => new(
        new AgentToolPresentationCatalog([new AgentToolPresentation([AgentKey], ReadTool, Things)]),
        _store,
        NullLogger<PresentableResultPublisher>.Instance);

    private static AgentExecutionContext Context(bool validated = true) =>
        new(RunId, validated ? new Dictionary<string, string> { [PresentSurfaceTool.ValidatedMetadataKey] = "true" } : new Dictionary<string, string>());

    private static string Surface(string handle, params string[] fields) =>
        JsonSerializer.Serialize(new
        {
            surface = new
            {
                components = new object[]
                {
                    new { id = "root", component = "RecordList", result = new Dictionary<string, string> { ["path"] = "/" + handle }, fields }
                }
            }
        });

    [Fact]
    public async Task A_declared_read_gets_a_handle_the_model_can_present()
    {
        AgentToolResult published = await Publisher().PublishAsync(AgentKey, ReadTool, RunId,
            new("call-1", """{"items":[{"id":"t1","name":"First","size":3}]}"""), CancellationToken.None);

        using JsonDocument output = JsonDocument.Parse(published.Output);
        JsonElement presentation = output.RootElement.GetProperty(PresentableResultPublisher.OutputProperty);
        presentation.GetProperty("handle").GetString().Should().Be(Handle);
        presentation.GetProperty("fields").EnumerateArray().Select(field => field.GetString()).Should().Equal("size");
        output.RootElement.GetProperty("items").GetArrayLength().Should().Be(1, "the read result itself is unchanged");
    }

    [Fact]
    public async Task Undeclared_tools_other_agents_and_failed_reads_are_left_alone()
    {
        const string output = """{"items":[{"id":"t1","name":"First","size":3}]}""";
        PresentableResultPublisher publisher = Publisher();

        (await publisher.PublishAsync(AgentKey, "other_tool", RunId, new("c", output), CancellationToken.None)).Output.Should().Be(output);
        (await publisher.PublishAsync("other.agent", ReadTool, RunId, new("c", output), CancellationToken.None)).Output.Should().Be(output);
        (await publisher.PublishAsync(AgentKey, ReadTool, RunId, new("c", output, IsError: true), CancellationToken.None)).Output.Should().Be(output);
    }

    [Fact]
    public async Task A_read_still_succeeds_when_its_result_cannot_be_stored()
    {
        PresentableResultPublisher publisher = new(
            new AgentToolPresentationCatalog([new AgentToolPresentation([AgentKey], ReadTool, Things)]),
            new FailingStore(),
            NullLogger<PresentableResultPublisher>.Instance);
        const string output = """{"items":[{"id":"t1","name":"First","size":3}]}""";

        AgentToolResult published = await publisher.PublishAsync(AgentKey, ReadTool, RunId, new("c", output), CancellationToken.None);

        published.Output.Should().Be(output);
        published.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Presenting_accepts_handles_of_this_run_with_their_fields()
    {
        await Publisher().PublishAsync(AgentKey, ReadTool, RunId, new("c", """{"items":[{"id":"t1","name":"First","size":3}]}"""), CancellationToken.None);

        AgentToolResult result = await new PresentSurfaceTool(_store).ExecuteAsync(new("p1", PresentSurfaceTool.Name, Surface(Handle, "size")), Context());

        result.IsError.Should().BeFalse(result.Output);
    }

    [Theory]
    [InlineData("r9-un1", "size", "not a presentation handle")]
    [InlineData(Handle, "secret", "not a field")]
    public async Task Presenting_refuses_unknown_handles_and_fields(string handle, string field, string problem)
    {
        await Publisher().PublishAsync(AgentKey, ReadTool, RunId, new("c", """{"items":[{"id":"t1","name":"First","size":3}]}"""), CancellationToken.None);

        AgentToolResult result = await new PresentSurfaceTool(_store).ExecuteAsync(new("p1", PresentSurfaceTool.Name, Surface(handle, field)), Context());

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain(problem);
    }

    [Fact]
    public async Task A_handle_remembered_from_an_earlier_turn_never_names_a_result_of_this_turn()
    {
        AgentToolResult earlier = await Publisher().PublishAsync(AgentKey, ReadTool, "earlier-turn", new("c", """{"items":[{"id":"old","name":"Old","size":1}]}"""), CancellationToken.None);
        await Publisher().PublishAsync(AgentKey, ReadTool, RunId, new("c", """{"items":[{"id":"t1","name":"First","size":3}]}"""), CancellationToken.None);
        string earlierHandle = JsonDocument.Parse(earlier.Output).RootElement.GetProperty(PresentableResultPublisher.OutputProperty).GetProperty("handle").GetString()!;

        AgentToolResult result = await new PresentSurfaceTool(_store).ExecuteAsync(new("p1", PresentSurfaceTool.Name, Surface(earlierHandle, "size")), Context());

        earlierHandle.Should().NotBe(Handle, "both runs hold a first result");
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Presenting_refuses_a_surface_the_runtime_did_not_validate()
    {
        await Publisher().PublishAsync(AgentKey, ReadTool, RunId, new("c", """{"items":[{"id":"t1","name":"First","size":3}]}"""), CancellationToken.None);

        AgentToolResult result = await new PresentSurfaceTool(_store).ExecuteAsync(new("p1", PresentSurfaceTool.Name, Surface(Handle, "size")), Context(validated: false));

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("not validated");
    }

    [Fact]
    public async Task Presenting_refuses_a_literal_where_a_result_binding_belongs()
    {
        string literal = """{"surface":{"components":[{"id":"root","component":"RecordCard","result":"Sara","fields":[]}]}}""";

        AgentToolResult result = await new PresentSurfaceTool(_store).ExecuteAsync(new("p1", PresentSurfaceTool.Name, literal), Context());

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task The_collector_fills_each_presented_surface_from_its_handles()
    {
        await Publisher().PublishAsync(AgentKey, ReadTool, RunId, new("c", """{"items":[{"id":"t1","name":"First","size":3}]}"""), CancellationToken.None);
        AgentToolExecution[] executions =
        [
            new(ReadTool, "c", "{}"),
            new(PresentSurfaceTool.Name, "p1", """{"status":"shown"}""", ArgumentsJson: Surface(Handle, "size")),
            new(PresentSurfaceTool.Name, "p2", """{"error":"x"}""", IsError: true, ArgumentsJson: Surface(Handle, "size"))
        ];

        IReadOnlyList<PresentedSurface> surfaces = await new PresentedSurfaceCollector(_store).CollectAsync(RunId, executions, CancellationToken.None);

        PresentedSurface surface = surfaces.Should().ContainSingle("a refused presentation shows nothing").Which;
        surface.SurfaceId.Should().Be("p1");
        surface.CatalogId.Should().Be(AssistantSurfaceCatalog.Id);
        surface.DataModel.GetProperty(Handle).GetProperty("records")[0].GetProperty("reference").GetString().Should().Be("t1");
        surface.Components.GetRawText().Should().NotContain("First", "the layout carries bindings, the data model carries values");
    }

    [Fact]
    public void The_present_tool_carries_the_catalog_the_runtime_validates_against()
    {
        AgentToolDefinition definition = new PresentSurfaceTool(_store).Definition;

        definition.SurfaceCatalog.Should().Be(AssistantSurfaceCatalog.Json);
        definition.Description.Should().Contain("RecordList").And.Contain(AssistantSurfaceCatalog.Id);
    }

    private sealed class InMemoryStore : IPresentableResultStore
    {
        private readonly ConcurrentDictionary<(string, string), PresentableResult> _results = new();
        private readonly ConcurrentDictionary<string, int> _counters = new();

        public Task<string> SaveAsync(string executionId, PresentableResult result, CancellationToken cancellationToken)
        {
            string handle = PresentableHandles.Create(executionId, _counters.AddOrUpdate(executionId, 1, (_, current) => current + 1));
            _results[(executionId, handle)] = result;
            return Task.FromResult(handle);
        }

        public Task<PresentableResult?> GetAsync(string executionId, string handle, CancellationToken cancellationToken) =>
            Task.FromResult(_results.TryGetValue((executionId, handle), out PresentableResult? result) ? result : null);
    }

    private sealed class FailingStore : IPresentableResultStore
    {
        public Task<string> SaveAsync(string executionId, PresentableResult result, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The store is unavailable.");

        public Task<PresentableResult?> GetAsync(string executionId, string handle, CancellationToken cancellationToken) =>
            Task.FromResult<PresentableResult?>(null);
    }
}
