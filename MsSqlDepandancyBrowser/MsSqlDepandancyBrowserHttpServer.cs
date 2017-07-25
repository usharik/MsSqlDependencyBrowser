using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace MsSqlDepandancyBrowser
{
    class MsSqlDepandancyBrowserHttpServer : SimpleHttpServer
    {
        string jsonConnectionParams;
        MsSqlRequestService msSqlRequestService;

        public MsSqlDepandancyBrowserHttpServer(string url) : base(url)
        {
            msSqlRequestService = new MsSqlRequestService();
        }
        
        [RequestMapping("/main.css", "GET")]
        public void handleMainCssRequest(HttpListenerContext context)
        {
            sendStaticResource(context.Response, Resources.main_css, "text/css");
        }

        [RequestMapping("/postConnectionString.js", "GET")]
        public void handlePostConnectionStringJsRequest(HttpListenerContext context)
        {
            sendStaticResource(context.Response, Resources.postConnectionString_js, "application/javascript");
        }

        [RequestMapping("/serverObjectList.js", "GET")]
        public void handleServerObjectList(HttpListenerContext context)
        {
            if (!msSqlRequestService.isConnected())
            {
                sendStaticResource(context.Response, "", "text/html");
                return;
            }
            string result = "var allServerObjects = " + JArray.FromObject(msSqlRequestService.requestAllServerObjectList()).ToString() + ";\r\n" +
                            "var objectNameParam = '" + Resources.objectNameParam + "';";
            sendStaticResource(context.Response, result, "application/javascript");
        }

        [RequestMapping("/", "GET")]
        public void handleIndexPageRequest(HttpListenerContext context)
        {
            if (!msSqlRequestService.isConnected())
            {
                sendStaticResource(context.Response, string.Format(Resources.index_html, "Not connected", "", "SQL Server Not connected. Press 'Connect' button."), "text/html");
                return;
            }
            string result = "";
            string spName = context.Request.QueryString[Resources.objectNameParam];
            if (spName != null) {
                result = msSqlRequestService.requestDatabaseObjectInfo(url, spName);
            }
            sendStaticResource(context.Response, string.Format(Resources.index_html, spName, jsonConnectionParams, result), "text/html");
        }

        [RequestMapping("/objtext", "GET")]
        public void handleGetObjTextRequest(HttpListenerContext context)
        {
            if (!msSqlRequestService.isConnected())
            {
                sendStaticResource(context.Response, "SQL Server Not connected. Press 'Connect' button.", "text/html");
                return;
            }
            string result = "";
            string spName = context.Request.QueryString[Resources.objectNameParam];
            if (spName != null)
            {
                result = msSqlRequestService.requestDatabaseObjectInfo(url, spName);
            }
            sendStaticResource(context.Response, result, "text/html");
        }

        [RequestMapping("/connect", "POST")]
        public void handleConnectRequest(HttpListenerContext context)
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                var tmpJsonConnectionParams = reader.ReadToEnd();
                dynamic connParams = JObject.Parse(tmpJsonConnectionParams);
                var tmpConnectionString = string.Format(Resources.connectionStringTemplate, connParams.server, connParams.database);
                Console.WriteLine(tmpConnectionString);
                try
                {
                    msSqlRequestService.ConnectionString = tmpConnectionString;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    sendStaticResourceWithCode(context.Response, JObject.FromObject(new { errorMessage = ex.Message}).ToString(), "application/json", 406);
                    return;
                }                
                jsonConnectionParams = tmpJsonConnectionParams;
                sendAnswerWithCode(context.Response, 200);
            }
        }

        [RequestMapping("/databaselist", "POST")]
        public void handleDatabaseListRequest(HttpListenerContext context)
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                var tmpJsonConnectionParams = reader.ReadToEnd();
                dynamic connParams = JObject.Parse(tmpJsonConnectionParams);
                try
                {
                    List<string> databaseList = msSqlRequestService.requestDatabaseList(string.Format(Resources.connectionStringTemplate, connParams.server, "master"));
                    sendStaticResource(context.Response, JArray.FromObject(databaseList).ToString(), "application/javascript");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    sendStaticResourceWithCode(context.Response, JObject.FromObject(new { errorMessage = ex.Message }).ToString(), "application/json", 406);
                }
            }
        }
    }
}
