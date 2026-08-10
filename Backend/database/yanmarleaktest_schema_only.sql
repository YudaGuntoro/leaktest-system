-- Leaktester Work Record - schema only bootstrap
-- Creates database and tables without demo/history records.
-- MySQL 8+

CREATE DATABASE IF NOT EXISTS yanmarleaktest
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE yanmarleaktest;

CREATE TABLE IF NOT EXISTS roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    role_name VARCHAR(30) NOT NULL,
    description VARCHAR(120) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_roles_role_name (role_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO roles
    (id, role_name, description, is_active)
VALUES
    (1, 'ADMIN', 'Administrator', 1),
    (2, 'SUPERVISOR', 'Supervisor', 1),
    (3, 'OPERATOR', 'Operator', 1),
    (4, 'VIEWER', 'Viewer', 1)
ON DUPLICATE KEY UPDATE
    role_name = VALUES(role_name),
    description = VALUES(description),
    is_active = VALUES(is_active);

CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(80) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    email VARCHAR(150) NULL,
    phone VARCHAR(50) NULL,
    roles_id INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    password_hash VARCHAR(255) NOT NULL,
    password_salt VARCHAR(255) NOT NULL,
    last_login_at DATETIME NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_users_username (username),
    UNIQUE KEY uq_users_email (email),
    KEY ix_users_roles_id (roles_id),
    CONSTRAINT fk_users_roles
        FOREIGN KEY (roles_id) REFERENCES roles (id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Default login: root / root_native
INSERT INTO users
    (id, username, full_name, email, roles_id, is_active, password_hash, password_salt)
VALUES
    (1, 'admin', 'Leaktester Work Record Administrator', 'admin@leaktester.local', 1, 1,
     'mV/QhZOhh7mvmWj0P1RgeXm3hZB1AkKHY5jfEcrC7PE=', 'Y21tcy1hZG1pbi1zYWx0LXYx'),
    (2, 'root', 'Leaktester Work Record Root', 'root@leaktester.local', 1, 1,
     'QzApLclLs39Wg6pGId5HXwbyiH5QdA41S8X40bj4Mm4=', 'eWFubWFyLXJvb3QtdjEhIQ==')
ON DUPLICATE KEY UPDATE
    username = VALUES(username),
    full_name = VALUES(full_name),
    email = VALUES(email),
    roles_id = VALUES(roles_id),
    is_active = VALUES(is_active);

CREATE TABLE IF NOT EXISTS engine_models (
    id INT AUTO_INCREMENT PRIMARY KEY,
    engine_model VARCHAR(45) NOT NULL,
    description VARCHAR(45) NULL,
    note VARCHAR(45) NULL,
    is_deleted TINYINT(1) NULL,
    UNIQUE KEY uq_engine_models_engine_model (engine_model)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS leak_test_parameters (
    id INT AUTO_INCREMENT PRIMARY KEY,
    channel_no VARCHAR(20) NOT NULL,
    model_parameter VARCHAR(150) NOT NULL,
    item_name VARCHAR(120) NOT NULL,
    item_value VARCHAR(80) NOT NULL,
    machine_names VARCHAR(1000) NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_leak_test_parameters_channel_item (channel_no, item_name),
    KEY ix_leak_test_parameters_channel_no (channel_no),
    KEY ix_leak_test_parameters_model_parameter (model_parameter)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS leak_test_judgements (
    id INT AUTO_INCREMENT PRIMARY KEY,
    judgement_code INT NOT NULL,
    judgement_name VARCHAR(80) NOT NULL,
    result VARCHAR(10) NOT NULL,
    note VARCHAR(150) NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_leak_test_judgements_code (judgement_code),
    KEY ix_leak_test_judgements_result (result)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO leak_test_judgements
    (judgement_code, judgement_name, result, note, is_deleted)
VALUES
    (1, 'LL NG', 'NG', 'HMI judgement', 0),
    (2, 'PASS', 'OK', 'HMI judgement', 0),
    (3, 'UL NG', 'NG', 'HMI judgement', 0),
    (4, 'LL2 NG', 'NG', 'HMI judgement', 0),
    (5, 'UL2 NG', 'NG', 'HMI judgement', 0),
    (6, 'ERROR', 'NG', 'HMI judgement', 0),
    (7, '', '', '', 0),
    (8, '', '', '', 0),
    (9, '', '', '', 0),
    (10, '', '', '', 0),
    (11, '', '', '', 0),
    (12, '', '', '', 0),
    (13, '', '', '', 0),
    (14, '', '', '', 0),
    (15, '', '', '', 0),
    (16, '', '', '', 0)
ON DUPLICATE KEY UPDATE
    judgement_name = VALUES(judgement_name),
    result = VALUES(result),
    note = VALUES(note),
    is_deleted = VALUES(is_deleted),
    updated_at = CURRENT_TIMESTAMP;

CREATE TABLE IF NOT EXISTS measurement_units (
    id INT AUTO_INCREMENT PRIMARY KEY,
    unit_category VARCHAR(50) NOT NULL,
    unit_symbol VARCHAR(20) NOT NULL,
    unit_name VARCHAR(80) NOT NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_measurement_units_category_symbol (unit_category, unit_symbol)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO measurement_units
    (unit_category, unit_symbol, unit_name, is_deleted)
VALUES
    ('pressure', 'MPa', 'Megapascal', 0),
    ('cycle_time', 's', 'Second', 0)
ON DUPLICATE KEY UPDATE
    unit_name = VALUES(unit_name),
    is_deleted = VALUES(is_deleted),
    updated_at = CURRENT_TIMESTAMP;

CREATE TABLE IF NOT EXISTS system_settings (
    id INT PRIMARY KEY,
    pressure_unit_id INT NOT NULL,
    cycle_time_unit_id INT NOT NULL,
    backup_db_location VARCHAR(500) NULL,
    backup_schedule VARCHAR(20) NOT NULL DEFAULT 'daily',
    plc_ip_address VARCHAR(80) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_system_settings_pressure_unit
        FOREIGN KEY (pressure_unit_id) REFERENCES measurement_units (id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT fk_system_settings_cycle_time_unit
        FOREIGN KEY (cycle_time_unit_id) REFERENCES measurement_units (id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO system_settings
    (id, pressure_unit_id, cycle_time_unit_id, backup_schedule)
SELECT
    1,
    pressure.id,
    cycle_time.id,
    'daily'
FROM measurement_units pressure
CROSS JOIN measurement_units cycle_time
WHERE pressure.unit_category = 'pressure'
  AND pressure.unit_symbol = 'MPa'
  AND cycle_time.unit_category = 'cycle_time'
  AND cycle_time.unit_symbol = 's'
ON DUPLICATE KEY UPDATE
    id = id;

CREATE TABLE IF NOT EXISTS leak_test_work_records (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    engine_model_id INT NOT NULL,
    engine_number VARCHAR(120) NOT NULL,
    barcode_scan VARCHAR(180) NULL,
    check_date DATE NOT NULL,
    check_time VARCHAR(8) NOT NULL,
    machine_name VARCHAR(150) NOT NULL,
    operator_name VARCHAR(150) NULL,
    parameter_pressure DECIMAL(8, 2) NOT NULL,
    channel_no VARCHAR(20) NULL,
    press_set_up DECIMAL(8, 2) NULL,
    press_set_low DECIMAL(8, 2) NULL,
    pressure_input DECIMAL(8, 2) NOT NULL,
    cycle_time_leak_test_minutes DECIMAL(8, 2) NOT NULL,
    result VARCHAR(10) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY ix_leak_test_work_records_date_engine (check_date, engine_number),
    KEY ix_leak_test_work_records_barcode_scan (barcode_scan),
    KEY ix_leak_test_work_records_channel_no (channel_no),
    KEY ix_leak_test_work_records_engine_model_id (engine_model_id),
    CONSTRAINT fk_leak_test_work_records_engine_model
        FOREIGN KEY (engine_model_id) REFERENCES engine_models (id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS rework_engine_records (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    engine_model_id INT NULL,
    engine_model_text VARCHAR(80) NULL,
    engine_number VARCHAR(120) NOT NULL,
    barcode_scan VARCHAR(180) NOT NULL,
    rework_date DATE NOT NULL,
    rework_time VARCHAR(8) NOT NULL,
    operator_name VARCHAR(150) NULL,
    parameter_pressure DECIMAL(8, 2) NOT NULL,
    pressure_input DECIMAL(8, 2) NOT NULL,
    result VARCHAR(10) NOT NULL,
    note VARCHAR(255) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY ix_rework_engine_records_date_engine (rework_date, engine_number),
    KEY ix_rework_engine_records_engine_model_id (engine_model_id),
    CONSTRAINT fk_rework_engine_records_engine_model
        FOREIGN KEY (engine_model_id) REFERENCES engine_models (id)
        ON UPDATE CASCADE
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
