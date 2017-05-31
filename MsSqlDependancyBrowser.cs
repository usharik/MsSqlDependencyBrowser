using System;

namespace MsSqlDependancyBrowser
{
    class Program
    {
        static class MainClass
        {
            static void Main()
            {
                MsSqlDependancyBrowserHttpServer httpServer = new MsSqlDependancyBrowserHttpServer("http://localhost:8085/");
                httpServer.Start();
                Console.ReadKey();
                httpServer.Stop();
            }
        }
    }
}
