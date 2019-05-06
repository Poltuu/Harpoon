using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Harpoon.Tests.Mocks
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

        class CallbackHandler : HttpMessageHandler
        {
            public Func<HttpRequestMessage, Task<HttpResponseMessage>> Callback { get; set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return (await Callback(request)) ?? new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        public static HttpClient Callback(Func<HttpRequestMessage, Task<HttpResponseMessage>> callback)
        {
            return new HttpClient(new CallbackHandler { Callback = callback });
        }

        public static HttpClient Callback(Func<HttpRequestMessage, HttpResponseMessage> callback)
        {
            return new HttpClient(new CallbackHandler { Callback = m => Task.FromResult(callback(m)) });
        }

        public static HttpClient Callback(Func<HttpRequestMessage, Task> callback)
        {
            return new HttpClient(new CallbackHandler { Callback = async m => { await callback(m); return null; } });
        }

        public static HttpClient Callback(Action<HttpRequestMessage> callback)
        {
            return new HttpClient(new CallbackHandler { Callback = m => { callback(m); return Task.FromResult<HttpResponseMessage>(null); } });
        }

        public static HttpClient Static(HttpStatusCode status, string content)
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