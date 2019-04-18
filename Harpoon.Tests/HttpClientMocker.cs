using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Tests
{
    public class HttpClientMocker
    {
        class MoqHandler : HttpMessageHandler
        {
            public HttpStatusCode Status { get; set; }
            public string Content { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage { StatusCode = Status, Content = new StringContent(Content) });
            }
        }
        class Failer : HttpMessageHandler
        {
            public Exception Exception { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromException<HttpResponseMessage>(Exception);
            }
        }

        public static HttpClient Get(HttpStatusCode status, string content)
        {
            return new HttpClient(new MoqHandler { Status = status, Content = content });
        }

        public static HttpClient AlwaysFail(Exception exception)
        {
            return new HttpClient(new Failer { Exception = exception });
        }
    }
}