using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Xml;
using System.Xml.Xsl;
using SqlScriptParser;

namespace MsSqlDependencyBrowser
{
    class MsSqlRequestService
    {
        HashSet<string> keywords;
        XslCompiledTransform xslTranCompiler;
        string connectionString;

        public MsSqlRequestService(string server, string database)
        {
            string tmpConnectionString = string.Format(Resources.connectionStringTemplate, server, database);
            try
            {
                using (var sqlConn = new SqlConnection(tmpConnectionString))
                {
                    sqlConn.Open();
                    connectionString = tmpConnectionString;
                }
            } catch (Exception ex)
            {
                connectionString = "";
                throw ex;
            }
            keywords = new HashSet<string>(Resources.keywords.Split(' '));
            xslTranCompiler = new XslCompiledTransform();
            var xslDoc = new XmlDocument();
            xslDoc.LoadXml(Resources.table2html_xslt);
            xslTranCompiler.Load(xslDoc);
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
                            List<object> serverObjects = new List<object>();
                            while (dr.Read())
                            {
                                curr_type_desc = dr.GetString(0);
                                serverObjects.Add(new { name = dr.GetString(1), schema = dr.GetString(2) });
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

        public string requestDatabaseObjectInfo(string objectName, string schemaName)
        {
            try
            {
                using (var sqlConn = new SqlConnection(connectionString))
                {
                    var sqlCmd = new SqlCommand(Resources.queryObjectInfo_sql, sqlConn);
                    sqlConn.Open();
                    sqlCmd.Parameters.Add("@objectName", SqlDbType.NVarChar);
                    sqlCmd.Parameters.Add("@schemaName", SqlDbType.NVarChar);
                    sqlCmd.Parameters["@objectName"].Value = objectName;
                    sqlCmd.Parameters["@schemaName"].Value = schemaName;

                    string object_text = "";
                    string type_desc = "";
                    using (SqlDataReader dr = sqlCmd.ExecuteReader())
                    {
                        if (dr.HasRows && dr.Read())
                        {
                            object_text = dr.IsDBNull(0) ? "" : dr.GetString(0);
                            objectName = dr.GetString(2);
                            schemaName = dr.GetString(1);
                            type_desc = dr.GetString(3);
                        } else
                        {
                            return $"object '{schemaName}.{objectName}' not exists";
                        }
                    }

                    if (type_desc == "USER_TABLE")
                    {
                        sqlCmd = new SqlCommand(Resources.queryTableXml_sql, sqlConn);
                        sqlCmd.Parameters.Add("@objectName", SqlDbType.NVarChar);
                        sqlCmd.Parameters.Add("@schemaName", SqlDbType.NVarChar);
                        sqlCmd.Parameters["@objectName"].Value = objectName;
                        sqlCmd.Parameters["@schemaName"].Value = schemaName;
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

                    if (objectName != "")
                    {
                        sqlCmd = new SqlCommand(Resources.queryObjectDependancies_sql, sqlConn);
                        sqlCmd.Parameters.Add("@objectFullName", SqlDbType.NVarChar);
                        sqlCmd.Parameters["@objectFullName"].Value = $"{schemaName}.{objectName}";

                        var depList = new Dictionary<string, string>();
                        using (SqlDataReader dr = sqlCmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                string depName = dr.GetString(0);
                                string typeDesc = dr.IsDBNull(1) ? "UNKNOWN_OBJECT" : dr.GetString(1);
                                typeDesc += dr.IsDBNull(2) ? "" : $": {dr.GetString(2)}";
                                string depSchemaName = dr.GetString(3);
                                depList[depName.ToLower()] = buildSqlServerObjectLink(depSchemaName, depName, typeDesc);
                            }
                        }

                        var wordProcessor = new WordProcessor(keywords, depList);
                        var singleCommentProcessor = new BlockProcessor(wordProcessor, @"--.*[\r\n]", "green");
                        var commentAndStringProcessor = new CommentAndStringProcessor(singleCommentProcessor);
                        return commentAndStringProcessor.Process(object_text);
                    }
                }
                return "unknown type of object";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
                return $"Exception: {ex}";
            }
        }

        string buildSqlServerObjectLink(string schemaName, string objectName, string typeDesc)
        {
            return $"<a href='#!/{Resources.schemaNameParam}/{schemaName}/{Resources.objectNameParam}/{objectName}' title='{typeDesc}'>{objectName}<a>";
        }
    }
}
