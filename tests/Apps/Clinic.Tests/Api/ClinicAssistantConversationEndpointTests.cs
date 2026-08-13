using FluentAssertions;
using MediatR;
using Moq;
using Vitarish.Clinic.Api.ClinicAssistant;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Intelligence.Application.Agents;
using Xunit;

namespace Clinic.Tests.Api;

public sealed class ClinicAssistantConversationEndpointTests
{
    [Fact]
    public async Task Operations_sentinel_subject_is_bound_to_the_authenticated_practice_context()
    {
        AssistantContext context = OperationsContext();
        Mock<IAssistantContextFactory> factory = new(MockBehavior.Strict);
        factory.Setup(item => item.CreateOperationsAsync(null, It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(context);
        Mock<ISender> sender = new(MockBehavior.Strict);
        sender.Setup(item => item.Send(It.IsAny<RunAssistant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Success(context));

        AgentConversationEndpointResult result = await new ClinicAssistantConversationEndpoint(factory.Object, sender.Object)
            .RunAsync(new(
                "clinic.assistant.operations", "practice", "practice", "list appointments", null), CancellationToken.None);

        result.Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        sender.Verify(item => item.Send(
            It.Is<RunAssistant>(request => request.Context.PracticeId == "practice-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Operations_subject_for_another_practice_is_not_accepted()
    {
        AssistantContext context = OperationsContext();
        Mock<IAssistantContextFactory> factory = new(MockBehavior.Strict);
        factory.Setup(item => item.CreateOperationsAsync(null, It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(context);
        Mock<ISender> sender = new(MockBehavior.Strict);

        AgentConversationEndpointResult result = await new ClinicAssistantConversationEndpoint(factory.Object, sender.Object)
            .RunAsync(new(
                "clinic.assistant.operations", "practice", "other-practice", "list appointments", null), CancellationToken.None);

        result.Outcome.Should().Be(AgentConversationOutcome.NotFound);
        sender.Verify(item => item.Send(It.IsAny<RunAssistant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invalid_history_is_preserved_as_an_explicit_outcome()
    {
        AssistantContext context = OperationsContext();
        Mock<IAssistantContextFactory> factory = new(MockBehavior.Strict);
        factory.Setup(item => item.CreateOperationsAsync(null, It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(context);
        Mock<ISender> sender = new(MockBehavior.Strict);
        sender.Setup(item => item.Send(It.IsAny<RunAssistant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Success(context) with { Outcome = AssistantRunOutcome.InvalidHistory });

        AgentConversationEndpointResult result = await new ClinicAssistantConversationEndpoint(factory.Object, sender.Object)
            .RunAsync(new(
                "clinic.assistant.operations", "practice", "practice", "continue", null), CancellationToken.None);

        result.Outcome.Should().Be(AgentConversationOutcome.InvalidHistory);
    }

    [Fact]
    public void Legacy_and_canonical_agent_routes_are_registered_together()
    {
        Mock<IAssistantContextFactory> factory = new();
        Mock<ISender> sender = new();
        ClinicAssistantConversationEndpoint endpoint = new(factory.Object, sender.Object);

        endpoint.Routes.Should().Contain(route => route.AgentKey == "clinic.patient" && route.SubjectKind == "patient");
        endpoint.Routes.Should().Contain(route => route.AgentKey == "clinic.operations" && route.SubjectKind == "practice");
        endpoint.Routes.Should().Contain(route => route.AgentKey == "clinic.assistant.patient" && route.SubjectKind == "patient");
        endpoint.Routes.Should().Contain(route => route.AgentKey == "clinic.assistant.operations" && route.SubjectKind == "practice");
    }

    private static AssistantContext OperationsContext() => new(
        "practice-1", Guid.Empty, "user-1", new HashSet<string>(), "TenantOwner",
        "en-US", "UTC", "018f47c2-a6b7-7000-8000-000000000001", SubjectKind: "practice");

    private static AssistantResult Success(AssistantContext context) => new(
        "run-1", context.ConversationId, [], []);
}
