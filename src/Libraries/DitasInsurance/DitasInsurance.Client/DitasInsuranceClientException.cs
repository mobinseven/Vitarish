using System.Net;

namespace Vitarish.DitasInsurance.Client;

public sealed class DitasInsuranceClientException : Exception
{
    public DitasInsuranceClientException(HttpStatusCode statusCode, string? responseBody)
        : base($"DITAS API request failed with HTTP {(int)statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }
    public string? ResponseBody { get; }
}
