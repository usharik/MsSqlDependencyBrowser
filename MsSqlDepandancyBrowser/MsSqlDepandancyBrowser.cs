using System;

namespace MsSqlDepandancyBrowser
{
    class Program
    {
        static class MainClass
        {
            static void Main()
            {
                MsSqlDepandancyBrowserHttpServer httpServer = new MsSqlDepandancyBrowserHttpServer("http://localhost:8085/");
                httpServer.Start();
                Console.ReadKey();
                httpServer.Stop();
            }
        }
    }
}
