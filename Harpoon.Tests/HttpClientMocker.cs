using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

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

        class QueryHandler : HttpMessageHandler
        {
            public string Parameter { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = HttpUtility.ParseQueryString(request.RequestUri.Query)[Parameter];
                return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(response) });
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

        public static HttpClient ReturnQueryParam(string queryParameter)
        {
            return new HttpClient(new QueryHandler { Parameter = queryParameter });
        }

        public static HttpClient AlwaysFail(Exception exception)
        {
            return new HttpClient(new Failer { Exception = exception });
        }
    }
}