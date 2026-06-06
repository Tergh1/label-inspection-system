using System.Net;

namespace client.Tests.TestSupport;

internal sealed class FakeHttpMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    public Exception? ExceptionToThrow { get; init; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri?.ToString() ?? string.Empty,
            body,
            request.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray())));

        return new HttpResponseMessage(statusCode);
    }
}

internal sealed record RecordedRequest(
    HttpMethod Method,
    string Uri,
    string? Body,
    IReadOnlyDictionary<string, string[]> Headers);
