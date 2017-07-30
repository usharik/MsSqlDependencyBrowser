SELECT sp_text = B.definition,
       sp_name = C.name,
	   sp_schema = A.name,
	   A.type_desc
FROM sys.objects A
 LEFT JOIN sys.sql_modules B ON A.object_id = B.object_id
INNER JOIN sys.schemas C ON A.schema_id = C.schema_id
WHERE A.name = @objectName
  AND C.name = @schemaName;