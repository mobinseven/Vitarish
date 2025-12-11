namespace Vitarish.Instagram.Scraper.Tests;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses;

    public StubHttpMessageHandler(params HttpResponseMessage[] responses)
    {
        _responses = new Queue<HttpResponseMessage>(responses);
    }

    public IList<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

    public HttpClient CreateClient()
    {
        return new HttpClient(this)
        {
            BaseAddress = new Uri("https://api.apify.com"),
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (_responses.Count == 0)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = request,
            });
        }

        var response = _responses.Dequeue();
        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}
