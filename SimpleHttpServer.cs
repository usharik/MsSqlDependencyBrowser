using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MsSqlDependancyBrowser
{
    class SimpleHttpServer
    {
        protected readonly string url;
        private Dictionary<string, MethodInfo> httpRequestHandlers;
        private HttpListener httpListner;

        public SimpleHttpServer(string url)
        {
            this.url = url;
            httpListner = new HttpListener();
            httpListner.Prefixes.Add(url);

            httpRequestHandlers = GetType()
                .GetMethods()
                .Where(y => y.GetCustomAttributes(false).OfType<RequestMapping>().Any())
                .ToDictionary(y => y.GetCustomAttributes(false).OfType<RequestMapping>().First().ToString());
        }

        public void Start()
        {
            Console.WriteLine($"Listening {url}");
            httpListner.Start();
            Task.Factory.StartNew(Listner);
        }

        public void Stop()
        {
            httpListner.Stop();
        }

        private async void Listner()
        {
            for (;;)
            {
                var context = await httpListner.GetContextAsync();
                Task.Factory.StartNew(() => processHttpRequest(context));
            }
        }

        private void processHttpRequest(HttpListenerContext context)
        {
            var response = context.Response;
            Console.WriteLine($"Thread ID {Thread.CurrentThread.ManagedThreadId} start");
            Console.WriteLine(context.Request.RawUrl);
            Console.WriteLine(context.Request.HttpMethod);

            MethodInfo handler;
            string requestHash = $"{context.Request.Url.AbsolutePath} - {context.Request.HttpMethod}";
            if (httpRequestHandlers.TryGetValue(requestHash, out handler))
            {
                try
                {
                    handler.Invoke(this, new object[]{context});
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                }
            }
            else
            {
                sendAnswerWithCode(context.Response, 404);
            }
            Console.WriteLine($"Thread ID {Thread.CurrentThread.ManagedThreadId} stop");
        }

        protected static void sendStaticResource(HttpListenerResponse response, string resourceText, string mime)
        {
            var buffer = Encoding.UTF8.GetBytes(resourceText);
            response.ContentLength64 = buffer.Length;
            response.Headers.Add("Content-Type", mime);
            var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        protected static void sendAnswerWithCode(HttpListenerResponse response, int statusCode)
        {
            response.Headers.Clear();
            response.SendChunked = false;
            response.StatusCode = statusCode;
            response.Headers.Add("Server", String.Empty);
            response.Headers.Add("Date", String.Empty);
            response.Close();
            return;
        }
    }
}
