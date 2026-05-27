using Gotenberg.Sharp.API.Client;
using Gotenberg.Sharp.API.Client.Application.Builders;
using Vitarish.Documents.Application.Abstractions;

namespace Vitarish.Documents.Infrastructure.Services;

[RegisterScoped]
public sealed class GotenbergPdfService(GotenbergSharpClient client) : IPdfService
{
    public async Task<Stream> ConvertOfficeToPdfAsync(IReadOnlyList<PdfDocument> documents, CancellationToken ct = default)
    {
        MergeOfficeBuilder builder = new MergeOfficeBuilder()
            .WithAssets(assets =>
            {
                foreach (PdfDocument document in documents)
                {
                    assets.AddItem(document.FileName, document.Content);
                }
            });
        return await client.MergeOfficeDocsAsync(builder, ct);
    }

    public async Task<Stream> MergePdfsAsync(IReadOnlyList<PdfDocument> pdfs, CancellationToken ct = default)
    {
        MergeBuilder builder = new MergeBuilder()
            .WithAssets(assets =>
            {
                foreach (PdfDocument pdf in pdfs)
                {
                    assets.AddItem(pdf.FileName, pdf.Content);
                }
            });
        return await client.MergePdfsAsync(builder.Build(), ct);
    }
}
