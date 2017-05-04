SELECT  [column].name AS '@columnName',
		[type].name AS '@typeName', 
		[type].max_length AS '@maxLength',
		[type].precision '@precision'
	FROM sys.tables as [table]
	INNER JOIN sys.columns [column] ON [table].object_id = [column].object_id
	INNER JOIN sys.types [type] ON [column].system_type_id = [type].system_type_id
	WHERE [table].name = @objectName
	FOR XML PATH('column'), ROOT('table');