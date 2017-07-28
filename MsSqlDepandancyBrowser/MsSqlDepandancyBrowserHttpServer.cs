using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace MsSqlDepandancyBrowser
{

    class MsSqlDepandancyBrowserHttpServer : SimpleHttpServer
    {
        class ConnectionDto
        {
            public string server;
            public string database;
        }

        public MsSqlDepandancyBrowserHttpServer(string url) : base(url)
        {            
        }
        
        [RequestMapping("/main.css", "GET")]
        public void handleMainCssRequest(HttpListenerContext context)
        {
            sendStaticResource(context.Response, Resources.main_css, "text/css");
        }

        [RequestMapping("/objectText.html", "GET")]
        public void handleObjectTextRequest(HttpListenerContext context)
        {
            sendStaticResource(context.Response, Resources.objectText_html, "text/css");
        }

        [RequestMapping("/postConnectionString.js", "GET")]
        public void handlePostConnectionStringJsRequest(HttpListenerContext context)
        {
            sendStaticResource(context.Response, Resources.postConnectionString_js, "application/javascript");
        }

        [RequestMapping("/serverobjectlist", "POST")]
        public void handleServerObjectList(HttpListenerContext context)
        {
            try
            {
                ConnectionDto connParams = readResponseAsJson<ConnectionDto>(context.Request);
                MsSqlRequestService msSqlRequestService = new MsSqlRequestService(connParams.server.ToString(), connParams.database.ToString());
                string result = JArray.FromObject(msSqlRequestService.requestAllServerObjectList()).ToString();
                sendStaticResource(context.Response, result, "application/javascript");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                sendStaticResourceWithCode(context.Response, JObject.FromObject(new { errorMessage = ex.Message }).ToString(), "application/json", 406);
                return;
            }
        }

        [RequestMapping("/", "GET")]
        public void handleIndexPageRequest(HttpListenerContext context)
        {
            sendStaticResource(context.Response, Resources.index_html, "text/html");
        }

        [RequestMapping("/objtext", "POST")]
        public void handleGetObjTextRequest(HttpListenerContext context)
        {
            try
            {
                ConnectionDto connParams = readResponseAsJson<ConnectionDto>(context.Request);
                MsSqlRequestService msSqlRequestService = new MsSqlRequestService(connParams.server, connParams.database);
                string result = "";
                string spName = context.Request.QueryString[Resources.objectNameParam];
                if (spName != null)
                {
                    result = msSqlRequestService.requestDatabaseObjectInfo(url, spName);
                }
                sendStaticResource(context.Response, result, "text/html");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                sendStaticResourceWithCode(context.Response, JObject.FromObject(new { errorMessage = ex.Message }).ToString(), "application/json", 406);
                return;
            }
        }

        [RequestMapping("/testconnect", "POST")]
        public void handleConnectRequest(HttpListenerContext context)
        {
            try
            {
                ConnectionDto connParams = readResponseAsJson<ConnectionDto>(context.Request);
                MsSqlRequestService msSqlRequestService = new MsSqlRequestService(connParams.server, connParams.database);
                sendAnswerWithCode(context.Response, 200);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                sendStaticResourceWithCode(context.Response, JObject.FromObject(new { errorMessage = ex.Message}).ToString(), "application/json", 406);
                return;
            }
        }

        [RequestMapping("/databaselist", "POST")]
        public void handleDatabaseListRequest(HttpListenerContext context)
        {            
            try
            {
                ConnectionDto connParams = readResponseAsJson<ConnectionDto>(context.Request);
                MsSqlRequestService msSqlRequestService = new MsSqlRequestService(connParams.server, "master");
                List<string> databaseList = msSqlRequestService.requestDatabaseList(string.Format(Resources.connectionStringTemplate, connParams.server, "master"));
                sendStaticResource(context.Response, JArray.FromObject(databaseList).ToString(), "application/javascript");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                sendStaticResourceWithCode(context.Response, JObject.FromObject(new { errorMessage = ex.Message }).ToString(), "application/json", 406);
            }
        }

        T readResponseAsJson<T>(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                var tmp = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(tmp);
            }
        }
    }
}
