-- 增减资表结构调整：增加 Type、Details 字段
-- 注意：SQLite 一次只能添加一列，且 NVARCHAR(MAX) 用 TEXT 替代

ALTER TABLE CapitalIncreases
ADD COLUMN Type NVARCHAR(20) NOT NULL DEFAULT 'Increase';

ALTER TABLE CapitalIncreases
ADD COLUMN Details TEXT NULL;

-- 如需在其他数据库执行，请根据方言调整数据类型

