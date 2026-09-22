-- SQL Server: выгрузка таблицы T в XML.
SELECT Id, Code, Name, StatusId
FROM T
FOR XML PATH('row'), ROOT('data'), TYPE;

-- Пример чтения XML из переменной/поля XML.
DECLARE @xml xml = N'
<data>
  <row>
  <Id>1</Id>
  <Code>gargadgadfga</Code>
  <Name>Запрос предложений 1</Name>
  <StatusId>45</StatusId>
  </row>
  <row>
  <Id>2</Id>
  <Code>bsftrggdfgadfgdfat</Code>
  <Name>Запрос предложений 2</Name>
  <StatusId>2</StatusId>
  </row>
</data>';

SELECT
    r.value('(Id/text())[1]', 'int') AS Id,
    r.value('(Code/text())[1]', 'nvarchar(100)') AS Code,
    r.value('(Name/text())[1]', 'nvarchar(200)') AS Name,
    r.value('(StatusId/text())[1]', 'int') AS StatusId
FROM @xml.nodes('/data/row') AS x(r)
WHERE r.value('(StatusId/text())[1]', 'int') <> 3;