-- Remove judgement name from work records; display it from judgement master mapping.

SET @has_judgement_name := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'judgement_name'
);
SET @sql := IF(
    @has_judgement_name > 0,
    'ALTER TABLE leak_test_work_records DROP COLUMN judgement_name',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
