-- Store the original MQTT payload alongside successfully inserted work records.
SET @source_payload_column_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'source_payload'
);
SET @add_source_payload_sql := IF(
    @source_payload_column_exists = 0,
    'ALTER TABLE leak_test_work_records ADD COLUMN source_payload LONGTEXT NULL AFTER judgement_code',
    'SELECT 1'
);
PREPARE add_source_payload_statement FROM @add_source_payload_sql;
EXECUTE add_source_payload_statement;
DEALLOCATE PREPARE add_source_payload_statement;
