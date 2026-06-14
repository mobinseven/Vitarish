using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Evaluation;

public sealed class KnowledgeEvaluator(IKnowledgeRepository repository, IKnowledgeRetriever retriever, IKnowledgeAnswerer answerer) : IKnowledgeEvaluator
{
    public async Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken)
    {
                ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        IReadOnlyList<KnowledgeQuestion> questions = await repository.GetQuestionsAsync(request.CorpusId, request.TenantId, cancellationToken);
        List<KnowledgeEvaluationItem> items = [];
        List<int> ranks = [];

        foreach (KnowledgeQuestion question in questions)
        {
            IReadOnlyList<KnowledgeHit> hits = await retriever.RetrieveAsync(new KnowledgeQuery { CorpusId = request.CorpusId, Question = question.Question, TenantId = request.TenantId, TopK = request.TopK }, cancellationToken);
            bool hasExpectedContext = question.ExpectedChunkIds.Count > 0;
            int rank = hasExpectedContext ? FirstExpectedRank(question.ExpectedChunkIds, hits) : 0;
            if (rank > 0) ranks.Add(rank);
            KnowledgeAnswer answer = await answerer.AnswerFromHitsAsync(new KnowledgeAskRequest { CorpusId = request.CorpusId, Question = question.Question, TenantId = request.TenantId, TopK = request.TopK, SaveTrace = false, TraceKind = "evaluation" }, hits, cancellationToken);
            items.Add(new KnowledgeEvaluationItem
            {
                QuestionId = question.Id,
                HasExpectedContext = hasExpectedContext,
                ExpectedContextFoundInTop5 = rank is > 0 and <= 5,
                ExpectedContextFoundInTop10 = rank is > 0 and <= 10,
                WasAnswered = answer.IsAnswered,
                ShouldHaveAnswered = !string.IsNullOrWhiteSpace(question.ExpectedAnswer),
                GeneratedAnswer = answer.Answer
            });
        }

        IReadOnlyList<KnowledgeEvaluationItem> retrievableItems = [.. items.Where(static item => item.HasExpectedContext)];
        return new KnowledgeEvaluationRun(Guid.NewGuid(), request.CorpusId, request.TenantId, items, Ratio(retrievableItems, static item => item.ExpectedContextFoundInTop5), Ratio(retrievableItems, static item => item.ExpectedContextFoundInTop10), ranks.Count == 0 ? 0 : ranks.Average());
    }

    private static int FirstExpectedRank(IReadOnlyList<Guid> expectedChunkIds, IReadOnlyList<KnowledgeHit> hits)
    {
        for (int i = 0; i < hits.Count; i++) if (expectedChunkIds.Contains(hits[i].ChunkId)) return i + 1;
        return 0;
    }

    private static double Ratio(IReadOnlyList<KnowledgeEvaluationItem> items, Func<KnowledgeEvaluationItem, bool> predicate) => items.Count == 0 ? 0 : items.Count(predicate) / (double)items.Count;
}
