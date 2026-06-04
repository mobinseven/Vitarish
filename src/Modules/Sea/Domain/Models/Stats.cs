namespace Vitarish.Sea.Domain.Models;

public sealed record Stats(
    ProgressState? State,
    int Procedures,
    int Events,
    int CommittedEvents,
    int EventItems,
    int NewItems,
    bool Unread,
    DateTime? LastUpdate);

