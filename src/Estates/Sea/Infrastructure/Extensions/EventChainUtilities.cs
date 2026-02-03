namespace Vitarish.Sea.Infrastructure.Extensions;

public static class EventChainUtilities
{
    public static IReadOnlyList<Event> OrderByChain(IReadOnlyList<Event> events)
    {
        if (events.Count <= 1)
        {
            return events;
        }

        Dictionary<Guid, Event> map = events.ToDictionary(static e => e.Id);
        Event? first = events.FirstOrDefault(e => e.PreviousEventId is null || !map.ContainsKey(e.PreviousEventId.Value));
        if (first is null)
        {
            return events;
        }

        List<Event> ordered = [first];
        HashSet<Guid> visited = [first.Id];
        Event? current = first;

        while (true)
        {
            Event? next = events.FirstOrDefault(e => e.PreviousEventId == current!.Id && !visited.Contains(e.Id));
            if (next is null)
            {
                break;
            }

            ordered.Add(next);
            _ = visited.Add(next.Id);
            current = next;
        }

        if (ordered.Count != map.Count)
        {
            ordered.AddRange(events.Where(e => !visited.Contains(e.Id)));
        }

        return ordered;
    }

    public static Event? FindPrevious(IReadOnlyList<Event> events, Event current, IReadOnlyDictionary<Guid, Event> map)
        => current.PreviousEventId is Guid previousId && map.TryGetValue(previousId, out Event? previous)
            ? previous
            : null;

    public static Event? FindNext(IReadOnlyList<Event> events, Guid eventId)
        => events.FirstOrDefault(e => e.PreviousEventId == eventId);

    public static Event? FindLast(IReadOnlyList<Event> events)
    {
        if (events.Count == 0)
        {
            return null;
        }

        HashSet<Guid> referenced = events
            .Where(e => e.PreviousEventId.HasValue)
            .Select(e => e.PreviousEventId!.Value)
            .ToHashSet();

        return events.FirstOrDefault(e => !referenced.Contains(e.Id));
    }
}
