using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeChunker { IReadOnlyList<KnowledgeChunkDraft> Chunk(KnowledgeDocument document); }
