-- Fresh database bootstrap for Leaktester Work Record
-- PT. Yanmar Diesel Indonesia / MySQL 8

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
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_roles_role_name (role_name)
);

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
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_users_username (username),
    UNIQUE KEY uq_users_email (email),
    KEY ix_users_roles_id (roles_id),
    CONSTRAINT fk_users_roles FOREIGN KEY (roles_id) REFERENCES roles(id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

-- Default login: root / root_native
INSERT IGNORE INTO users
    (id, username, full_name, email, roles_id, is_active, password_hash, password_salt)
VALUES
    (1, 'admin', 'Leaktester Work Record Administrator', 'admin@leaktester.local', 1, 1,
     'mV/QhZOhh7mvmWj0P1RgeXm3hZB1AkKHY5jfEcrC7PE=', 'Y21tcy1hZG1pbi1zYWx0LXYx'),
    (2, 'root', 'Leaktester Work Record Root', 'root@leaktester.local', 1, 1,
     'QzApLclLs39Wg6pGId5HXwbyiH5QdA41S8X40bj4Mm4=', 'eWFubWFyLXJvb3QtdjEhIQ==');

SOURCE Backend/Web.API.Persistence/Migrations/20260729_003_engine_models.sql;
SOURCE Backend/Web.API.Persistence/Migrations/20260729_001_leak_test_work_records.sql;
SOURCE Backend/Web.API.Persistence/Migrations/20260729_004_demo_leak_test_work_records.sql;
SOURCE Backend/Web.API.Persistence/Migrations/20260729_006_decimal_pressure_values.sql;
SOURCE Backend/Web.API.Persistence/Migrations/20260730_004_demo_current_date_leak_test_work_records.sql;
SOURCE Backend/Web.API.Persistence/Migrations/20260807_001_operators.sql;
SOURCE Backend/Web.API.Persistence/Migrations/20260807_002_rework_engine_records.sql;
SOURCE Backend/Web.API.Persistence/Migrations/20260807_003_demo_dashboard_work_records.sql;
SOURCE Backend/Web.API.Persistence/Migrations/20260807_004_demo_dashboard_monthly_volume.sql;
SOURCE Backend/Web.API.Persistence/Migrations/20260807_005_leak_test_parameters.sql;
SOURCE Backend/Web.API.Persistence/Migrations/20260807_006_hmi_work_record_payload.sql;
