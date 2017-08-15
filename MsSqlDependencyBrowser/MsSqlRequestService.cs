using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Xml;
using System.Xml.Xsl;
using SqlScriptParser;
using Dapper;

namespace MsSqlDependencyBrowser
{
    class DbObject
    {
        public string name { get; set; }
        public string schema_name { get; set; }
        public string type_desc { get; set; }
        public string object_text { get; set; }
    }

    class DbObjectType
    {
        public string type { get; set; }
        public string type_desc { get; set; }
    }

    class DbDependentObject
    {
        public string referenced_entity_name { get; set; }
        public string type_desc { get; set; }
        public string base_object_name { get; set; }
        public string schema_name { get; set; }
        public int num { get; set; }

        public string buildSqlServerObjectLink()
        {
            return $"<a href='#!/{schema_name}.{referenced_entity_name}' title='{type_desc}'>{referenced_entity_name}<a>";
        }
    }

    class MsSqlRequestService
    {
        HashSet<string> keywords1;
        HashSet<string> keywords2;
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
            keywords1 = new HashSet<string>(Resources.keywords1.Split(' '));
            keywords2 = new HashSet<string>(Resources.keywords2.Split(' '));
            xslTranCompiler = new XslCompiledTransform();
            var xslDoc = new XmlDocument();
            xslDoc.LoadXml(Resources.table2html_xslt);
            xslTranCompiler.Load(xslDoc);
        }

        public List<string> requestDatabaseList(string connectionString)
        {
            using (var sqlConn = openConnection(connectionString))
            {
                return sqlConn.Query<String>(Resources.queryDatabaseList_sql).ToList();
            }
        }

        public List<object> requestAllServerObjectList()
        {
            List<object> allServerObjects = new List<object>();
            try
            {
                using (var sqlConn = openConnection(connectionString))
                {
                    var objectTypeList = sqlConn.Query<DbObjectType>(Resources.queryObjectTypes_sql).ToList();
                    foreach (DbObjectType objType in objectTypeList)
                    {
                        var objectList = sqlConn
                            .Query<DbObject>(Resources.queryAllObjects_sql, new { type = objType.type })
                            .Select(o => new { name = o.name, schema_name = o.schema_name })
                            .ToList();
                        if (objectList.Count > 0)
                        {
                            allServerObjects.Add(new { type_desc = objType.type_desc, objects = objectList });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }
            return allServerObjects;
        }

        public string requestDatabaseObjectInfo(string objectName, string schemaName)
        {
            try
            {
                using (var sqlConn = openConnection(connectionString))
                {
                    DbObject dbObject = null;
                    try
                    {
                        dbObject = sqlConn.Query<DbObject>(Resources.queryObjectInfo_sql, new { objectName = objectName, schemaName = schemaName }).Single();
                    } catch(InvalidOperationException)
                    {
                        return $"object '{schemaName}.{objectName}' not exists";
                    }

                    if (dbObject.type_desc == "USER_TABLE")
                    {
                        var objXml = sqlConn.Query<String>(Resources.queryTableXml_sql, new { objectName = objectName, schemaName = schemaName }).ToList();
                        var xmlSource = new XmlDocument();
                        xmlSource.LoadXml(String.Join("", objXml));
                        var htmlDest = new StringBuilder();
                        xslTranCompiler.Transform(xmlSource, XmlWriter.Create(htmlDest));
                        return htmlDest.ToString();
                    }

                    if (objectName != "")
                    {
                        Dictionary<string, string> depList = sqlConn
                            .Query<DbDependentObject>(Resources.queryObjectDependancies_sql, new { objectFullName = $"{schemaName}.{objectName}" })
                            .Where(dep => dep.num == 1)
                            .ToDictionary(dep => dep.referenced_entity_name.ToLower(), dep => dep.buildSqlServerObjectLink());

                        var dependencyProcessor = new DependencyProcessor(depList);
                        var keywordProcessor1 = new KeywordProcessor(dependencyProcessor, keywords1, "<b style='color:blue'>{0}</b>");
                        var keywordProcessor2 = new KeywordProcessor(keywordProcessor1, keywords2, "<span style='color:magenta'>{0}</span>");
                        var tempTableProcessor = new BlockProcessor(keywordProcessor2, @"@{1,2}\w+", "<span style='color:dimgray'>{0}</span>");
                        var varableProcessor = new BlockProcessor(tempTableProcessor, @"\#{1,2}\w+", "<span style='color:dimgray'>{0}</span>");
                        var singleCommentProcessor = new BlockProcessor(varableProcessor, @"--.*[\r\n]", "<b style='color:green'>{0}</b>");
                        var commentAndStringProcessor = new CommentAndStringProcessor(singleCommentProcessor);
                        return commentAndStringProcessor.Process(dbObject.object_text);
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

        IDbConnection openConnection(string connectionString)
        {
            var sqlConn = new SqlConnection(connectionString);
            sqlConn.Open();
            return sqlConn;
        }
    }
}
