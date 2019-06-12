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
        public class MoqHandler : DelegatingHandler
        {
            public HttpStatusCode Status { get; set; }
            public string Content { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage { StatusCode = Status, Content = new StringContent(Content) });
        }

        public class QueryHandler : DelegatingHandler
        {
            public string Parameter { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = HttpUtility.ParseQueryString(request.RequestUri.Query)[Parameter];
                return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(response) });
            }
        }

        public class Failer : DelegatingHandler
        {
            public Exception Exception { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromException<HttpResponseMessage>(Exception);
        }

        public class CallbackHandler : DelegatingHandler
        {
            public Func<HttpRequestMessage, Task<HttpResponseMessage>> Callback { get; set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => (await Callback(request)) ?? new HttpResponseMessage(HttpStatusCode.OK);
        }

        public class CounterHandler : DelegatingHandler
        {
            private int _counter;
            public int Counter => _counter;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _counter);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }

        public static HttpClient Callback(Func<HttpRequestMessage, Task<HttpResponseMessage>> callback)
            => new HttpClient(new CallbackHandler { Callback = callback });

        public static HttpClient Callback(Func<HttpRequestMessage, HttpResponseMessage> callback)
            => new HttpClient(new CallbackHandler { Callback = m => Task.FromResult(callback(m)) });

        public static HttpClient Callback(Func<HttpRequestMessage, Task> callback)
            => new HttpClient(new CallbackHandler { Callback = async m => { await callback(m); return null; } });

        public static HttpClient Callback(Action<HttpRequestMessage> callback)
            => new HttpClient(new CallbackHandler { Callback = m => { callback(m); return Task.FromResult<HttpResponseMessage>(null); } });

        public static HttpClient Static(HttpStatusCode status, string content)
            => new HttpClient(new MoqHandler { Status = status, Content = content });

        public static HttpClient ReturnQueryParam(string queryParameter)
            => new HttpClient(new QueryHandler { Parameter = queryParameter });

        public static HttpClient AlwaysFail(Exception exception)
            => new HttpClient(new Failer { Exception = exception });
    }
}