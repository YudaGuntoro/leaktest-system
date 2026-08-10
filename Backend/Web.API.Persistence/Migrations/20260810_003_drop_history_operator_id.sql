-- History stores operator_name as a snapshot, not a relation to master operators.

SET @has_work_operator_id := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'operator_id'
);

SET @sql := IF(
    @has_work_operator_id > 0,
    'UPDATE leak_test_work_records records JOIN operators operators_master ON operators_master.id = records.operator_id SET records.operator_name = operators_master.operator_name WHERE records.operator_name IS NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_rework_operator_id := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'rework_engine_records'
      AND COLUMN_NAME = 'operator_id'
);

SET @sql := IF(
    @has_rework_operator_id > 0,
    'UPDATE rework_engine_records records JOIN operators operators_master ON operators_master.id = records.operator_id SET records.operator_name = operators_master.operator_name WHERE records.operator_name IS NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_work_fk := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND CONSTRAINT_NAME = 'fk_leak_test_work_records_operator'
);
SET @sql := IF(@has_work_fk > 0, 'ALTER TABLE leak_test_work_records DROP FOREIGN KEY fk_leak_test_work_records_operator', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_rework_fk := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'rework_engine_records'
      AND CONSTRAINT_NAME = 'fk_rework_engine_records_operator'
);
SET @sql := IF(@has_rework_fk > 0, 'ALTER TABLE rework_engine_records DROP FOREIGN KEY fk_rework_engine_records_operator', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_work_index := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND INDEX_NAME = 'ix_leak_test_work_records_operator_id'
);
SET @sql := IF(@has_work_index > 0, 'DROP INDEX ix_leak_test_work_records_operator_id ON leak_test_work_records', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_rework_index := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'rework_engine_records'
      AND INDEX_NAME = 'ix_rework_engine_records_operator_id'
);
SET @sql := IF(@has_rework_index > 0, 'DROP INDEX ix_rework_engine_records_operator_id ON rework_engine_records', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(@has_work_operator_id > 0, 'ALTER TABLE leak_test_work_records DROP COLUMN operator_id', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(@has_rework_operator_id > 0, 'ALTER TABLE rework_engine_records DROP COLUMN operator_id', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
