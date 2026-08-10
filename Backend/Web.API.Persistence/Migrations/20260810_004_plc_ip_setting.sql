-- PLC connection target for the top-bar Online/Offline status.

SET @plc_ip_column_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'system_settings'
      AND COLUMN_NAME = 'plc_ip_address'
);

SET @add_plc_ip_sql := IF(
    @plc_ip_column_exists = 0,
    'ALTER TABLE system_settings ADD COLUMN plc_ip_address VARCHAR(80) NULL AFTER backup_schedule',
    'SELECT 1'
);
PREPARE add_plc_ip_statement FROM @add_plc_ip_sql;
EXECUTE add_plc_ip_statement;
DEALLOCATE PREPARE add_plc_ip_statement;
