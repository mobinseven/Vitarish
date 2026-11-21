using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vitarish.Clinic.Api;
using Vitarish.Clinic.Api.Assistant;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Intelligence.Application.Agents;
using Xunit;

namespace Clinic.Tests.Api;

/// <summary>
/// A conversation is created, resumed and deleted under one scope on every entry: the stored owner, tenant, agent
/// and subject never depend on which route deletes it, and a route for another mode never deletes it.
/// </summary>
public sealed class AssistantConversationLifecycleTests
{
    private const string PracticeId = "practice-1";
    private const string DeleteCanonicalOperations = "ClinicDeleteOperationsAssistantConversation";
    private const string DeleteLegacyOperations = "ClinicDeleteOperationsAgentConversation";
    private const string DeleteHelp = "ClinicDeleteOperationsHelpConversation";
    private const string DeleteCanonicalPatient = "ClinicDeletePatientAssistantConversation";
    private const string DeleteLegacyPatient = "ClinicDeletePatientAgentConversation";
    private static readonly Guid PatientId = Guid.Parse("018f47c2-a6b7-7000-8000-0000000000aa");

    [Theory]
    [InlineData(AssistantSubjects.OperationsAgentKey, DeleteCanonicalOperations)]
    [InlineData(AssistantSubjects.LegacyOperationsAgentKey, DeleteLegacyOperations)]
    [InlineData(AssistantSubjects.OperationsHelpAgentKey, DeleteHelp)]
    public async Task An_operations_conversation_is_created_resumed_and_deleted_under_one_scope(string agentKey, string deleteOperation)
    {
        FakeStore store = new();
        Guid conversationId = await CreateOperationsAsync(store, agentKey);

        (await store.AcquireAsync(conversationId, await ScopeOfAsync(agentKey), TimeSpan.FromMinutes(1), CancellationToken.None))
            .Outcome.Should().Be(AgentConversationOutcome.Succeeded, "resuming uses the scope it was created with");
        (await DeleteAsync(store, deleteOperation, conversationId)).Should().Be(StatusCodes.Status204NoContent);
        store.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(AssistantSubjects.OperationsAgentKey, DeleteLegacyOperations)]
    [InlineData(AssistantSubjects.OperationsAgentKey, DeleteHelp)]
    [InlineData(AssistantSubjects.LegacyOperationsAgentKey, DeleteCanonicalOperations)]
    [InlineData(AssistantSubjects.LegacyOperationsAgentKey, DeleteHelp)]
    [InlineData(AssistantSubjects.OperationsHelpAgentKey, DeleteCanonicalOperations)]
    [InlineData(AssistantSubjects.OperationsHelpAgentKey, DeleteLegacyOperations)]
    public async Task Deleting_through_the_route_of_another_mode_leaves_the_conversation_intact(string agentKey, string wrongDeleteOperation)
    {
        FakeStore store = new();
        Guid conversationId = await CreateOperationsAsync(store, agentKey);

        (await DeleteAsync(store, wrongDeleteOperation, conversationId)).Should().Be(StatusCodes.Status404NotFound);

        store.Count.Should().Be(1);
        (await store.AcquireAsync(conversationId, await ScopeOfAsync(agentKey), TimeSpan.FromMinutes(1), CancellationToken.None))
            .Outcome.Should().Be(AgentConversationOutcome.Succeeded);
    }

    [Fact]
    public async Task A_legacy_operations_row_stored_under_the_historical_subject_stays_deletable_only_by_its_owner_and_tenant()
    {
        FakeStore store = new();
        Guid conversationId = store.Seed(new(PracticeId, "user-1", AssistantSubjects.LegacyOperationsAgentKey, AssistantSubjects.PracticeKind, AssistantSubjects.PracticeKind));

        (await DeleteAsync(store, DeleteCanonicalOperations, conversationId)).Should().Be(StatusCodes.Status404NotFound);
        (await DeleteAsync(store, DeleteLegacyOperations, conversationId, userId: "user-2")).Should().Be(StatusCodes.Status404NotFound);
        (await DeleteAsync(store, DeleteLegacyOperations, conversationId, practiceId: "practice-2")).Should().Be(StatusCodes.Status404NotFound);
        store.Count.Should().Be(1);

        (await DeleteAsync(store, DeleteLegacyOperations, conversationId)).Should().Be(StatusCodes.Status204NoContent);
        store.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(AssistantSubjects.PatientAgentKey, DeleteCanonicalPatient, DeleteLegacyPatient)]
    [InlineData(AssistantSubjects.LegacyPatientAgentKey, DeleteLegacyPatient, DeleteCanonicalPatient)]
    public async Task A_patient_conversation_is_created_resumed_and_deleted_under_one_scope_and_not_by_the_other_mode(
        string agentKey, string deleteOperation, string wrongDeleteOperation)
    {
        FakeStore store = new();
        AssistantContext created = PatientContext() with { AgentKey = agentKey };
        Guid conversationId = (await store.AcquireAsync(null, AssistantSubjects.ConversationScope(created), TimeSpan.FromMinutes(1), CancellationToken.None)).Lease!.ConversationId;

        (await store.AcquireAsync(conversationId, AssistantSubjects.ConversationScope(created), TimeSpan.FromMinutes(1), CancellationToken.None))
            .Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        (await DeleteAsync(store, wrongDeleteOperation, conversationId)).Should().Be(StatusCodes.Status404NotFound);
        store.Count.Should().Be(1);
        (await DeleteAsync(store, deleteOperation, conversationId)).Should().Be(StatusCodes.Status204NoContent);
        store.Count.Should().Be(0);
    }

    private static async Task<Guid> CreateOperationsAsync(FakeStore store, string agentKey)
    {
        // The scope a turn stores is taken from the context the conversation endpoint hands to the assistant run.
        AssistantContext captured = await CaptureRunContextAsync(agentKey);
        return (await store.AcquireAsync(null, AssistantSubjects.ConversationScope(captured), TimeSpan.FromMinutes(1), CancellationToken.None)).Lease!.ConversationId;
    }

    private static async Task<AgentConversationScope> ScopeOfAsync(string agentKey) =>
        AssistantSubjects.ConversationScope(await CaptureRunContextAsync(agentKey));

    private static async Task<AssistantContext> CaptureRunContextAsync(string agentKey)
    {
        Mock<IAssistantContextFactory> factory = new(MockBehavior.Strict);
        factory.Setup(item => item.CreateOperationsAsync(null, It.IsAny<CancellationToken>(), null)).ReturnsAsync(OperationsContext());
        AssistantContext? captured = null;
        Mock<ISender> sender = new(MockBehavior.Strict);
        sender.Setup(item => item.Send(It.IsAny<RunAssistant>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<AssistantResult>, CancellationToken>((request, _) => captured = ((RunAssistant)request).Context)
            .ReturnsAsync(new AssistantResult("run-1", "conv", [], []));
        _ = await new AssistantConversationEndpoint(factory.Object, sender.Object).RunAsync(
            new(agentKey, AssistantSubjects.PracticeKind, "practice", "hello", null), CancellationToken.None);
        return captured!;
    }

    private static async Task<int> DeleteAsync(
        FakeStore store, string operation, Guid conversationId, string userId = "user-1", string practiceId = PracticeId)
    {
        Mock<IAssistantContextFactory> factory = new();
        factory.Setup(item => item.CreateOperationsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync((string? _, CancellationToken _, string? agentKey) => OperationsContext(userId, practiceId) with { AgentKey = agentKey });
        factory.Setup(item => item.CreateAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync((Guid _, string? _, CancellationToken _, string? agentKey) => PatientContext(userId, practiceId) with { AgentKey = agentKey });
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(factory.Object);
        builder.Services.AddSingleton(Mock.Of<Vitarish.Audit.Application.ISecurityAuditWriter>());
        builder.Services.AddSingleton(Mock.Of<Vitarish.Requests.Application.Abstractions.IRequestContext>());
        builder.Services.AddSingleton<IAgentConversationStore>(store);
        builder.Services.AddSingleton<ISender>(new Sender(new DeleteAgentConversationHandler(store)));
        using WebApplication app = builder.Build();
        _ = app.MapClinicApi();
        Endpoint endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == operation);
        DefaultHttpContext http = new() { RequestServices = app.Services };
        http.Request.RouteValues["conversationId"] = conversationId.ToString();
        http.Request.RouteValues["patientId"] = PatientId.ToString();
        http.Response.Body = new MemoryStream();
        await endpoint.RequestDelegate!(http);
        return http.Response.StatusCode;
    }

    private static AssistantContext OperationsContext(string userId = "user-1", string practiceId = PracticeId) => new(
        practiceId, Guid.Empty, userId, new HashSet<string>(), "TenantOwner",
        "en-US", "UTC", "018f47c2-a6b7-7000-8000-000000000001", SubjectKind: AssistantSubjects.PracticeKind);

    private static AssistantContext PatientContext(string userId = "user-1", string practiceId = PracticeId) => new(
        practiceId, PatientId, userId, new HashSet<string>(), "TenantOwner",
        "en-US", "UTC", "018f47c2-a6b7-7000-8000-000000000001", SubjectKind: AssistantSubjects.PatientKind);

    /// <summary>Resolves only the delete command, through the real handler.</summary>
    private sealed class Sender(DeleteAgentConversationHandler handler) : ISender
    {
        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            request is DeleteAgentConversation delete
                ? (Task<TResponse>)(object)handler.Handle(delete, cancellationToken)
                : throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotSupportedException();
    }

    /// <summary>An exact owner, tenant, agent and subject match, as the production store requires.</summary>
    private sealed class FakeStore : IAgentConversationStore
    {
        private readonly Dictionary<Guid, AgentConversationScope> rows = [];

        public int Count => rows.Count;

        public Guid Seed(AgentConversationScope scope)
        {
            Guid id = Guid.CreateVersion7();
            rows[id] = scope;
            return id;
        }

        public Task<AgentConversationStoreResult> AcquireAsync(Guid? conversationId, AgentConversationScope scope, TimeSpan leaseDuration, CancellationToken cancellationToken)
        {
            if (conversationId is null)
            {
                Guid created = Seed(scope);
                return Task.FromResult(new AgentConversationStoreResult(AgentConversationOutcome.Succeeded, new(created, Guid.NewGuid(), 0, null)));
            }
            return Task.FromResult(rows.TryGetValue(conversationId.Value, out AgentConversationScope? stored) && stored == scope
                ? new AgentConversationStoreResult(AgentConversationOutcome.Succeeded, new(conversationId.Value, Guid.NewGuid(), 0, null))
                : new AgentConversationStoreResult(AgentConversationOutcome.NotFound));
        }

        public Task<AgentConversationOutcome> DeleteAsync(Guid conversationId, AgentConversationScope scope, CancellationToken cancellationToken)
        {
            if (!rows.TryGetValue(conversationId, out AgentConversationScope? stored) || stored != scope)
                return Task.FromResult(AgentConversationOutcome.NotFound);
            _ = rows.Remove(conversationId);
            return Task.FromResult(AgentConversationOutcome.Succeeded);
        }

        public Task<AgentConversationOutcome> ReplaceHistoryAsync(AgentConversationLease lease, AgentConversationScope scope, string history, string historyFormat, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentConversationSummary>> ListAsync(AgentConversationScope scope, DateTimeOffset? before, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<AgentConversationDisplay?> GetDisplayAsync(Guid conversationId, AgentConversationScope scope, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> RenameAsync(Guid conversationId, AgentConversationScope scope, string title, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ReleaseAsync(AgentConversationLease lease, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<AgentConversationOwnerDeletionResult> DeleteForOwnerAsync(AgentConversationOwnerScope owner, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> DeleteExpiredAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
