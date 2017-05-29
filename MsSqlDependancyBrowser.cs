using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using SqlScriptParser;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace MsSqlDependancyBrowser
{
    class Program
    {
        static class MainClass
        {
            delegate void HttpRequestHandler(HttpListenerContext context);
            static HashSet<string> keywords;
            static XslCompiledTransform xslTranCompiler;
            const string objectNameParam = "sp";
            const string clientUrl = "http://localhost:8085/";
            static string connectionString = @"Data Source=PC;Initial Catalog=test;Integrated Security=True";
            static Dictionary<string, HttpRequestHandler> url2requestHandlerMapping = new Dictionary<string, HttpRequestHandler>();

            static void Main()
            {
                keywords = new HashSet<string>(Resources.keywords.Split(' '));

                xslTranCompiler = new XslCompiledTransform();
                var xslDoc = new XmlDocument();
                xslDoc.LoadXml(Resources.table2html_xslt);
                xslTranCompiler.Load(xslDoc);

                url2requestHandlerMapping.Add("/", handleIndexPageRequest);
                url2requestHandlerMapping.Add("/connect", handleConnectRequest);
                url2requestHandlerMapping.Add("/main.css", (context) => sendStaticResource(context.Response, Resources.main_css));
                url2requestHandlerMapping.Add("/postConnectionString.js", (context) => sendStaticResource(context.Response, Resources.postConnectionString_js));

                var web = new HttpListener();
                web.Prefixes.Add(clientUrl);
                Console.WriteLine("Listening..");
                web.Start();
                Listner(web);
                Console.ReadKey();
                web.Stop();
            }

            static async void Listner(HttpListener web)
            {
                for (;;)
                {
                    var context = await web.GetContextAsync();
                    Task.Factory.StartNew(() => processHttpRequest(context));
                }
            }

            static void processHttpRequest(HttpListenerContext context)
            {
                var response = context.Response;
                Console.WriteLine($"Thread ID {Thread.CurrentThread.ManagedThreadId}");
                Console.WriteLine(context.Request.RawUrl);
                Console.WriteLine(context.Request.HttpMethod);

                HttpRequestHandler httpRequestHandler;
                if (url2requestHandlerMapping.TryGetValue(context.Request.Url.AbsolutePath, out httpRequestHandler))
                {
                    httpRequestHandler(context);
                    return;
                }
            }

            static void handleIndexPageRequest(HttpListenerContext context)
            {
                if (context.Request.HttpMethod != "GET")
                {
                    sendAnswerWithCode(context.Response, 405);
                    return;
                }
                string result = "";
                string spName = "";
                foreach (string key in context.Request.QueryString.Keys)
                {
                    if (key == objectNameParam)
                    {
                        spName = context.Request.QueryString[key];
                        result = requestDatabase(spName);
                    }
                    Console.WriteLine($"key {key} value {context.Request.QueryString[key]}");
                }
                sendStaticResource(context.Response, string.Format(Resources.index_html, spName, connectionString, result));
            }

            static void handleConnectRequest(HttpListenerContext context)
            {
                if (context.Request.HttpMethod != "POST")
                {
                    sendAnswerWithCode(context.Response, 405);
                    return;
                }
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    connectionString = reader.ReadToEnd();
                    Console.WriteLine(connectionString);
                    sendAnswerWithCode(context.Response, 200);
                }
            }

            static string requestDatabase(string spName)
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

                            string tableInfoXml = "";
                            using (SqlDataReader dr = sqlCmd.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    tableInfoXml = dr.GetString(0);
                                }
                            }

                            var xmlSource = new XmlDocument();
                            var htmlDest = new StringBuilder();
                            xmlSource.LoadXml(tableInfoXml);
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
                                    depList[depName.ToLower()] = $"<a href='{clientUrl}?{objectNameParam}={depName}' title='{typeDesc}'>{depName}<a>";
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

            static void sendStaticResource(HttpListenerResponse response, string resourceText)
            {
                var buffer = Encoding.UTF8.GetBytes(resourceText);
                response.ContentLength64 = buffer.Length;
                var output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                return;
            }

            static void sendAnswerWithCode(HttpListenerResponse response, int statusCode)
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
}
