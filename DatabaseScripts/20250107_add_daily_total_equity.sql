-- 创建每日总权益记录表
CREATE TABLE IF NOT EXISTS DailyTotalEquities (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecordDate DATE NOT NULL UNIQUE,
    TotalAmount DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    Remarks TEXT
);

-- 创建索引
CREATE INDEX IF NOT EXISTS IX_DailyTotalEquities_RecordDate ON DailyTotalEquities(RecordDate);

