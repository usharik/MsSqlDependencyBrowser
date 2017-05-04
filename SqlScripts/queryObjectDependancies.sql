SELECT * 
FROM (
    SELECT DISTINCT A.referenced_entity_name, B.type_desc
    FROM sys.dm_sql_referenced_entities(@objectFullName, 'OBJECT') A
    LEFT JOIN sys.objects B ON A.referenced_id = B.object_id
    WHERE A.referenced_id IS NOT NULL
    ) A
ORDER BY LEN(A.referenced_entity_name) desc;