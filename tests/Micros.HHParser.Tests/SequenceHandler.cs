using System.Net;

namespace Micros.HHParser.Tests;

public sealed class SequenceHandler(params HttpStatusCode[] codes) : HttpMessageHandler
{
    private readonly Queue<HttpStatusCode> _codes = new(codes);
    private HttpStatusCode _last = codes.Length > 0 ? codes[^1] : HttpStatusCode.OK;

    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Calls++;
        if (_codes.Count > 0)
            _last = _codes.Dequeue();
        return Task.FromResult(new HttpResponseMessage(_last));
    }
}
