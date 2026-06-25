using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vitarish.Files.Contracts.Errors;
using Vitarish.Files.Contracts.Metadata;
using Vitarish.Files.Contracts.Requests;
using Vitarish.Files.Contracts.Responses;
using Vitarish.Files.Contracts.Results;
using Vitarish.Files.Contracts.Routes;

namespace Vitarish.Files.Client;

public sealed class FilesClient(HttpClient httpClient) : IFilesClient
{
    public async Task<FilesResult<FileInformationDto>> GetFileInformationAsync(Guid fileId, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync($"{FilesApiRoutes.Base}/{fileId}", cancellationToken).ConfigureAwait(false);
            return await ReadJsonResultAsync<FileInformationDto>(response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return FilesResult<FileInformationDto>.Failed(CreateCancellationError(cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            return FilesResult<FileInformationDto>.Failed(new FilesError(FilesErrorCode.Transport, ex.Message));
        }
    }

    public async Task<FilesResult<IReadOnlyList<FileInformationDto>>> FindFilesAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(FilesApiRoutes.Base + "/search", new ListFilesRequest(descriptor), cancellationToken).ConfigureAwait(false);
            return await ReadJsonResultAsync<IReadOnlyList<FileInformationDto>>(response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return FilesResult<IReadOnlyList<FileInformationDto>>.Failed(CreateCancellationError(cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            return FilesResult<IReadOnlyList<FileInformationDto>>.Failed(new FilesError(FilesErrorCode.Transport, ex.Message));
        }
    }

    public async Task<FilesResult<FileInformationDto>> CreateFileRecordAsync(FileInformationWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(FilesApiRoutes.Base, new CreateFileRecordRequest(request), cancellationToken).ConfigureAwait(false);
            return await ReadJsonResultAsync<FileInformationDto>(response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return FilesResult<FileInformationDto>.Failed(CreateCancellationError(cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            return FilesResult<FileInformationDto>.Failed(new FilesError(FilesErrorCode.Transport, ex.Message));
        }
    }

    public async Task<FilesResult<FileUploadResponse>> UploadFileAsync(FileUploadRequest request, Stream content, CancellationToken cancellationToken)
    {
        try
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

            using HttpResponseMessage response = await httpClient.PostAsync(FilesApiRoutes.Base + FilesApiRoutes.Upload, form, cancellationToken).ConfigureAwait(false);
            return await ReadJsonResultAsync<FileUploadResponse>(response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return FilesResult<FileUploadResponse>.Failed(CreateCancellationError(cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException or IOException)
        {
            return FilesResult<FileUploadResponse>.Failed(new FilesError(FilesErrorCode.Transport, ex.Message));
        }
    }

    public async Task<FilesResult<Stream>> DownloadFileAsync(Guid fileId, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync($"{FilesApiRoutes.Base}/{fileId}/content", HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                return FilesResult<Stream>.Success(new HttpResponseStream(stream, response));
            }

            FilesError error = await CreateErrorAsync(response, cancellationToken).ConfigureAwait(false);
            response.Dispose();
            return FilesResult<Stream>.Failed(error);
        }
        catch (OperationCanceledException)
        {
            return FilesResult<Stream>.Failed(CreateCancellationError(cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException or IOException)
        {
            return FilesResult<Stream>.Failed(new FilesError(FilesErrorCode.Transport, ex.Message));
        }
    }

    private static async Task<FilesResult<TResponse>> ReadJsonResultAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            TResponse? value = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken).ConfigureAwait(false);
            return value is null
                ? FilesResult<TResponse>.Failed(new FilesError(FilesErrorCode.Deserialization, "Files API returned an empty response.", (int)response.StatusCode))
                : FilesResult<TResponse>.Success(value);
        }

        return FilesResult<TResponse>.Failed(await CreateErrorAsync(response, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<FilesError> CreateErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            FilesError? error = await response.Content.ReadFromJsonAsync<FilesError>(cancellationToken).ConfigureAwait(false);
            if (error is not null) return error;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException) { }

        FilesErrorCode code = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => FilesErrorCode.Validation,
            HttpStatusCode.Unauthorized => FilesErrorCode.Unauthorized,
            HttpStatusCode.Forbidden => FilesErrorCode.Forbidden,
            HttpStatusCode.NotFound => FilesErrorCode.NotFound,
            HttpStatusCode.Conflict => FilesErrorCode.Conflict,
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => FilesErrorCode.Timeout,
            _ => FilesErrorCode.Unexpected
        };
        return new FilesError(code, "Files API request failed.", (int)response.StatusCode);
    }

    public async Task<FilesResult<Stream>> DownloadFileAsync(Guid fileId, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync($"{FilesApiRoutes.Base}/{fileId}/content", HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                return FilesResult<Stream>.Success(new HttpResponseStream(stream, response));
            }

            FilesError error = await CreateErrorAsync(response, cancellationToken).ConfigureAwait(false);
            response.Dispose();
            return FilesResult<Stream>.Failed(error);
        }
        catch (OperationCanceledException)
        {
            return FilesResult<Stream>.Failed(CreateCancellationError(cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException or IOException)
        {
            return FilesResult<Stream>.Failed(new FilesError(FilesErrorCode.Transport, ex.Message));
        }
    }

    private static FilesError CreateCancellationError(CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested
        ? new FilesError(FilesErrorCode.Canceled, "Files request was canceled.")
        : new FilesError(FilesErrorCode.Timeout, "Files request timed out.");

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
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                innerStream.Dispose();
                response.Dispose();
            }
            base.Dispose(disposing);
        }
        public override async ValueTask DisposeAsync()
        {
            await innerStream.DisposeAsync().ConfigureAwait(false);
            response.Dispose();
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
