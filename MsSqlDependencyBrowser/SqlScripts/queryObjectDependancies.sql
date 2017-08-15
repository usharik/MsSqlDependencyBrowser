SELECT *, num = row_number() over (partition by referenced_entity_name order by schema_name) 
FROM (
    SELECT DISTINCT 
	       A.referenced_entity_name,
		   B.type_desc,
		   C.base_object_name,
		   D.name AS schema_name
    FROM sys.dm_sql_referenced_entities(@objectFullName, 'OBJECT') A
    LEFT JOIN sys.objects B ON A.referenced_id = B.object_id
	LEFT JOIN sys.synonyms C ON B.object_id = C.object_id
   INNER JOIN sys.schemas D on isnull(B.schema_id, C.schema_id) = D.schema_id
   WHERE A.referenced_id IS NOT NULL
     AND B.type in ('P', 'TF', 'IF', 'FN', 'V', 'U')
    ) A
ORDER BY LEN(A.referenced_entity_name) desc;