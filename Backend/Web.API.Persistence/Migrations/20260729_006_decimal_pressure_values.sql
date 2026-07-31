-- Convert leak test pressure fields from text with MPa suffix to DECIMAL values.

SET @parameter_pressure_type = (
  SELECT DATA_TYPE
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'leak_test_work_records'
    AND COLUMN_NAME = 'parameter_pressure'
);

SET @pressure_input_type = (
  SELECT DATA_TYPE
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'leak_test_work_records'
    AND COLUMN_NAME = 'pressure_input'
);

SET @sql = IF(
  @parameter_pressure_type <> 'decimal' OR @pressure_input_type <> 'decimal',
  "UPDATE `leak_test_work_records`
   SET
     `parameter_pressure` = NULLIF(REPLACE(REPLACE(UPPER(`parameter_pressure`), 'MPA', ''), ' ', ''), ''),
     `pressure_input` = NULLIF(REPLACE(REPLACE(UPPER(`pressure_input`), 'MPA', ''), ' ', ''), '')",
  "SELECT 1"
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

ALTER TABLE `leak_test_work_records`
  MODIFY COLUMN `parameter_pressure` decimal(8, 2) NOT NULL,
  MODIFY COLUMN `pressure_input` decimal(8, 2) NOT NULL;
