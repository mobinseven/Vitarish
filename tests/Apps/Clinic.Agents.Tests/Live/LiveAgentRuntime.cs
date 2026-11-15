using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Infrastructure.Extensions;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Vitarish.Clinic.Application.Assistant.Presentation;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Services.Agents;
using Xunit;

namespace Vitarish.Clinic.Agents.Tests.Live;

/// <summary>
/// Settings for the live agent behaviour suite. It runs only when an OpenAI key is supplied, so
/// ordinary test runs never call a model. See docs/platform/modules/intelligence/agent-behaviour-spec.md.
/// </summary>
internal static class LiveAgentSettings
{
    public const string OpenAiKeyVariable = "VITARISH_LIVE_AGENT_OPENAI_API_KEY";
    public const string RuntimeImageVariable = "VITARISH_LIVE_AGENT_RUNTIME_IMAGE";
    public const string ModelVariable = "VITARISH_LIVE_AGENT_MODEL";
    public const string DefaultRuntimeImage = "vitarish-agent-runtime:local";
    public const string DefaultModel = "gpt-6-luna";

    public static string? OpenAiKey => Environment.GetEnvironmentVariable(OpenAiKeyVariable);
    public static string RuntimeImage => Environment.GetEnvironmentVariable(RuntimeImageVariable) is { Length: > 0 } image ? image : DefaultRuntimeImage;
    public static string Model => Environment.GetEnvironmentVariable(ModelVariable) is { Length: > 0 } model ? model : DefaultModel;
}

internal sealed class LiveAgentFactAttribute : FactAttribute
{
    public LiveAgentFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(LiveAgentSettings.OpenAiKey))
            Skip = $"The live agent behaviour suite runs only when {LiveAgentSettings.OpenAiKeyVariable} is set.";
    }
}

internal sealed class LiveAgentTheoryAttribute : TheoryAttribute
{
    public LiveAgentTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(LiveAgentSettings.OpenAiKey))
            Skip = $"The live agent behaviour suite runs only when {LiveAgentSettings.OpenAiKeyVariable} is set.";
    }
}

/// <summary>One recorded tool call the model made through the runtime.</summary>
internal sealed record RecordedToolCall(string ToolName, JsonElement Arguments);

/// <summary>
/// Runs the real agent runtime image in Docker against the real model, and answers its tool
/// callbacks from scripted results, recording every call the model makes.
/// </summary>
public sealed class LiveAgentRuntime : IAsyncLifetime
{
    private const int RuntimePort = 3003;
    private const string ToolCallbackPath = "/tools";
    private readonly string sharedSecret = Guid.NewGuid().ToString("N");
    private readonly ConcurrentQueue<RecordedToolCall> calls = new();
    private readonly ConcurrentDictionary<string, string> scriptedResults = new(StringComparer.Ordinal);
    private readonly CalendarDateConversionTool calendarTool = new();
    // Presentation runs for real, as Main runs it: handles on presentable reads, then the present tool.
    private readonly InMemoryPresentableResultStore presentables = new();
    private readonly PresentableResultPublisher publisher;
    private readonly PresentSurfaceTool presentTool;

    public LiveAgentRuntime()
    {
        publisher = new(new AgentToolPresentationCatalog(ClinicAssistantPresentations.All()), presentables, NullLogger<PresentableResultPublisher>.Instance);
        presentTool = new(presentables);
    }

    /// <summary>The surfaces a finished run presented, filled from its handles, as Main returns them.</summary>
    internal Task<IReadOnlyList<PresentedSurface>> SurfacesAsync(string executionId, AgentRunResult result) =>
        new PresentedSurfaceCollector(presentables).CollectAsync(executionId, result.ToolExecutions, CancellationToken.None);
    private WebApplication? callbackHost;
    private IContainer? runtime;

    internal AgentRuntimeHttpRunner Runner { get; private set; } = null!;

    internal IReadOnlyList<RecordedToolCall> Calls => [.. calls];

    internal void Reset()
    {
        calls.Clear();
        scriptedResults.Clear();
    }

    /// <summary>
    /// Why a run failed: the runner hides provider detail by design, so the runtime's own failure
    /// lines (provider status and code, never the key or payloads) are attached to the assertion.
    /// </summary>
    internal async Task<string> FailureReasonAsync(AgentRunResult result)
    {
        if (result.IsSuccess || runtime is null) return string.Empty;
        (string stdout, string stderr) = await runtime.GetLogsAsync();
        string[] lines = (stdout + stderr).Split('\n');
        int failure = Array.FindLastIndex(lines, line => line.Contains("run failed", StringComparison.Ordinal));
        string detail = failure < 0 ? string.Empty : string.Join('\n', lines.Skip(failure).Take(10));
        return $"{result.Error?.Code}: {result.Error?.Message}\n{detail}";
    }

    /// <summary>Answers calls to <paramref name="toolName"/> with <paramref name="resultJson"/>; unscripted tools answer with an empty success.</summary>
    internal void Script(string toolName, string resultJson) => scriptedResults[toolName] = resultJson;

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(LiveAgentSettings.OpenAiKey)) return;

        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        callbackHost = builder.Build();
        callbackHost.MapPost(ToolCallbackPath, AnswerToolCallAsync);
        await callbackHost.StartAsync();
        int callbackPort = new Uri(callbackHost.Services.GetRequiredService<IServer>()
            .Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.Single()).Port;
        await TestcontainersSettings.ExposeHostPortsAsync((ushort)callbackPort);

        runtime = new ContainerBuilder(LiveAgentSettings.RuntimeImage)
            .WithEnvironment("OPENAI_API_KEY", LiveAgentSettings.OpenAiKey)
            .WithEnvironment("AGENT_RUNTIME_SHARED_SECRET", sharedSecret)
            .WithPortBinding(RuntimePort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(RuntimePort).ForPath("/api/health")))
            .Build();
        await runtime.StartAsync();

        string baseUrl = $"http://{runtime.Hostname}:{runtime.GetMappedPublicPort(RuntimePort)}";
        Runner = new AgentRuntimeHttpRunner(
            new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(5) },
            Options.Create(new AgentRuntimeOptions
            {
                BaseUrl = baseUrl,
                ToolCallbackUrl = $"http://host.testcontainers.internal:{callbackPort}{ToolCallbackPath}",
                SharedSecret = sharedSecret
            }),
            new UnverifiedCredentials(),
            NullLogger<AgentRuntimeHttpRunner>.Instance);
    }

    public async Task DisposeAsync()
    {
        if (runtime is not null) await runtime.DisposeAsync();
        if (callbackHost is not null) await callbackHost.DisposeAsync();
    }

    private async Task<IResult> AnswerToolCallAsync(HttpRequest request)
    {
        using JsonDocument body = await JsonDocument.ParseAsync(request.Body);
        JsonElement envelope = body.RootElement.GetProperty("executionEnvelope");
        string toolName = envelope.GetProperty("toolName").GetString()!;
        string callId = envelope.GetProperty("toolCallId").GetString()!;
        string argumentsJson = body.RootElement.GetProperty("argumentsJson").GetString()!;
        string executionId = envelope.GetProperty("executionId").GetString()!;
        string agentKey = envelope.GetProperty("agentKey").GetString()!;
        bool surfaceValidated = body.RootElement.TryGetProperty("surfaceValidated", out JsonElement validated) && validated.ValueKind == JsonValueKind.True;
        using JsonDocument arguments = JsonDocument.Parse(argumentsJson);
        calls.Enqueue(new(toolName, arguments.RootElement.Clone()));

        if (toolName == PresentSurfaceTool.Name)
        {
            Dictionary<string, string> metadata = new(StringComparer.Ordinal);
            if (surfaceValidated) metadata[PresentSurfaceTool.ValidatedMetadataKey] = "true";
            AgentToolResult presented = await presentTool.ExecuteAsync(new(callId, toolName, argumentsJson), new(executionId, metadata));
            return Results.Json(new { callId, output = presented.Output, isError = presented.IsError });
        }

        // The calendar conversion is deterministic and needs no clinic data, so it runs for real.
        if (toolName == CalendarDateConversionTool.Name)
        {
            AgentToolResult converted = await calendarTool.ExecuteAsync(new(callId, toolName, argumentsJson), new("live-spec"));
            return Results.Json(new { callId, output = converted.Output, isError = converted.IsError });
        }
        string output = scriptedResults.TryGetValue(toolName, out string? scripted) ? scripted : "{\"status\":\"Succeeded\",\"value\":[]}";
        AgentToolResult published = await publisher.PublishAsync(agentKey, toolName, executionId, new(callId, output), CancellationToken.None);
        return Results.Json(new { callId, output = published.Output, isError = false });
    }

    private sealed class UnverifiedCredentials : IAgentRuntimeCredentialService
    {
        public string Create(AgentRuntimeExecutionEnvelope envelope) => "live-spec-credential";
        public bool Validate(string credential, AgentRuntimeExecutionEnvelope envelope) => true;
        public bool ValidateCallback(string credential, AgentRuntimeExecutionEnvelope envelope) => true;
    }
}

/// <summary>Composes the operations assistant exactly as production does, from the real Clinic capabilities.</summary>
internal static class LiveOperationsAssistant
{
    public static readonly AssistantContext Context = new(
        "practice-1",
        Guid.Empty,
        "user-1",
        new HashSet<string>(),
        AssistantOperationsScope.TenantOwnerRole,
        "en-US",
        "UTC",
        "conversation-1",
        SubjectKind: AssistantSubjects.PracticeKind);

    public static ComposedAgentManifest<AssistantCompositionContext> Compose(DateTimeOffset now)
    {
        IReadOnlyCollection<IAssistantCapability> capabilities = RealCapabilities()
            .Where(capability => AssistantCapabilityKeys.OperationsProfile.Contains(capability.Name, StringComparer.Ordinal))
            .Where(capability => capability.IsAvailable(Context))
            .ToArray();
        AssistantCompositionContext composition = new(Context, capabilities, now);
        AgentProfileComposer<AssistantCompositionContext> composer = new(
            new AgentCapabilityCatalog<AssistantCompositionContext>(
                AssistantCapabilityKeys.OperationsProfile.Select(key => (IAgentCapabilityPack<AssistantCompositionContext>)new AssistantCapabilityPack(key))),
            new IntelligenceAgentToolCatalog([new CalendarDateConversionTool()]));
        return composer.Compose(
            new ClinicOperationsAssistantProfile(composition, new AssistantOptions(LiveAgentSettings.Model)),
            composition,
            _ => new AgentExposureDecision(true, "enabled"));
    }

    public static AgentExecutionContext ExecutionContext(DateTimeOffset now) => new(
        "live-" + Guid.NewGuid().ToString("N"),
        new Dictionary<string, string>
        {
            ["tenantId"] = Context.PracticeId,
            ["practiceId"] = Context.PracticeId,
            ["subjectKind"] = AssistantSubjects.PracticeKind,
            ["subjectId"] = Context.PracticeId,
            ["patientId"] = Guid.Empty.ToString("D"),
            ["authenticatedUserId"] = Context.AuthenticatedUserId,
            ["conversationId"] = Context.ConversationId,
            ["agentKey"] = AssistantSubjects.OperationsAgentKey,
            ["permissions"] = "[]",
            ["role"] = Context.Role,
            ["locale"] = Context.Locale,
            ["timeZone"] = Context.TimeZone,
            ["calendar"] = Context.Calendar,
            ["currentUtc"] = now.ToString("O"),
            ["currentLocal"] = now.ToString("O"),
            ["correlationId"] = "live-spec"
        });

    /// <summary>The patient-page assistant for one patient, composed with every patient capability unlocked.</summary>
    internal static class Patient
    {
        public static readonly Guid PatientId = Guid.Parse("7a1f3a52-6a3e-4a39-9f0e-3d1c2b4a5e61");

        public static AssistantContext Context => new(
            "practice-1",
            PatientId,
            "3f2b8c1e-9d4a-4e7b-8a6c-5b1d2e3f4a5b",
            RealCapabilities()
                .Where(capability => AssistantCapabilityKeys.Patient.Contains(capability.Name, StringComparer.Ordinal))
                .SelectMany(capability => capability.RequiredPermissions.Concat(capability.ActionPolicies.SelectMany(policy => policy.RequiredPermissions)))
                .ToHashSet(StringComparer.Ordinal),
            AssistantOperationsScope.TenantOwnerRole,
            "en-US",
            "UTC",
            "conversation-2",
            SubjectKind: AssistantSubjects.PatientKind);

        public static ComposedAgentManifest<AssistantCompositionContext> Compose(DateTimeOffset now)
        {
            AssistantContext context = Context;
            IReadOnlyCollection<IAssistantCapability> capabilities = RealCapabilities()
                .Where(capability => AssistantCapabilityKeys.Patient.Contains(capability.Name, StringComparer.Ordinal))
                .Where(capability => capability.IsAvailable(context))
                .ToArray();
            AssistantCompositionContext composition = new(context, capabilities, now);
            AgentProfileComposer<AssistantCompositionContext> composer = new(
                new AgentCapabilityCatalog<AssistantCompositionContext>(
                    AssistantCapabilityKeys.Patient.Select(key => (IAgentCapabilityPack<AssistantCompositionContext>)new AssistantCapabilityPack(key))),
                new IntelligenceAgentToolCatalog([new CalendarDateConversionTool()]));
            return composer.Compose(
                new ClinicPatientAssistantProfile(composition, new AssistantOptions(LiveAgentSettings.Model)),
                composition,
                _ => new AgentExposureDecision(true, "enabled"));
        }

        public static AgentExecutionContext ExecutionContext(DateTimeOffset now)
        {
            AssistantContext context = Context;
            return new(
                "live-" + Guid.NewGuid().ToString("N"),
                new Dictionary<string, string>
                {
                    ["tenantId"] = context.PracticeId,
                    ["practiceId"] = context.PracticeId,
                    ["subjectKind"] = AssistantSubjects.PatientKind,
                    ["subjectId"] = PatientId.ToString("D"),
                    ["patientId"] = PatientId.ToString("D"),
                    ["authenticatedUserId"] = context.AuthenticatedUserId,
                    ["conversationId"] = context.ConversationId,
                    ["agentKey"] = AssistantSubjects.PatientAgentKey,
                    ["permissions"] = System.Text.Json.JsonSerializer.Serialize(context.Permissions),
                    ["role"] = context.Role,
                    ["locale"] = context.Locale,
                    ["timeZone"] = context.TimeZone,
                    ["calendar"] = context.Calendar,
                    ["currentUtc"] = now.ToString("O"),
                    ["currentLocal"] = now.ToString("O"),
                    ["correlationId"] = "live-spec"
                });
        }
    }

    // Builds the Clinic capabilities through the production registration, so the tool set is exactly
    // what Main composes. Tool callbacks are answered by the suite, so the data dependencies behind the
    // tools are never called and are satisfied with mocks.
    internal static IEnumerable<IAssistantCapability> RealCapabilities() => RealServices().GetServices<IAssistantCapability>();

    /// <summary>The production Clinic assistant registration, with data dependencies mocked.</summary>
    internal static IServiceProvider RealServices()
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddSingleton<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>(
            new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());
        _ = services.AddClinicInfrastructure();
        _ = services.AddAssistantComposition();
        // The modules register these tools in their own startup (SeaModule, SearchModule, ContentsModule);
        // the Clinic capabilities adapt them. Keep these lines in step with those registrations.
        _ = services.AddTransient<Vitarish.Sea.Application.Agents.ISeaManagementAgentTool, Vitarish.Sea.Infrastructure.Agents.SeaManagementAgentTool>();
        _ = services.AddTransient<Vitarish.Search.Agents.ISearchAgentTool>(provider => new Vitarish.Search.Agents.SearchAgentTool(
            provider.GetRequiredService<Vitarish.Search.Application.Abstractions.ISearchService>(), new Vitarish.Search.Agents.SearchAgentOptions()));
        _ = services.AddTransient<Vitarish.Contents.Agents.IContentsAgentTool, Vitarish.Contents.Agents.ContentsListSitesAgentTool>();
        _ = services.AddTransient<Vitarish.Contents.Agents.IContentsAgentTool, Vitarish.Contents.Agents.ContentsListTypesAgentTool>();
        _ = services.AddTransient<Vitarish.Contents.Agents.IContentsAgentTool, Vitarish.Contents.Agents.ContentsListEntriesAgentTool>();
        _ = services.AddTransient<Vitarish.Contents.Agents.IContentsAgentTool, Vitarish.Contents.Agents.ContentsGetEntryAgentTool>();
        _ = services.AddTransient<Vitarish.Contents.Agents.IContentsAgentTool, Vitarish.Contents.Agents.ContentsCreateDraftEntryAgentTool>();
        _ = services.AddTransient<Vitarish.Contents.Agents.IContentsAgentTool, Vitarish.Contents.Agents.ContentsSaveDraftAgentTool>();
        _ = services.AddSingleton(new Mock<Vitarish.Search.Application.Abstractions.ISearchService>().Object);
        AddMocksForMissingDependencies(services);
        return services.BuildServiceProvider();
    }

    private static void AddMocksForMissingDependencies(ServiceCollection services)
    {
        Queue<Type> pending = new(services.Select(descriptor => descriptor.ImplementationType).OfType<Type>());
        HashSet<Type> visited = [];
        while (pending.TryDequeue(out Type? type))
        {
            if (!visited.Add(type) || type.IsGenericTypeDefinition || type.Namespace?.StartsWith("Vitarish.", StringComparison.Ordinal) != true) continue;
            foreach (ParameterInfo parameter in type.GetConstructors().SelectMany(constructor => constructor.GetParameters()))
            {
                Type dependency = parameter.ParameterType;
                if (dependency.IsGenericType && dependency.GetGenericTypeDefinition() == typeof(IEnumerable<>)) continue;
                if (services.Any(descriptor => descriptor.ServiceType == dependency ||
                    (dependency.IsGenericType && descriptor.ServiceType == dependency.GetGenericTypeDefinition()))) continue;
                if (!dependency.IsVisible) continue;
                if (typeof(Microsoft.EntityFrameworkCore.DbContext).IsAssignableFrom(dependency))
                {
                    Microsoft.EntityFrameworkCore.DbContextOptionsBuilder builder = (Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)
                        Activator.CreateInstance(typeof(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<>).MakeGenericType(dependency))!;
                    _ = Microsoft.EntityFrameworkCore.InMemoryDbContextOptionsExtensions.UseInMemoryDatabase(builder, "live-agent-spec");
                    _ = services.AddSingleton(builder.Options.GetType(), builder.Options);
                    _ = services.AddTransient(dependency);
                    pending.Enqueue(dependency);
                }
                else if (dependency.IsInterface || dependency.IsAbstract)
                    _ = services.AddSingleton(dependency, ((Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(dependency))!).Object);
                else if (dependency.IsClass && dependency != typeof(string))
                {
                    _ = services.AddTransient(dependency);
                    pending.Enqueue(dependency);
                }
            }
        }
    }
}

[CollectionDefinition(Name)]
public sealed class LiveAgentCollection : ICollectionFixture<LiveAgentRuntime>
{
    public const string Name = "Live agent runtime";
}
