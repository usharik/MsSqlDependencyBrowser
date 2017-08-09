 select distinct type, type_desc 
   from sys.objects
  where type in ('P', 'TF', 'IF', 'FN', 'V', 'U');