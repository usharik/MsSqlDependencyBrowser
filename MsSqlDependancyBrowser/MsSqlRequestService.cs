using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Xml;
using System.Xml.Xsl;
using SqlScriptParser;

namespace MsSqlDependancyBrowser
{
    class MsSqlRequestService
    {
        HashSet<string> keywords;
        XslCompiledTransform xslTranCompiler;
        string connectionString;

        public MsSqlRequestService()
        {
            keywords = new HashSet<string>(Resources.keywords.Split(' '));
            xslTranCompiler = new XslCompiledTransform();
            var xslDoc = new XmlDocument();
            xslDoc.LoadXml(Resources.table2html_xslt);
            xslTranCompiler.Load(xslDoc);
            connectionString = null;
        }

        public string ConnectionString
        {
            get
            {
                return connectionString;
            }

            set
            {
                try
                {
                    using (var sqlConn = new SqlConnection(value))
                    {
                        sqlConn.Open();
                        connectionString = value;
                    }
                } catch (Exception ex)
                {
                    connectionString = null;
                    throw ex;
                }
            }
        }

        public bool isConnected()
        {
            return connectionString != null;
        }

        public List<string> requestDatabaseList(string connectionString)
        {
            List<string> databaseList = new List<string>();
            using (var sqlConn = new SqlConnection(connectionString))
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
                return databaseList;
            }
        }

        public List<object> requestAllServerObjectList()
        {
            try
            {
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
                    return allServerObjects;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
                return new List<object>();
            }
        }

        public string requestDatabaseObjectInfo(string url, string spName)
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
                                depList[depName.ToLower()] = $"<a href='{url}?{Resources.objectNameParam}={depName}' title='{typeDesc}'>{depName}<a>";
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
