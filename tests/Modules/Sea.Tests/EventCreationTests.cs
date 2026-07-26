using FluentAssertions;
using Moq;
using Vitarish.Sea.Application.Commands.Events;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Models;
using Vitarish.Sea.Infrastructure.Handlers;
using Vitarish.Entities.Server.Repositories;

namespace Vitarish.Sea.Tests;

public sealed class EventCreationTests
{
    [Fact]
    public async Task Add_event_handler_persists_supplied_content()
    {
        Guid projectId = Guid.NewGuid();
        Guid procedureId = Guid.NewGuid();
        AddEventAfterCommand command = new(
            projectId,
            procedureId,
            null,
            "Initial consultation",
            "Review the patient's history.");
        var repository = new Mock<IEntityRepository<Event, EventDescriptor>>();
        repository
            .Setup(value => value.GetAsync(
                It.Is<EventDescriptor>(descriptor => descriptor.ProcedureId == procedureId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository
            .Setup(value => value.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event @event, CancellationToken _) => @event);

        Event? result = await new AddEventAfterHandler(repository.Object).Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProjectId.Should().Be(projectId);
        result.ProcedureId.Should().Be(procedureId);
        result.Title.Should().Be("Initial consultation");
        result.Description.Should().Be("Review the patient's history.");
    }
}
