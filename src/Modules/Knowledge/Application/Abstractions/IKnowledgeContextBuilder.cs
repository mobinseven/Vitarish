using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeContextBuilder { KnowledgeContext Build(string question, IReadOnlyList<KnowledgeHit> hits); }
