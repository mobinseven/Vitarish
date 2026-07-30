using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Services.Agents;

namespace Intelligence.Tests.Agents;

public sealed class HmacAgentRuntimeCredentialServiceTests
{
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly AgentExecutionContext context = new(
        "execution-1",
        new Dictionary<string, string>
        {
            ["practiceId"] = "practice-1",
            ["patientId"] = "patient-1"
        });

    [Fact]
    public void Validate_AcceptsCredentialForTheSameExecutionContext()
    {
        HmacAgentRuntimeCredentialService service = CreateService();

        string credential = service.Create(context, TimeSpan.FromMinutes(5));

        service.Validate(credential, context).Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsChangedExecutionMetadata()
    {
        HmacAgentRuntimeCredentialService service = CreateService();
        string credential = service.Create(context, TimeSpan.FromMinutes(5));
        AgentExecutionContext changedContext = context with
        {
            Metadata = new Dictionary<string, string>(context.Metadata!) { ["patientId"] = "patient-2" }
        };

        service.Validate(credential, changedContext).Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsExpiredCredential()
    {
        HmacAgentRuntimeCredentialService service = CreateService();
        string credential = service.Create(context, TimeSpan.FromMinutes(5));
        time.Advance(TimeSpan.FromMinutes(5));

        service.Validate(credential, context).Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsMalformedCredential()
    {
        CreateService().Validate("not-a-credential", context).Should().BeFalse();
    }

    [Fact]
    public void Create_RequiresSharedSecret()
    {
        HmacAgentRuntimeCredentialService service = new(
            Options.Create(new AgentRuntimeOptions()),
            time);

        FluentActions.Invoking(() => service.Create(context, TimeSpan.FromMinutes(5)))
            .Should().Throw<InvalidOperationException>();
    }

    private HmacAgentRuntimeCredentialService CreateService() => new(
        Options.Create(new AgentRuntimeOptions { SharedSecret = "test-secret" }),
        time);
}
