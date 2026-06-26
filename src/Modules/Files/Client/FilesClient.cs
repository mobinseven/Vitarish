using System.Net;
using Vitarish.Files.Contracts.Errors;
using Vitarish.Files.Contracts.Metadata;
using Vitarish.Files.Contracts.Requests;
using Vitarish.Files.Contracts.Responses;
using Vitarish.Files.Contracts.Results;
using Vitarish.Files.Contracts.Routes;
using Vitarish.Modules.Base.Client;

namespace Vitarish.Files.Client;

public sealed class FilesClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), IFilesClient
{
    public async Task<FilesResult<FileInformationDto>> GetFileInformationAsync(Guid fileId, CancellationToken cancellationToken) =>
        MapResult(await GetJsonAsync<FileInformationDto>($"{FilesApiRoutes.Base}/{fileId}", cancellationToken).ConfigureAwait(false));

    public async Task<FilesResult<IReadOnlyList<FileInformationDto>>> FindFilesAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken) =>
        MapResult(await PostJsonAsync<ListFilesRequest, IReadOnlyList<FileInformationDto>>(FilesApiRoutes.Base + "/search", new ListFilesRequest(descriptor), cancellationToken).ConfigureAwait(false));

    public async Task<FilesResult<FileInformationDto>> CreateFileRecordAsync(FileInformationWriteRequest request, CancellationToken cancellationToken) =>
        MapResult(await PostJsonAsync<CreateFileRecordRequest, FileInformationDto>(FilesApiRoutes.Base, new CreateFileRecordRequest(request), cancellationToken).ConfigureAwait(false));

    public async Task<FilesResult<FileUploadResponse>> UploadFileAsync(FileUploadRequest request, Stream content, CancellationToken cancellationToken)
    {
        using MultipartFormDataContent form = new();
        form.Add(new StringContent(request.FileName), nameof(FileUploadRequest.FileName));
        form.Add(new StringContent(request.ContentType), nameof(FileUploadRequest.ContentType));
        form.Add(new StringContent(request.Size.ToString(System.Globalization.CultureInfo.InvariantCulture)), nameof(FileUploadRequest.Size));
        form.Add(new StringContent(request.Checksum), nameof(FileUploadRequest.Checksum));
        if (!string.IsNullOrWhiteSpace(request.Thumbnail)) form.Add(new StringContent(request.Thumbnail), nameof(FileUploadRequest.Thumbnail));
        if (!string.IsNullOrWhiteSpace(request.Tag)) form.Add(new StringContent(request.Tag), nameof(FileUploadRequest.Tag));
        StreamContent fileContent = new(content);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType);
        form.Add(fileContent, "content", request.FileName);

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, FilesApiRoutes.Base + FilesApiRoutes.Upload) { Content = form };
        ModuleApiClientResult<HttpResponseMessage> sent = await SendRawAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        if (!sent.IsSuccess) return FilesResult<FileUploadResponse>.Failed(MapFailure(sent.Failure!));
        using HttpResponseMessage response = sent.Value!;
        return response.IsSuccessStatusCode
            ? MapResult(await ReadResponseJsonAsync<FileUploadResponse>(response, cancellationToken).ConfigureAwait(false))
            : FilesResult<FileUploadResponse>.Failed(MapFailure(await CreateFailureAsync(response, cancellationToken).ConfigureAwait(false)));
    }

    public async Task<FilesResult<Stream>> DownloadFileAsync(Guid fileId, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"{FilesApiRoutes.Base}/{fileId}/content");
        ModuleApiClientResult<HttpResponseMessage> sent = await SendRawAsync(request, cancellationToken).ConfigureAwait(false);
        if (!sent.IsSuccess) return FilesResult<Stream>.Failed(MapFailure(sent.Failure!));

        HttpResponseMessage response = sent.Value!;
        if (response.IsSuccessStatusCode)
        {
            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return FilesResult<Stream>.Success(new HttpResponseStream(stream, response));
        }

        FilesError error = MapFailure(await CreateFailureAsync(response, cancellationToken).ConfigureAwait(false));
        response.Dispose();
        return FilesResult<Stream>.Failed(error);
    }

    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) =>
        new(statusCode.ToString(), message, statusCodeValue);

    private static FilesResult<T> MapResult<T>(ModuleApiClientResult<T> result) =>
        result.IsSuccess ? FilesResult<T>.Success(result.Value!) : FilesResult<T>.Failed(MapFailure(result.Failure!));

    private static FilesError MapFailure(ModuleApiClientFailure failure) =>
        new(MapCode(failure), failure.Message, failure.StatusCode);

    private static FilesErrorCode MapCode(ModuleApiClientFailure failure) => failure.Code switch
    {
        "Canceled" => FilesErrorCode.Canceled,
        "Timeout" => FilesErrorCode.Timeout,
        "Transport" => FilesErrorCode.Transport,
        "Deserialization" => FilesErrorCode.Deserialization,
        nameof(HttpStatusCode.BadRequest) => FilesErrorCode.Validation,
        nameof(HttpStatusCode.Unauthorized) => FilesErrorCode.Unauthorized,
        nameof(HttpStatusCode.Forbidden) => FilesErrorCode.Forbidden,
        nameof(HttpStatusCode.NotFound) => FilesErrorCode.NotFound,
        nameof(HttpStatusCode.Conflict) => FilesErrorCode.Conflict,
        _ => FilesErrorCode.Unexpected
    };

    private sealed class HttpResponseStream(Stream innerStream, HttpResponseMessage response) : Stream
    {
        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => innerStream.CanSeek;
        public override bool CanWrite => innerStream.CanWrite;
        public override long Length => innerStream.Length;
        public override long Position { get => innerStream.Position; set => innerStream.Position = value; }
        public override void Flush() => innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);
        public override void SetLength(long value) => innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => innerStream.Write(buffer, offset, count);
        protected override void Dispose(bool disposing) { if (disposing) { innerStream.Dispose(); response.Dispose(); } base.Dispose(disposing); }
        public override async ValueTask DisposeAsync() { await innerStream.DisposeAsync().ConfigureAwait(false); response.Dispose(); await base.DisposeAsync().ConfigureAwait(false); }
    }
}
