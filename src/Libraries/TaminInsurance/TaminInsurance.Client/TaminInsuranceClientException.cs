using System.Net;

namespace Vitarish.TaminInsurance.Client;

public sealed class TaminInsuranceClientException : Exception
{
    public TaminInsuranceClientException(HttpStatusCode statusCode, string? responseBody)
        : base($"Tamin API request failed with HTTP {(int)statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }
    public string? ResponseBody { get; }
}
