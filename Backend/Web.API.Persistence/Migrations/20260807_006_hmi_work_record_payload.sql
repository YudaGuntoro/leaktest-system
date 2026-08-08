-- Leaktester Work Record - store raw HMI/COSMO payload context on normal work records.

SET @barcode_scan_column_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'barcode_scan'
);
SET @add_barcode_scan_sql := IF(
    @barcode_scan_column_exists = 0,
    'ALTER TABLE leak_test_work_records ADD COLUMN barcode_scan VARCHAR(180) NULL AFTER engine_number',
    'SELECT 1'
);
PREPARE add_barcode_scan_statement FROM @add_barcode_scan_sql;
EXECUTE add_barcode_scan_statement;
DEALLOCATE PREPARE add_barcode_scan_statement;

SET @channel_no_column_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'channel_no'
);
SET @add_channel_no_sql := IF(
    @channel_no_column_exists = 0,
    'ALTER TABLE leak_test_work_records ADD COLUMN channel_no VARCHAR(20) NULL AFTER parameter_pressure',
    'SELECT 1'
);
PREPARE add_channel_no_statement FROM @add_channel_no_sql;
EXECUTE add_channel_no_statement;
DEALLOCATE PREPARE add_channel_no_statement;

SET @press_set_up_column_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'press_set_up'
);
SET @add_press_set_up_sql := IF(
    @press_set_up_column_exists = 0,
    'ALTER TABLE leak_test_work_records ADD COLUMN press_set_up DECIMAL(8, 2) NULL AFTER channel_no',
    'SELECT 1'
);
PREPARE add_press_set_up_statement FROM @add_press_set_up_sql;
EXECUTE add_press_set_up_statement;
DEALLOCATE PREPARE add_press_set_up_statement;

SET @press_set_low_column_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'press_set_low'
);
SET @add_press_set_low_sql := IF(
    @press_set_low_column_exists = 0,
    'ALTER TABLE leak_test_work_records ADD COLUMN press_set_low DECIMAL(8, 2) NULL AFTER press_set_up',
    'SELECT 1'
);
PREPARE add_press_set_low_statement FROM @add_press_set_low_sql;
EXECUTE add_press_set_low_statement;
DEALLOCATE PREPARE add_press_set_low_statement;

SET @barcode_scan_index_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND INDEX_NAME = 'ix_leak_test_work_records_barcode_scan'
);
SET @add_barcode_scan_index_sql := IF(
    @barcode_scan_index_exists = 0,
    'CREATE INDEX ix_leak_test_work_records_barcode_scan ON leak_test_work_records (barcode_scan)',
    'SELECT 1'
);
PREPARE add_barcode_scan_index_statement FROM @add_barcode_scan_index_sql;
EXECUTE add_barcode_scan_index_statement;
DEALLOCATE PREPARE add_barcode_scan_index_statement;

SET @channel_no_index_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND INDEX_NAME = 'ix_leak_test_work_records_channel_no'
);
SET @add_channel_no_index_sql := IF(
    @channel_no_index_exists = 0,
    'CREATE INDEX ix_leak_test_work_records_channel_no ON leak_test_work_records (channel_no)',
    'SELECT 1'
);
PREPARE add_channel_no_index_statement FROM @add_channel_no_index_sql;
EXECUTE add_channel_no_index_statement;
DEALLOCATE PREPARE add_channel_no_index_statement;

UPDATE leak_test_work_records records
JOIN engine_models models ON models.id = records.engine_model_id
SET records.barcode_scan = CONCAT(models.engine_model, ' ', records.engine_number)
WHERE records.barcode_scan IS NULL OR TRIM(records.barcode_scan) = '';
