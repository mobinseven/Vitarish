using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Evaluation;

public sealed class KnowledgeEvaluator(IKnowledgeRepository repository, IKnowledgeRetriever retriever, IKnowledgeAnswerer answerer) : IKnowledgeEvaluator
{
    public async Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CorpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        IReadOnlyList<KnowledgeQuestion> questions = await repository.GetQuestionsAsync(request.CorpusId, cancellationToken);
        List<KnowledgeEvaluationItem> items = [];
        List<int> ranks = [];

        foreach (KnowledgeQuestion question in questions)
        {
            IReadOnlyList<KnowledgeHit> hits = await retriever.RetrieveAsync(new KnowledgeQuery { CorpusId = request.CorpusId, Question = question.Question, TenantId = request.TenantId, TopK = request.TopK }, cancellationToken);
            int rank = FirstExpectedRank(question.ExpectedChunkIds, hits);
            if (rank > 0) ranks.Add(rank);
            KnowledgeAnswer answer = await answerer.AnswerFromHitsAsync(new KnowledgeAskRequest { CorpusId = request.CorpusId, Question = question.Question, TenantId = request.TenantId, TopK = request.TopK }, hits, cancellationToken);
            items.Add(new KnowledgeEvaluationItem
            {
                QuestionId = question.Id,
                ExpectedContextFoundInTop5 = rank is > 0 and <= 5,
                ExpectedContextFoundInTop10 = rank is > 0 and <= 10,
                WasAnswered = answer.IsAnswered,
                ShouldHaveAnswered = !string.IsNullOrWhiteSpace(question.ExpectedAnswer),
                GeneratedAnswer = answer.Answer
            });
        }

        return new KnowledgeEvaluationRun(Guid.NewGuid().ToString("n"), request.CorpusId, items, Ratio(items, static item => item.ExpectedContextFoundInTop5), Ratio(items, static item => item.ExpectedContextFoundInTop10), ranks.Count == 0 ? 0 : ranks.Average());
    }

    private static int FirstExpectedRank(IReadOnlyList<string> expectedChunkIds, IReadOnlyList<KnowledgeHit> hits)
    {
        for (int i = 0; i < hits.Count; i++) if (expectedChunkIds.Contains(hits[i].ChunkId, StringComparer.Ordinal)) return i + 1;
        return 0;
    }

    private static double Ratio(IReadOnlyList<KnowledgeEvaluationItem> items, Func<KnowledgeEvaluationItem, bool> predicate) => items.Count == 0 ? 0 : items.Count(predicate) / (double)items.Count;
}
