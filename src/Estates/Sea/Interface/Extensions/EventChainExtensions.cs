namespace Vitarish.Sea.Interface.Extensions;

public static class EventChainExtensions
{
    public static IReadOnlyList<EventReadDto> SortByChain(this IEnumerable<EventReadDto> events)
    {
        Dictionary<Guid, EventReadDto> map = events.ToDictionary(e => e.Id);
        EventReadDto? first = events.FirstOrDefault(e => e.PreviousEventId is null || !map.ContainsKey(e.PreviousEventId.Value));
        if (first is null)
        {
            return [.. events];
        }

        List<EventReadDto> ordered = [first];
        EventReadDto? current = first;
        while (true)
        {
            current = events.FirstOrDefault(e => e.PreviousEventId == current.Id);
            if (current is null)
            {
                break;
            }

            ordered.Add(current);
        }
        if (ordered.Count != map.Count)
        {
            ordered.AddRange(events.Where(e => !ordered.Any(o => o.Id == e.Id)));
        }

        return ordered;
    }

    public static EventReadDto? GetFirstByChain(this IEnumerable<EventReadDto> events)
    {
        Dictionary<Guid, EventReadDto> map = events.ToDictionary(e => e.Id);
        return events.FirstOrDefault(e => e.PreviousEventId is null || !map.ContainsKey(e.PreviousEventId.Value));
    }

    public static EventReadDto? GetLastByChain(this IEnumerable<EventReadDto> events)
    {
        HashSet<Guid> referenced = [.. events.Where(e => e.PreviousEventId.HasValue).Select(e => e.PreviousEventId!.Value)];
        return events.FirstOrDefault(e => !referenced.Contains(e.Id));
    }

}

