-- Leaktester Work Record - denormalize operator name into history records.

SET @leak_history_operator_name_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'operator_name'
);

SET @add_leak_history_operator_name_sql := IF(
    @leak_history_operator_name_exists = 0,
    'ALTER TABLE leak_test_work_records ADD COLUMN operator_name VARCHAR(150) NULL AFTER operator_id',
    'SELECT 1'
);
PREPARE add_leak_history_operator_name_stmt FROM @add_leak_history_operator_name_sql;
EXECUTE add_leak_history_operator_name_stmt;
DEALLOCATE PREPARE add_leak_history_operator_name_stmt;

SET @rework_history_operator_name_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'rework_engine_records'
      AND COLUMN_NAME = 'operator_name'
);

SET @add_rework_history_operator_name_sql := IF(
    @rework_history_operator_name_exists = 0,
    'ALTER TABLE rework_engine_records ADD COLUMN operator_name VARCHAR(150) NULL AFTER operator_id',
    'SELECT 1'
);
PREPARE add_rework_history_operator_name_stmt FROM @add_rework_history_operator_name_sql;
EXECUTE add_rework_history_operator_name_stmt;
DEALLOCATE PREPARE add_rework_history_operator_name_stmt;

UPDATE leak_test_work_records records
JOIN operators operators_master ON operators_master.id = records.operator_id
SET records.operator_name = operators_master.operator_name
WHERE records.operator_name IS NULL;

UPDATE rework_engine_records records
JOIN operators operators_master ON operators_master.id = records.operator_id
SET records.operator_name = operators_master.operator_name
WHERE records.operator_name IS NULL;
