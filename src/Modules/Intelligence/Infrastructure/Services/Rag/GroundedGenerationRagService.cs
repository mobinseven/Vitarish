using System.Text;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Infrastructure.Services.Rag;

public sealed class GroundedGenerationRagService(
    IRagRetrievalService retrievalService,
    IResponder responder) : IRagGroundedGenerationService
{
    public async Task<RagGroundedGenerationResult> GenerateAsync(
        RagGroundedGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new RagGroundedGenerationResult
            {
                Answer = string.Empty,
                IsGrounded = false,
                Citations = []
            };
        }

        int maxDocuments = request.MaxDocuments > 0 ? request.MaxDocuments : 3;

        RagRetrieveResult retrieveResult = await retrievalService
            .RetrieveAsync(new RagRetrieveRequest
            {
                Query = request.Query,
                MaxResults = maxDocuments
            }, cancellationToken)
            .ConfigureAwait(false);

        if (retrieveResult.Documents.Count == 0)
        {
            return new RagGroundedGenerationResult
            {
                Answer = "No grounding documents were found for this query.",
                IsGrounded = false,
                Citations = []
            };
        }

        StringBuilder contextBuilder = new();
        foreach (RagRetrievedDocument document in retrieveResult.Documents)
        {
            _ = contextBuilder.AppendLine($"Source: {document.Source}");
            _ = contextBuilder.AppendLine(document.Content);
            _ = contextBuilder.AppendLine();
        }

        string prompt = $"""
Use only the following retrieved context to answer the query.
If the context is insufficient, explicitly say so.

Context:
{contextBuilder}

Query:
{request.Query}
""";

        IReadOnlyList<ResponderOutput> response = await responder
            .GenerateAsync([ResponderInput.TextInput(prompt)], new ResponderOptions
            {
                Model = request.Model,
                Instructions = request.Instructions
            }, cancellationToken)
            .ConfigureAwait(false);

        string answer = string.Join("\n", response
            .Where(x => x.Type == ResponderOutputType.Text && !string.IsNullOrWhiteSpace(x.Text))
            .Select(x => x.Text));

        return new RagGroundedGenerationResult
        {
            Answer = string.IsNullOrWhiteSpace(answer)
                ? "Grounded response is unavailable."
                : answer,
            IsGrounded = true,
            Citations = [..
                retrieveResult.Documents.Select(x => new RagGroundedCitation
                {
                    DocumentId = x.Id,
                    Source = x.Source,
                    Score = x.Score
                })]
        };
    }
}
