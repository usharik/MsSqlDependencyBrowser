select A.name, schema_name = B.name, A.type_desc
  from sys.objects A
  left join sys.schemas B on A.schema_id = B.schema_id
 where type = @type
 order by type_desc, name;