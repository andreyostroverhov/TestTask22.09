-- 1. Только записи с одинаковым ID в обеих таблицах.
SELECT t1.*, t2.*
FROM T1 AS t1
INNER JOIN T2 AS t2 ON t2.ID = t1.ID;

-- 2. Все записи T1 и совпавшие записи T2.
SELECT t1.*, t2.*
FROM T1 AS t1
LEFT JOIN T2 AS t2 ON t2.ID = t1.ID;

-- 3. Записи T1, для которых ID отсутствует в T2.
SELECT t1.*
FROM T1 AS t1
LEFT JOIN T2 AS t2 ON t2.ID = t1.ID
WHERE t2.ID IS NULL;
