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

        [RequestMapping("/modalDialog.js", "GET")]
        public void handleModalDialogJsRequest(HttpListenerContext context)
        {
            sendStaticResource(context.Response, Resources.modalDialog_js, "application/javascript");
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
                    sendAnswerWithCode(context.Response, 406);
                    return;
                }
                connectionString = tmpConnectionString;
                jsonConnectionParams = tmpJsonConnectionParams;
                sendAnswerWithCode(context.Response, 200);
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
