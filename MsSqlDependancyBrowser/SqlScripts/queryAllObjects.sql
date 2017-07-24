select A.type_desc, A.name, schema_name = B.name
  from sys.objects A
  left join sys.schemas B on A.schema_id = B.schema_id
 where type in ('P')
 order by type_desc, name;

select A.type_desc, A.name, schema_name = B.name
  from sys.objects A
  left join sys.schemas B on A.schema_id = B.schema_id
 where type in ('TF')
 order by type_desc, name;

select A.type_desc, A.name, schema_name = B.name
  from sys.objects A
  left join sys.schemas B on A.schema_id = B.schema_id
 where type in ('IF')
 order by type_desc, name;

select A.type_desc, A.name, schema_name = B.name
  from sys.objects A
  left join sys.schemas B on A.schema_id = B.schema_id
 where type in ('FN')
 order by type_desc, name;

select A.type_desc, A.name, schema_name = B.name
  from sys.objects A
  left join sys.schemas B on A.schema_id = B.schema_id
 where type in ('V')
 order by type_desc, name;

select A.type_desc, A.name, schema_name = B.name
  from sys.objects A
  left join sys.schemas B on A.schema_id = B.schema_id
 where type in ('U')
 order by type_desc, name;