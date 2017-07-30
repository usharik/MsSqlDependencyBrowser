using System;

namespace MsSqlDependencyBrowser
{
    class Program
    {
        static class MainClass
        {
            static void Main()
            {
                MsSqlDependencyBrowserHttpServer httpServer = new MsSqlDependencyBrowserHttpServer("http://localhost:8085/");
                httpServer.Start();
                Console.ReadKey();
                httpServer.Stop();
            }
        }
    }
}
