namespace Entities.Server.Tests;

public class ValidationTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public class TestEntityValidator : AbstractValidator<TestEntity>
    {
        public TestEntityValidator()
        {
            _ = RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MaximumLength(50)
                .WithMessage("Name must not exceed 50 characters");
        }
    }

    public ValidationTests()
    {
        ServiceCollection services = new();
        services.AddEntitiesServerTestsEntityHandlers();
        _ = services.AddSingleton<IEntityRepository<TestEntity, TestDescriptor>, InMemoryRepository>();

        // Add validation
        _ = services.AddTransient<IValidator<AddEntityCommand<TestEntity, TestDescriptor>>, TestAddEntityCommandValidator>();
        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        _provider = services.BuildServiceProvider();
    }

    public class TestAddEntityCommandValidator : AbstractValidator<AddEntityCommand<TestEntity, TestDescriptor>>
    {
        public TestAddEntityCommandValidator()
        {
            _ = RuleFor(x => x.Entity.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MaximumLength(50)
                .WithMessage("Name must not exceed 50 characters");
        }
    }

    [Fact]
    public async Task Validation_ValidEntity_Succeeds()
    {
        IMediator mediator = _provider.GetRequiredService<IMediator>();
        TestEntity entity = new() { Name = "Valid Name" };

        TestEntity? result = await mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));

        Assert.NotNull(result);
        Assert.Equal("Valid Name", result.Name);
    }

    [Fact]
    public async Task Validation_EmptyName_ThrowsException()
    {
        IMediator mediator = _provider.GetRequiredService<IMediator>();
        TestEntity entity = new() { Name = "" };

        EntityFailureException exception = await Assert.ThrowsAsync<EntityFailureException>(() =>
            mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity)));

        Assert.Equal(EntityErrorCode.Validation, exception.Error.Code);
        Assert.NotNull(exception.Error.Details);
        Assert.True(exception.Error.Details.TryGetValue("Entity.Name", out string[]? nameErrors));
        Assert.Contains("Name is required", nameErrors);
    }

    [Fact]
    public async Task Validation_NameTooLong_ThrowsException()
    {
        IMediator mediator = _provider.GetRequiredService<IMediator>();
        TestEntity entity = new() { Name = new string('A', 51) }; // 51 characters

        EntityFailureException exception = await Assert.ThrowsAsync<EntityFailureException>(() =>
            mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity)));

        Assert.Equal(EntityErrorCode.Validation, exception.Error.Code);
        Assert.NotNull(exception.Error.Details);
        Assert.True(exception.Error.Details.TryGetValue("Entity.Name", out string[]? nameErrors));
        Assert.Contains("Name must not exceed 50 characters", nameErrors);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}
