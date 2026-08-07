-- Leaktester Work Record - operator master data.

CREATE TABLE IF NOT EXISTS operators (
    id INT AUTO_INCREMENT PRIMARY KEY,
    operator_code VARCHAR(50) NOT NULL,
    operator_name VARCHAR(150) NOT NULL,
    department VARCHAR(80) NULL,
    note VARCHAR(150) NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_operators_operator_code (operator_code),
    KEY ix_operators_operator_name (operator_name)
);

INSERT INTO operators
    (id, operator_code, operator_name, department, note, is_deleted)
VALUES
    (1, 'LT-OP-0001', 'Budi Santoso', 'Production', 'Dummy leak test operator', 0),
    (2, 'LT-OP-0002', 'Siti Aminah', 'Production', 'Dummy leak test operator', 0),
    (3, 'LT-OP-0003', 'Agus Pratama', 'Production', 'Dummy leak test operator', 0),
    (4, 'LT-OP-0004', 'Dewi Lestari', 'Production', 'Dummy leak test operator', 0)
ON DUPLICATE KEY UPDATE
    operator_code = VALUES(operator_code),
    operator_name = VALUES(operator_name),
    department = VALUES(department),
    note = VALUES(note),
    is_deleted = VALUES(is_deleted);

SET @operator_id_column_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'operator_id'
);

SET @add_operator_id_sql := IF(
    @operator_id_column_exists = 0,
    'ALTER TABLE leak_test_work_records ADD COLUMN operator_id INT NULL AFTER machine_name',
    'SELECT 1'
);
PREPARE add_operator_id_statement FROM @add_operator_id_sql;
EXECUTE add_operator_id_statement;
DEALLOCATE PREPARE add_operator_id_statement;

SET @operator_index_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND INDEX_NAME = 'ix_leak_test_work_records_operator_id'
);

SET @add_operator_index_sql := IF(
    @operator_index_exists = 0,
    'CREATE INDEX ix_leak_test_work_records_operator_id ON leak_test_work_records (operator_id)',
    'SELECT 1'
);
PREPARE add_operator_index_statement FROM @add_operator_index_sql;
EXECUTE add_operator_index_statement;
DEALLOCATE PREPARE add_operator_index_statement;

SET @operator_fk_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND CONSTRAINT_NAME = 'fk_leak_test_work_records_operator'
);

SET @add_operator_fk_sql := IF(
    @operator_fk_exists = 0,
    'ALTER TABLE leak_test_work_records ADD CONSTRAINT fk_leak_test_work_records_operator FOREIGN KEY (operator_id) REFERENCES operators (id) ON UPDATE CASCADE ON DELETE SET NULL',
    'SELECT 1'
);
PREPARE add_operator_fk_statement FROM @add_operator_fk_sql;
EXECUTE add_operator_fk_statement;
DEALLOCATE PREPARE add_operator_fk_statement;

UPDATE leak_test_work_records
SET operator_id = ((id - 1) MOD 4) + 1
WHERE operator_id IS NULL;
