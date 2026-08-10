-- Add judgement code to leak test work records.

SET @has_judgement_code := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'judgement_code'
);
SET @sql := IF(
    @has_judgement_code = 0,
    'ALTER TABLE leak_test_work_records ADD COLUMN judgement_code INT NULL AFTER cycle_time_leak_test_minutes',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_judgement_index := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND INDEX_NAME = 'ix_leak_test_work_records_judgement_code'
);
SET @sql := IF(
    @has_judgement_index = 0,
    'CREATE INDEX ix_leak_test_work_records_judgement_code ON leak_test_work_records (judgement_code)',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
