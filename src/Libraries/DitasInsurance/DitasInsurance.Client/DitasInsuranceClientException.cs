using System.Net;

namespace Vitarish.DitasInsurance.Client;

public sealed class DitasInsuranceClientException(HttpStatusCode statusCode, string? responseBody) : Exception($"DITAS API request failed with HTTP {(int)statusCode}.")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? ResponseBody { get; } = responseBody;
}
