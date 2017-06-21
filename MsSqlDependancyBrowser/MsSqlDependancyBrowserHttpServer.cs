using Newtonsoft.Json.Linq;
using SqlScriptParser;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

namespace MsSqlDependancyBrowser
{
    class MsSqlDependancyBrowserHttpServer : SimpleHttpServer
    {
        static HashSet<string> keywords;
        static XslCompiledTransform xslTranCompiler;
        const string objectNameParam = "sp";
        const string connectionStringTemplate = @"Data Source={0};Initial Catalog={1};Integrated Security=True";
        static string connectionString;
        static string jsonConnectionParams;

        public MsSqlDependancyBrowserHttpServer(string url) : base(url)
        {
            keywords = new HashSet<string>(Resources.keywords.Split(' '));
            xslTranCompiler = new XslCompiledTransform();
            var xslDoc = new XmlDocument();
            xslDoc.LoadXml(Resources.table2html_xslt);
            xslTranCompiler.Load(xslDoc);
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
            if (connectionString == null)
            {
                sendStaticResource(context.Response, "", "text/html");
                return;
            }
            string result = "";
            using (var sqlConn = new SqlConnection(connectionString))
            {
                var sqlCmd = new SqlCommand(Resources.queryAllObjects_sql, sqlConn);
                sqlConn.Open();
                List<object> allServerObjects = new List<object>();
                using (SqlDataReader dr = sqlCmd.ExecuteReader())
                {               
                    do
                    {
                        string curr_type_desc = "";
                        List<string> serverObjects = new List<string>();
                        while (dr.Read())
                        {
                            curr_type_desc = dr.GetString(0);
                            serverObjects.Add(dr.GetString(1));
                        }
                        if (serverObjects.Count > 0)
                        {
                            allServerObjects.Add(new { type_desc = curr_type_desc, objects = serverObjects });
                        }
                    } while (dr.NextResult());
                }
                result = "var allServerObjects = " + JArray.FromObject(allServerObjects).ToString() + ";\r\n" +
                    "var objectNameParam = '" + objectNameParam + "';";
            }
            sendStaticResource(context.Response, result, "application/javascript");
        }

        [RequestMapping("/", "GET")]
        public void handleIndexPageRequest(HttpListenerContext context)
        {
            if (connectionString == null)
            {
                sendStaticResource(context.Response, string.Format(Resources.index_html, "Not connected", "", "SQL Server Not connected. Press 'Connect' button."), "text/html");
                return;
            }
            string result = "";
            string spName = context.Request.QueryString[objectNameParam];
            if (spName != null) {
                result = requestDatabase(spName);
            }
            sendStaticResource(context.Response, string.Format(Resources.index_html, spName, jsonConnectionParams, result), "text/html");
        }

        [RequestMapping("/objtext", "GET")]
        public void handleGetObjTextRequest(HttpListenerContext context)
        {
            if (connectionString == null)
            {
                sendStaticResource(context.Response, "SQL Server Not connected. Press 'Connect' button.", "text/html");
                return;
            }
            string result = "";
            string spName = context.Request.QueryString[objectNameParam];
            if (spName != null)
            {
                result = requestDatabase(spName);
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
                var tmpConnectionString = string.Format(connectionStringTemplate, connParams.server, connParams.database);
                Console.WriteLine(tmpConnectionString);
                try
                {
                    using (var sqlConn = new SqlConnection(tmpConnectionString)) sqlConn.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    sendStaticResourceWithCode(context.Response, JObject.FromObject(new { errorMessage = ex.Message}).ToString(), "application/json", 406);
                    return;
                }
                connectionString = tmpConnectionString;
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
                var tmpConnectionString = string.Format(connectionStringTemplate, connParams.server, "master");
                try
                {
                    List<string> databaseList = new List<string>();
                    using (var sqlConn = new SqlConnection(tmpConnectionString))
                    {
                        var sqlCmd = new SqlCommand(Resources.queryDatabaseList_sql, sqlConn);
                        sqlConn.Open();
                        using (SqlDataReader dr = sqlCmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                databaseList.Add(dr.GetString(0));
                            }
                        }
                        sendStaticResource(context.Response, JArray.FromObject(databaseList).ToString(), "application/javascript");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    sendStaticResourceWithCode(context.Response, JObject.FromObject(new { errorMessage = ex.Message }).ToString(), "application/json", 406);
                    return;
                }
            }
        }

        string requestDatabase(string spName)
        {
            string queryQbjectInfo = Resources.queryObjectInfo_sql;
            string queryObjectDependancies = Resources.queryObjectDependancies_sql;
            string queryTableXml = Resources.queryTableXml_sql;

            try
            {
                using (var sqlConn = new SqlConnection(connectionString))
                {
                    var sqlCmd = new SqlCommand(queryQbjectInfo, sqlConn);
                    sqlConn.Open();
                    sqlCmd.Parameters.Add("@objectName", SqlDbType.NVarChar);
                    sqlCmd.Parameters["@objectName"].Value = spName;

                    string objectFullName = "";
                    string object_text = "";
                    string type_desc = "";
                    using (SqlDataReader dr = sqlCmd.ExecuteReader())
                    {
                        if (dr.HasRows && dr.Read())
                        {
                            object_text = dr.IsDBNull(0) ? "" : dr.GetString(0);
                            objectFullName = dr.GetString(1);
                            type_desc = dr.GetString(2);
                        }
                    }

                    if (type_desc == "USER_TABLE")
                    {
                        sqlCmd = new SqlCommand(queryTableXml, sqlConn);
                        sqlCmd.Parameters.Add("@objectName", SqlDbType.NVarChar);
                        sqlCmd.Parameters["@objectName"].Value = spName;
                        var xmlSource = new XmlDocument();
                        using (XmlReader dr = sqlCmd.ExecuteXmlReader())
                        {
                            if (dr.Read())
                            {
                                xmlSource.Load(dr);
                            }
                        }
                        var htmlDest = new StringBuilder();
                        xslTranCompiler.Transform(xmlSource, XmlWriter.Create(htmlDest));
                        return htmlDest.ToString();
                    }

                    if (objectFullName != "")
                    {
                        sqlCmd = new SqlCommand(queryObjectDependancies, sqlConn);
                        sqlCmd.Parameters.Add("@objectFullName", SqlDbType.NVarChar);
                        sqlCmd.Parameters["@objectFullName"].Value = objectFullName;

                        var depList = new Dictionary<string, string>();
                        using (SqlDataReader dr = sqlCmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                string depName = dr.GetString(0);
                                string typeDesc = dr.IsDBNull(1) ? "UNKNOWN_OBJECT" : dr.GetString(1);
                                typeDesc += dr.IsDBNull(2) ? "" : $": {dr.GetString(2)}";
                                depList[depName.ToLower()] = $"<a href='{url}?{objectNameParam}={depName}' title='{typeDesc}'>{depName}<a>";
                            }
                        }

                        var wordProcessor = new WordProcessor(keywords, depList);
                        var singleCommentProcessor = new BlockProcessor(wordProcessor, @"--.*[\r\n]", "green");
                        var commentAndStringProcessor = new CommentAndStringProcessor(singleCommentProcessor);
                        return commentAndStringProcessor.Process(object_text);
                    }
                    return "object not exists";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
                return $"Exception: {ex}";
            }
        }
    }
}
