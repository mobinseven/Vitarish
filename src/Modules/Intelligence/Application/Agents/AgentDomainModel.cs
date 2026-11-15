using System.Text;

namespace Vitarish.Intelligence.Application.Agents;

/// <summary>
/// An app's description of the things its users talk about, for its assistants (ADR-0023). The app owns the content;
/// <see cref="AgentDomainModelInstructions"/> turns it into the instruction text one composed assistant reads.
/// </summary>
public sealed record AgentDomainModel
{
    public AgentDomainModel(string name, IReadOnlyList<AgentDomainConcept> concepts, IReadOnlyList<string>? rules = null)
    {
        Name = Required(name, nameof(name));
        Concepts = concepts is { Count: > 0 } ? concepts : throw new ArgumentException("A domain model needs at least one concept.", nameof(concepts));
        Rules = rules ?? [];
        Validate();
    }

    public string Name { get; }
    public IReadOnlyList<AgentDomainConcept> Concepts { get; }
    public IReadOnlyList<string> Rules { get; }

    /// <summary>Every tool an operation of this model names, for checking the model against an app's registered tools.</summary>
    public IReadOnlySet<string> ToolNames =>
        Concepts.SelectMany(concept => concept.Operations).SelectMany(operation => operation.Tools).ToHashSet(StringComparer.Ordinal);

    private void Validate()
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (AgentDomainConcept concept in Concepts)
        {
            if (!names.Add(concept.Name))
                throw new ArgumentException($"Concept '{concept.Name}' is declared more than once.");
        }

        Dictionary<string, string> owners = new(StringComparer.OrdinalIgnoreCase);
        foreach (AgentDomainConcept concept in Concepts)
        {
            foreach (string word in concept.Synonyms)
            {
                if (names.Contains(word) && !string.Equals(word, concept.Name, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"Word '{word}' of concept '{concept.Name}' is the name of another concept.");
                if (owners.TryGetValue(word, out string? owner) && !string.Equals(owner, concept.Name, StringComparison.Ordinal))
                    throw new ArgumentException($"Word '{word}' is claimed by both '{owner}' and '{concept.Name}'.");
                owners[word] = concept.Name;
            }
        }
    }

    internal static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"A domain model {name} is required.", name) : value.Trim();
}

/// <summary>One kind of thing the app's users talk about.</summary>
public sealed record AgentDomainConcept
{
    public AgentDomainConcept(
        string name,
        string meaning,
        IReadOnlyList<string>? synonyms = null,
        IReadOnlyList<string>? relations = null,
        IReadOnlyList<AgentDomainState>? states = null,
        IReadOnlyList<string>? notToConfuseWith = null,
        IReadOnlyList<AgentDomainOperation>? operations = null)
    {
        Name = AgentDomainModel.Required(name, nameof(name));
        Meaning = AgentDomainModel.Required(meaning, nameof(meaning));
        Synonyms = synonyms ?? [];
        Relations = relations ?? [];
        States = states ?? [];
        NotToConfuseWith = notToConfuseWith ?? [];
        Operations = operations ?? [];
    }

    public string Name { get; }
    public string Meaning { get; }
    /// <summary>Other words users say for this concept, in any language the app supports.</summary>
    public IReadOnlyList<string> Synonyms { get; }
    /// <summary>How this concept relates to others, as short clauses such as "belongs to one patient".</summary>
    public IReadOnlyList<string> Relations { get; }
    public IReadOnlyList<AgentDomainState> States { get; }
    public IReadOnlyList<string> NotToConfuseWith { get; }
    public IReadOnlyList<AgentDomainOperation> Operations { get; }
}

/// <summary>A state a concept can be in, what it means to users, and the words they use for it.</summary>
public sealed record AgentDomainState
{
    public AgentDomainState(string name, string meaning, IReadOnlyList<string>? synonyms = null)
    {
        Name = AgentDomainModel.Required(name, nameof(name));
        Meaning = AgentDomainModel.Required(meaning, nameof(meaning));
        Synonyms = synonyms ?? [];
    }

    public string Name { get; }
    public string Meaning { get; }
    public IReadOnlyList<string> Synonyms { get; }
}

/// <summary>
/// One verb on a concept and the tools that can perform it. The first exposed tool is named; when none is exposed the
/// operation is stated as unavailable, with <see cref="WhenUnavailable"/> telling the user where to go instead.
/// </summary>
public sealed record AgentDomainOperation
{
    public AgentDomainOperation(string verb, IReadOnlyList<string> tools, string? note = null, string? whenUnavailable = null)
    {
        Verb = AgentDomainModel.Required(verb, nameof(verb));
        Tools = tools ?? [];
        if (Tools.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("A domain operation names tools by their exact names.", nameof(tools));
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        WhenUnavailable = string.IsNullOrWhiteSpace(whenUnavailable) ? null : whenUnavailable.Trim();
    }

    public string Verb { get; }
    /// <summary>Tools that perform this verb, in preference order; empty when it is never available to an assistant.</summary>
    public IReadOnlyList<string> Tools { get; }
    public string? Note { get; }
    public string? WhenUnavailable { get; }
}

/// <summary>Generates the domain section of one composed assistant from a domain model and its exposed tools.</summary>
public static class AgentDomainModelInstructions
{
    public const string SectionKey = "domain-model";

    public static string Render(AgentDomainModel model, IEnumerable<string> exposedToolNames)
    {
        ArgumentNullException.ThrowIfNull(model);
        HashSet<string> exposed = (exposedToolNames ?? []).ToHashSet(StringComparer.Ordinal);
        StringBuilder text = new();
        text.Append("Domain model: ").Append(model.Name).Append('\n');
        text.Append("These are the things the user talks about. Users may use any of the listed words; map them to the concept and use the concept's tools. ")
            .Append("Speak to the user in their own words, never in tool or field names.\n");
        foreach (AgentDomainConcept concept in model.Concepts)
        {
            text.Append('\n').Append("## ").Append(concept.Name).Append('\n');
            text.Append(concept.Meaning).Append('\n');
            if (concept.Synonyms.Count > 0)
                text.Append("Users may say: ").Append(string.Join(", ", concept.Synonyms.Select(Quote))).Append('\n');
            foreach (string relation in concept.Relations)
                text.Append("- ").Append(relation).Append('\n');
            foreach (AgentDomainState state in concept.States)
            {
                text.Append("- State \"").Append(state.Name).Append("\": ").Append(state.Meaning);
                if (state.Synonyms.Count > 0)
                    text.Append(" Users may say: ").Append(string.Join(", ", state.Synonyms.Select(Quote))).Append('.');
                text.Append('\n');
            }
            foreach (string other in concept.NotToConfuseWith)
                text.Append("- Not to be confused with: ").Append(other).Append('\n');
            foreach (AgentDomainOperation operation in concept.Operations)
            {
                string? tool = operation.Tools.FirstOrDefault(exposed.Contains);
                if (tool is not null)
                {
                    text.Append("- To ").Append(operation.Verb).Append(": ").Append(tool);
                    if (operation.Note is not null) text.Append(" (").Append(operation.Note).Append(')');
                }
                else
                {
                    text.Append("- To ").Append(operation.Verb).Append(": not available in this conversation");
                    if (operation.WhenUnavailable is not null) text.Append("; ").Append(operation.WhenUnavailable);
                }
                text.Append('\n');
            }
        }

        if (model.Rules.Count > 0)
        {
            text.Append("\n## Rules\n");
            foreach (string rule in model.Rules)
                text.Append("- ").Append(rule).Append('\n');
        }

        return text.ToString().TrimEnd();
    }

    private static string Quote(string word) => $"\"{word}\"";
}
