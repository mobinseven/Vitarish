using System.Collections.Concurrent;
using Vitarish.Medical.Application.AgentCapabilities.Prescriptions;

namespace Vitarish.Medical.Infrastructure.AgentCapabilities.Prescriptions;

public sealed class InMemoryPrescriptionAgentReviewStore : IPrescriptionAgentReviewStore
{
    private readonly ConcurrentDictionary<Guid, PrescriptionDraftReview> _reviews = new();
    private readonly ConcurrentDictionary<Guid, PrescriptionConfirmationResult> _confirmations = new();
    private readonly ConcurrentDictionary<Guid, byte> _confirmationsInProgress = new();

    public Task<PrescriptionDraftReview?> GetAsync(
        Guid reviewId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_reviews.GetValueOrDefault(reviewId));

    public Task SaveAsync(
        PrescriptionDraftReview review,
        CancellationToken cancellationToken)
    {
        _reviews[review.ReviewId] = review;
        return Task.CompletedTask;
    }

    public Task<PrescriptionConfirmationResult?> GetConfirmationAsync(
        Guid reviewId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_confirmations.GetValueOrDefault(reviewId));

    public Task<bool> TryBeginConfirmationAsync(
        Guid reviewId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_confirmationsInProgress.TryAdd(reviewId, 0));

    public Task SaveConfirmationAsync(
        Guid reviewId,
        PrescriptionConfirmationResult result,
        CancellationToken cancellationToken)
    {
        _confirmations[reviewId] = result;
        return Task.CompletedTask;
    }
}
