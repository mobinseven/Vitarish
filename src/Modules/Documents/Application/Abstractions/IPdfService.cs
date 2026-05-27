namespace Vitarish.Documents.Application.Abstractions;

public interface IPdfService
{
    Task<Stream> ConvertOfficeToPdfAsync(IReadOnlyList<PdfDocument> documents, CancellationToken ct = default);
    Task<Stream> MergePdfsAsync(IReadOnlyList<PdfDocument> pdfs, CancellationToken ct = default);
}
